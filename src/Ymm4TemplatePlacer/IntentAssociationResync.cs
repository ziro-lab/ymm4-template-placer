using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>Explicit, selected-scope, whole-bundle geometry Resync. Incomplete/copy-ambiguous bundles are never reconstructed.</summary>
public sealed class IntentAssociationResync
{
    private readonly List<(IntentGeometry Geometry, Guid Palette, string Snapshot)> guarded;
    public ResyncPlan Result { get; }
    private IntentAssociationResync(ResyncPlan result, List<(IntentGeometry, Guid, string)> guarded) { Result = result; this.guarded = guarded; }
    public static IntentAssociationResync Create(Timeline timeline, PlacerSettings settings, ExpressionPreset legacyPreset)
    {
        var tagged = timeline.Items.Select(item => (Item: item, State: IntentAssociationTag.Read(item.Remark, out var tag), Tag: tag)).ToArray();
        var groups = new HashSet<Guid>(); var skipped = new List<string>(); var legacy = new HashSet<IItem>(); var ignored = 0;
        foreach (var selected in timeline.SelectedItems.Distinct())
        {
            if (!timeline.Items.Contains(selected)) { skipped.Add("シーンに存在しない選択アイテム"); continue; }

            var registeredSelected = TachiePresetSourceAssociationTag.Read(selected.Remark, out _);
            if (registeredSelected == AssociationTagState.Invalid)
            {
                skipped.Add("選択アイテムの登録済み立ち絵プリセット関連付けが不正です");
                continue;
            }
            if (registeredSelected == AssociationTagState.Valid)
                continue; // handled by RegisteredPresetAssociationResync

            var state = IntentAssociationTag.Read(selected.Remark, out var membership);
            if (state == AssociationTagState.Invalid) { skipped.Add("選択アイテムのBundle関連付けが不正です"); continue; }
            if (state == AssociationTagState.Valid) { groups.Add(membership!.Group); continue; }
            if (selected is VoiceItem voice && AssociationTag.Voice(voice.Remark, out var serial) == AssociationTagState.Valid)
            {
                var related = tagged.Where(x => AssociationTag.Source(x.Item.Remark, out var source) == AssociationTagState.Valid && source!.Serial == serial).ToArray();
                foreach (var item in related)
                {
                    var registered = TachiePresetSourceAssociationTag.Read(item.Item.Remark, out _);
                    if (registered == AssociationTagState.Invalid)
                    {
                        skipped.Add("対象音声に不正な登録済み立ち絵プリセット関連付けがあります");
                        continue;
                    }
                    if (registered == AssociationTagState.Valid)
                        continue; // handled by RegisteredPresetAssociationResync
                    if (item.State == AssociationTagState.Valid) groups.Add(item.Tag!.Group);
                    else if (item.State == AssociationTagState.Invalid) skipped.Add("対象音声に不正なBundleメンバーがあります");
                    else if (item.Item is TachieFaceItem face && Equals(face.Character, voice.Character)) legacy.Add(face);
                }
                if (related.Length == 0) ignored++;
            }
            else if (selected is TachieFaceItem && AssociationTag.Source(selected.Remark, out _) != AssociationTagState.None) legacy.Add(selected);
            else ignored++;
        }
        var updates = new List<PlannedItemUpdate>(); var occupancy = timeline.Items.ToList(); var unchanged = 0;
        var guarded = new List<(IntentGeometry, Guid, string)>();
        foreach (var group in groups.Order())
        {
            try
            {
                var members = tagged.Where(x => x.State == AssociationTagState.Valid && x.Tag!.Group == group).OrderBy(x => x.Tag!.Index).ToArray();
                var descriptor = members[0].Tag!;
                if (members.Length != descriptor.Count || members.Any(x => !descriptor.SameGroup(x.Tag!)) ||
                    !members.Select(x => x.Tag!.Index).SequenceEqual(Enumerable.Range(0, descriptor.Count)))
                    throw new InvalidOperationException("メンバーの欠落・重複・コピー・設定不一致があります。復元を推測しません");
                if (members.Any(x => x.Item.Group != 0)) throw new InvalidOperationException("YMM4側でグループ化されています。解除してから再同期してください");
                if (AssociationTag.Source(members[0].Item.Remark, out var sourceTag) != AssociationTagState.Valid ||
                    members.Any(x => AssociationTag.Source(x.Item.Remark, out var other) != AssociationTagState.Valid || other!.Serial != sourceTag!.Serial))
                    throw new InvalidOperationException("音声への関連付けが不正または不一致です");
                var palette = settings.IntentPalettes.SingleOrDefault(x => x.Id == descriptor.Palette);
                var entry = palette?.Entries.SingleOrDefault(x => x.LibraryEntryId == descriptor.Entry);
                var reference = settings.Library.SingleOrDefault(x => x.Id == descriptor.Entry);
                if (palette == null || !palette.ExpressionCandidates || entry == null || reference == null)
                    throw new InvalidOperationException("元の表情パレット・演出登録がありません。別のセットへ推測して接続しません");
                var bundle = TemplateResolver.RequireBundle(reference);
                if (bundle.CharacterName == null || IntentAssociationTag.Hash(bundle) != descriptor.GeometryHash || bundle.Items.Count != members.Length ||
                    members.Where((x, i) => x.Item.GetType() != bundle.Items[i].GetType() || ItemCharacters.Get(x.Item)?.Name != ItemCharacters.Get(bundle.Items[i])?.Name).Any())
                    throw new InvalidOperationException("元テンプレートの構成・相対配置またはメンバーの種類・キャラクターが変更されています");
                var targets = timeline.Items.OfType<VoiceItem>().Where(x => x.Character?.Name == bundle.CharacterName &&
                    AssociationTag.Voice(x.Remark, out var id) == AssociationTagState.Valid && id == sourceTag!.Serial).Take(2).ToArray();
                if (targets.Length != 1) throw new InvalidOperationException("対応する音声が存在しないか、IDとキャラクター名が重複しています");
                var context = IntentSelectionContext.ForItems(timeline, [targets[0]]);
                var own = members.Select(x => x.Item).ToHashSet();
                var geometry = IntentPlacementGeometry.Prepare(timeline, context, palette, entry, settings.Library, occupancy.Where(x => !own.Contains(x)));
                if (geometry.Skipped) { skipped.Add($"{palette.Name}: 設定が「配置しない」のため既存Bundleを変更しませんでした"); continue; }
                var groupUpdates = new List<PlannedItemUpdate>();
                for (var i = 0; i < members.Length; i++)
                {
                    var member = members[i].Item; var planned = geometry.Items[i];
                    if (member.Frame != planned.Frame || member.Length != planned.Length || member.Layer != planned.Layer)
                        groupUpdates.Add(new(member, planned.Frame, planned.Length, planned.Layer, member.Remark ?? ""));
                }
                // Publish the reservation only after every member and the full saved relation succeed.
                updates.AddRange(groupUpdates); unchanged += members.Length - groupUpdates.Count;
                occupancy.RemoveAll(x => own.Contains(x)); occupancy.AddRange(geometry.Items);
                guarded.Add((geometry, palette.Id, JsonSerializer.Serialize(palette)));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or OverflowException)
            { skipped.Add($"Bundle {group.ToString("N")[..8]}: {ex.Message}"); }
        }
        var relativePlan = PlacementPlan.Create(timeline, [], updates);
        var old = ResyncPlan.Create(timeline, legacyPreset, legacy.ToArray());
        var combined = PlacementPlan.Combine(timeline, [relativePlan, old.Plan]);
        return new(new(combined, unchanged + old.Unchanged, ignored + old.Ignored, skipped.Concat(old.Skipped).ToArray()), guarded);
    }
    internal void ValidateCurrent(Timeline timeline, PlacerSettings settings)
    {
        foreach (var check in guarded)
        {
            check.Geometry.Context.ValidateCurrent(timeline, false);
            check.Geometry.Source.ValidateCurrent();
            if (JsonSerializer.Serialize(settings.IntentPalettes.SingleOrDefault(x => x.Id == check.Palette)) != check.Snapshot ||
                settings.Library.SingleOrDefault(x => x.Id == check.Geometry.Source.Entry.Id)?.Source != check.Geometry.Source.Entry.Source)
                throw new InvalidOperationException("計画後にパレット・元テンプレートの登録が変わりました。再同期していません。");
            IntentPlacementGeometry.ValidateCharacters(check.Geometry.Context, check.Geometry.Source);
        }
    }

    public ResyncPlan Commit(Timeline timeline, UndoRedoManager undo, PlacerSettings settings)
    {
        ValidateCurrent(timeline, settings);
        Result.Plan.Commit(timeline, undo);
        return Result;
    }
}
