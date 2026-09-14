using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record ResyncPlan(PlacementPlan Plan, int Unchanged, int Ignored, IReadOnlyList<string> Skipped)
{
    public static ResyncPlan Create(Timeline timeline, ExpressionPreset preset)
    {
        preset.Validate();
        var candidates = new HashSet<TachieFaceItem>();
        var skipped = new List<string>();
        var ignored = 0;
        foreach (var selected in timeline.SelectedItems.Distinct())
        {
            if (!timeline.Items.Contains(selected)) { skipped.Add("現在のタイムラインにない選択アイテム"); continue; }
            if (selected is VoiceItem voice)
            {
                var state = AssociationTag.Voice(voice.Remark, out var id);
                if (state == AssociationTagState.None) { ignored++; continue; }
                if (state == AssociationTagState.Invalid) { skipped.Add("音声の関連付けタグが不正または重複"); continue; }
                foreach (var face in timeline.Items.OfType<TachieFaceItem>().Where(x => Equals(x.Character, voice.Character) &&
                    AssociationTag.Source(x.Remark, out var source) == AssociationTagState.Valid && source!.Serial == id))
                    candidates.Add(face);
            }
            else if (selected is TachieFaceItem face && AssociationTag.Source(face.Remark, out _) != AssociationTagState.None)
                candidates.Add(face);
            else ignored++;
        }
        var snapshots = VoiceSnapshot.Capture(timeline);
        var occupancy = timeline.Items.ToList();
        var updates = new List<PlannedItemUpdate>();
        var unchanged = 0;
        foreach (var face in timeline.Items.OfType<TachieFaceItem>().Where(candidates.Contains).OrderBy(x => x.Frame).ThenBy(x => x.Layer))
        {
            try
            {
                if (AssociationTag.Source(face.Remark, out var source) != AssociationTagState.Valid || face.Character == null)
                    throw new InvalidOperationException("関連表情のタグまたはキャラクターが不正");
                var targets = snapshots.Where(x => Equals(x.Voice.Character, face.Character) &&
                    AssociationTag.Voice(x.Voice.Remark, out var id) == AssociationTagState.Valid && id == source!.Serial).Take(2).ToArray();
                if (targets.Length != 1) throw new InvalidOperationException(targets.Length == 0 ? "対応音声が見つかりません" : "対応音声が複数あります");
                if (face.Group != 0) throw new InvalidOperationException("関連表情がグループ化されています。YMM4でグループを解除してから再同期してください");
                var span = CharacterExpressionProfile.Span(targets[0], snapshots, preset);
                // No historical Template snapshot is stored. Template-Layer policy retains this Item's current Layer on resync.
                var layer = LayerPlanner.Find(span.Frame, span.Length, face.Layer, preset.Layer, CharacterLayerMode.Base, face.Character, occupancy, face);
                if (span.Frame == face.Frame && span.Length == face.Length && layer == face.Layer) { unchanged++; continue; }
                var reservation = face.GetClone() as TachieFaceItem;
                if (reservation == null || ReferenceEquals(reservation, face)) throw new InvalidOperationException("関連表情の予約領域を独立して作れません");
                reservation.Frame = span.Frame; reservation.Length = span.Length; reservation.Layer = layer;
                updates.Add(new(face, span.Frame, span.Length, layer, face.Remark ?? ""));
                occupancy.Remove(face); occupancy.Add(reservation);
            }
            catch (InvalidOperationException ex) { skipped.Add($"開始 {face.Frame} / レイヤー {face.Layer}: {ex.Message}"); }
        }
        return new(PlacementPlan.Create(timeline, [], updates), unchanged, ignored, skipped);
    }
}
