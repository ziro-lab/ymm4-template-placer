using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>
/// Geometry-only Resync for Set-owned registered Tachie Preset expressions.
/// The live FaceParameter is validated but never replaced/reapplied.
/// </summary>
internal sealed class RegisteredPresetAssociationResync
{
    private sealed record Guard(
        VoiceItem Voice,
        ManagedExpressionAssociation Existing,
        PlacementSourceGeometry Geometry,
        Guid PaletteId,
        string PaletteSnapshot,
        Guid SourceId,
        string SourceHash);

    private readonly IReadOnlyList<Guard> guards;
    public ResyncPlan Result { get; }

    private RegisteredPresetAssociationResync(ResyncPlan result, IReadOnlyList<Guard> guards)
    {
        Result = result;
        this.guards = guards;
    }

    internal static async Task<RegisteredPresetAssociationResync> CreateAsync(
        Timeline timeline,
        PlacerSettings settings,
        Func<Character, TachiePresetProbeTarget> resolver,
        IReadOnlyList<IItem>? selectedItems = null,
        CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(resolver);

        var selected = (selectedItems ?? timeline.SelectedItems.ToArray()).Distinct().ToArray();
        var groups = new HashSet<Guid>();
        var skipped = new List<string>();

        foreach (var item in selected)
        {
            token.ThrowIfCancellationRequested();
            if (!timeline.Items.Contains(item))
            {
                skipped.Add("シーンに存在しない選択アイテム");
                continue;
            }

            if (item is VoiceItem voice)
            {
                var voiceState = AssociationTag.Voice(voice.Remark, out var serial);
                if (voiceState == AssociationTagState.Invalid)
                {
                    skipped.Add("対象音声の関連付けタグが不正または重複しています");
                    continue;
                }
                if (voiceState != AssociationTagState.Valid) continue;

                foreach (var related in timeline.Items)
                {
                    var sourceState = AssociationTag.Source(related.Remark, out var source);
                    if (sourceState != AssociationTagState.Valid || source!.Serial != serial) continue;
                    var registeredState = TachiePresetSourceAssociationTag.Read(related.Remark, out var registered);
                    if (registeredState == AssociationTagState.Invalid)
                    {
                        skipped.Add("対象音声に不正な登録済み立ち絵プリセット関連付けがあります");
                        continue;
                    }
                    if (registeredState == AssociationTagState.Valid)
                        groups.Add(registered!.Group);
                }
                continue;
            }

            var state = TachiePresetSourceAssociationTag.Read(item.Remark, out var tag);
            if (state == AssociationTagState.Invalid)
            {
                skipped.Add("選択アイテムの登録済み立ち絵プリセット関連付けが不正です");
                continue;
            }
            if (state == AssociationTagState.Valid)
                groups.Add(tag!.Group);
        }

        var occupancy = timeline.Items.ToList();
        var updates = new List<PlannedItemUpdate>();
        var guards = new List<Guard>();
        var unchanged = 0;

        foreach (var group in groups.Order())
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var members = timeline.Items.Select(item =>
                    (Item: item, State: TachiePresetSourceAssociationTag.Read(item.Remark, out var tag), Tag: tag))
                    .Where(x => x.State == AssociationTagState.Valid && x.Tag!.Group == group)
                    .ToArray();

                if (members.Length != 1)
                    throw new InvalidOperationException("登録済み立ち絵プリセットBundleに欠落・重複またはコピーがあります");
                var member = members[0];
                var descriptor = member.Tag!;
                if (member.Item is not TachieFaceItem face || face.TachieFaceParameter == null)
                    throw new InvalidOperationException("登録済み立ち絵プリセットBundleを単一表情として確認できません");
                if (face.Group != 0)
                    throw new InvalidOperationException("YMM4側でグループ化されています。解除してから再同期してください");

                if (AssociationTag.Source(face.Remark, out var sourceTag) != AssociationTagState.Valid)
                    throw new InvalidOperationException("音声への関連付けが不正です");

                var sourceMatches = settings.TachiePresetSources.Where(x => x.Id == descriptor.Source).Take(2).ToArray();
                if (sourceMatches.Length != 1)
                    throw new InvalidOperationException("元の登録済み立ち絵プリセットSourceがありません");
                var source = sourceMatches[0];
                if (source.SemanticHash() != descriptor.SourceHash)
                    throw new InvalidOperationException("登録済み立ち絵プリセットSourceの内容が関連付け時から変更されています");

                var palette = settings.IntentPalettes.SingleOrDefault(x => x.Id == descriptor.Palette);
                var entry = palette?.Entries.SingleOrDefault(x => x.SourceId == descriptor.Source);
                if (palette == null || !palette.ExpressionCandidates || entry == null)
                    throw new InvalidOperationException("元の表情Set・Source所属がありません。別のSetへ推測して接続しません");

                var targets = timeline.Items.OfType<VoiceItem>()
                    .Where(x => x.CharacterName == source.CharacterName &&
                        AssociationTag.Voice(x.Remark, out var serial) == AssociationTagState.Valid &&
                        serial == sourceTag!.Serial)
                    .Take(2)
                    .ToArray();
                if (targets.Length != 1)
                    throw new InvalidOperationException("対応する音声が存在しないか、IDとキャラクター名が重複しています");

                var voice = targets[0];
                var character = voice.Character
                    ?? throw new InvalidOperationException("対応音声のキャラクターを取得できません");

                var existing = ManagedExpressionReader.Read(timeline, voice);
                if (existing.Bundle?.Descriptor is not
                    { Kind: ManagedExpressionSourceKind.RegisteredTachiePreset, RegisteredTachiePreset: { } liveDescriptor } ||
                    liveDescriptor.Group != group)
                    throw new InvalidOperationException("現在のPlugin-managed表情Bundleを登録済みPreset v2として一意に確認できません");
                ManagedExpressionSafety.ValidatePresetState(existing.Bundle, token);

                // Re-resolve and apply only to a fresh off-Timeline item so the saved source/candidate
                // must still be valid. The live FaceParameter is never overwritten during Resync.
                var materialized = await TachiePresetSourceMaterializer.MaterializeAsync(
                    source, character, resolver, token);

                var context = IntentSelectionContext.ForItems(timeline, [voice]);
                var own = new HashSet<IItem>(ReferenceEqualityComparer.Instance) { face };
                var geometry = IntentPlacementGeometry.Prepare(
                    timeline,
                    context,
                    palette,
                    entry,
                    materialized,
                    occupancy.Where(x => !own.Contains(x)));

                if (geometry.Skipped)
                {
                    skipped.Add($"{palette.Name}: 設定が「配置しない」のため既存表情を変更しませんでした");
                    continue;
                }
                if (geometry.Items.Count != 1)
                    throw new InvalidOperationException("登録済みPreset Sourceの再同期Geometryが単一表情になりません");

                var planned = geometry.Items[0];
                if (face.Frame == planned.Frame && face.Length == planned.Length && face.Layer == planned.Layer)
                {
                    unchanged++;
                }
                else
                {
                    updates.Add(new PlannedItemUpdate(
                        face,
                        planned.Frame,
                        planned.Length,
                        planned.Layer,
                        face.Remark ?? ""));
                }

                occupancy.RemoveAll(x => ReferenceEquals(x, face));
                occupancy.Add(planned);
                guards.Add(new Guard(
                    voice,
                    existing,
                    geometry,
                    palette.Id,
                    JsonSerializer.Serialize(palette),
                    source.Id,
                    source.SemanticHash()));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or OverflowException)
            {
                skipped.Add($"Preset {group.ToString("N")[..8]}: {ex.Message}");
            }
        }

        return new RegisteredPresetAssociationResync(
            new ResyncPlan(
                PlacementPlan.Create(timeline, [], updates),
                unchanged,
                0,
                skipped),
            guards);
    }

    internal void ValidateCurrent(
        Timeline timeline,
        PlacerSettings settings,
        CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        foreach (var guard in guards)
        {
            guard.Geometry.Context.ValidateCurrent(timeline, false);
            guard.Geometry.Source.ValidateCurrent();

            var current = ManagedExpressionReader.Read(timeline, guard.Voice);
            if (!ManagedExpressionSafety.Same(guard.Existing, current))
                throw new InvalidOperationException("計画後に現在の関連表情が変わりました。再同期していません。");
            ManagedExpressionSafety.ValidatePresetState(current.Bundle, token);

            var palette = settings.IntentPalettes.SingleOrDefault(x => x.Id == guard.PaletteId);
            var source = settings.TachiePresetSources.SingleOrDefault(x => x.Id == guard.SourceId);
            if (palette == null ||
                JsonSerializer.Serialize(palette) != guard.PaletteSnapshot ||
                source == null ||
                source.SemanticHash() != guard.SourceHash)
                throw new InvalidOperationException("計画後に表情Setまたは登録済み立ち絵プリセットSourceが変更されました。再同期していません。");
        }
    }

    internal ResyncPlan Commit(
        Timeline timeline,
        UndoRedoManager undo,
        PlacerSettings settings,
        CancellationToken token = default)
    {
        ValidateCurrent(timeline, settings, token);
        Result.Plan.Commit(timeline, undo);
        return Result;
    }
}
