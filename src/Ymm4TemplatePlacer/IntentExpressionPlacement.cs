using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed class IntentExpressionPlacement
{
    private readonly List<(IntentGeometry Geometry, Guid PaletteId, string Palette, Guid LibraryId, TemplateLocator Source)> guarded;
    public PlacementPlan Plan { get; }
    public long NextSerial { get; }
    public int Skipped { get; }
    private IntentExpressionPlacement(PlacementPlan plan, long nextSerial, int skipped,
        List<(IntentGeometry Geometry, Guid PaletteId, string Palette, Guid LibraryId, TemplateLocator Source)> guarded)
    { Plan = plan; NextSerial = nextSerial; Skipped = skipped; this.guarded = guarded; }

    public static IntentExpressionPlacement Create(Timeline timeline, IReadOnlyList<AssignmentRow> rows, PlacerSettings settings)
    {
        PlacementEngine.ValidateSnapshot(timeline, rows.Select(x => x.Target).ToArray());
        if (settings.NextAssociationId < 1) throw new InvalidOperationException("関連付け連番の設定が不正です。");
        var serial = settings.NextAssociationId; var skipped = 0;
        var additions = new List<IItem>(); var updates = new List<PlannedItemUpdate>();
        var guarded = new List<(IntentGeometry, Guid, string, Guid, TemplateLocator)>();
        var occupancy = timeline.Items.ToList();
        long Allocate()
        {
            foreach (var item in timeline.Items)
            {
                if (AssociationTag.Voice(item.Remark, out var targetId) == AssociationTagState.Valid && targetId >= serial)
                    serial = checked(targetId + 1);
                if (AssociationTag.Source(item.Remark, out var source) == AssociationTagState.Valid && source!.Serial >= serial)
                    serial = checked(source.Serial + 1);
            }
            if (serial == long.MaxValue) throw new InvalidOperationException("関連付けIDが上限です。配置していません。");
            return serial++;
        }
        foreach (var row in rows.Where(x => x.SelectedChoice.Template != null))
        {
            if (!row.SelectedChoice.IsAvailable || row.SelectedChoice.Template?.IntentSource is not { } reference)
                throw new InvalidOperationException("表情の選択元が現在のパレットと一致しません。候補を更新して選び直してください。");
            var current = reference.ResolveCurrent(settings); var voice = row.Target.Voice;
            if (current.Bundle.CharacterName != voice.Character?.Name)
                throw new InvalidOperationException("表情テンプレートと対象音声のキャラクター名が一致しません。");
            var context = IntentSelectionContext.ForItems(timeline, [voice]);
            var geometry = IntentPlacementGeometry.Prepare(timeline, context, current.Palette, current.Entry, settings.Library, occupancy);
            guarded.Add((geometry, current.Palette.Id, JsonSerializer.Serialize(current.Palette), current.Entry.LibraryEntryId, geometry.Source.Entry.Source));
            if (geometry.Skipped) { skipped++; continue; }
            var state = AssociationTag.Voice(voice.Remark, out var id);
            if (state == AssociationTagState.Invalid) throw new InvalidOperationException("対象音声の関連付けタグが不正または重複しています。");
            if (state == AssociationTagState.None)
            {
                id = Allocate(); updates.Add(PlannedItemUpdate.RemarkOnly(voice, PluginRemarks.Append(voice.Remark, AssociationTag.TargetLine(id))));
            }
            else if (timeline.Items.OfType<VoiceItem>().Count(x => x.Character?.Name == voice.Character?.Name &&
                AssociationTag.Voice(x.Remark, out var candidate) == AssociationTagState.Valid && candidate == id) != 1)
                throw new InvalidOperationException("関連付けIDとキャラクター名が一致する音声が複数あります。");
            if (timeline.Items.Any(x => AssociationTag.Source(x.Remark, out var source) == AssociationTagState.Valid && source!.Serial == id))
                throw new InvalidOperationException("この音声には関連する表情が既にあります。置き換えず、タイムラインで選択して再同期してください。");
            var group = Guid.NewGuid(); var hash = IntentAssociationTag.Hash(geometry.Source);
            for (var i = 0; i < geometry.Items.Count; i++)
            {
                var item = geometry.Items[i];
                var tag = new IntentAssociationTag(group, current.Palette.Id, current.Entry.LibraryEntryId, i, geometry.Items.Count, hash);
                item.Remark = PluginRemarks.Append(PluginRemarks.Append(PluginRemarks.Append(PluginRemarks.WithoutAssociation(item.Remark),
                    PlacementEngine.Marker), AssociationTag.SourceLine(id)), tag.Line);
                additions.Add(item); occupancy.Add(item);
            }
        }
        return new(PlacementPlan.Create(timeline, additions, updates), serial, skipped, guarded);
    }
    public int Commit(Timeline timeline, UndoRedoManager undo, PlacerSettings settings)
    {
        foreach (var check in guarded)
        {
            check.Geometry.Context.ValidateCurrent(timeline, false); check.Geometry.Source.ValidateCurrent();
            if (JsonSerializer.Serialize(settings.IntentPalettes.SingleOrDefault(x => x.Id == check.PaletteId)) != check.Palette ||
                settings.Library.SingleOrDefault(x => x.Id == check.LibraryId)?.Source != check.Source)
                throw new InvalidOperationException("計画後に表情パレットが変更されました。配置していません。");
            IntentPlacementGeometry.ValidateCharacters(check.Geometry.Context, check.Geometry.Source);
        }
        Plan.Commit(timeline, undo); return Plan.Count;
    }
}
