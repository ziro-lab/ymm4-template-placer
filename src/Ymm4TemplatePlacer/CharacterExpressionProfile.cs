using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

/// <summary>A finite Voice-to-Face strategy over the existing add-only planner.</summary>
public static class CharacterExpressionProfile
{
    public static (int Frame, int Length) Span(VoiceSnapshot target,
        IReadOnlyList<VoiceSnapshot> orderedVoices, ExpressionPreset preset)
    {
        preset.Validate();
        PlacementMath.ValidateSpan(target.Frame, target.Length);
        var index = -1;
        for (var i = 0; i < orderedVoices.Count; i++)
            if (ReferenceEquals(orderedVoices[i].Voice, target.Voice)) { index = i; break; }
        if (index < 0 || target.Voice.Character == null)
            throw new InvalidOperationException("対象Voiceを現在の一覧で特定できません。更新してください。");
        long end = (long)target.Frame + target.Length;
        if (preset.Duration == ExpressionDuration.NextSameCharacter)
        {
            var next = orderedVoices.Skip(index + 1)
                .FirstOrDefault(x => Equals(x.Voice.Character, target.Voice.Character));
            // The immediate next same-Character Voice may overlap. Never shorten for that reason.
            if (next != null && next.Frame >= end && (long)next.Frame - end <= preset.MaxGap)
                end = next.Frame;
        }
        var start = (long)target.Frame + preset.StartOffset;
        end += preset.EndOffset;
        if (start < 0 || end > int.MaxValue || end <= start)
            throw new InvalidOperationException("表情Presetのoffsetで配置範囲が不正になります。開始0以上・長さ1以上にしてください。Timelineは変更していません。");
        return ((int)start, (int)(end - start));
    }

    public static PlacementPlan Create(Timeline timeline, IReadOnlyList<AssignmentRow> rows, ExpressionPreset preset)
    {
        PlacementEngine.ValidateSnapshot(timeline, rows.Select(x => x.Target).ToArray());
        preset.Validate();
        var ordered = rows.OrderBy(x => x.Frame).ThenBy(x => x.Target.Layer).ThenBy(x => x.No).ToArray();
        var voices = ordered.Select(x => x.Target).ToArray();
        var occupancy = timeline.Items.ToList();
        var additions = new List<IItem>();
        foreach (var row in ordered.Where(x => x.SelectedChoice.Template != null))
        {
            var clone = PlacementEngine.CloneForVoice(row.Target, row.SelectedChoice.Template!);
            var span = Span(row.Target, voices, preset);
            clone.Frame = span.Frame; clone.Length = span.Length;
            clone.Layer = LayerPlanner.Find(clone.Frame, clone.Length, clone.Layer, preset.Layer,
                CharacterLayerMode.Base, row.Target.Voice.Character, occupancy);
            occupancy.Add(clone); additions.Add(clone);
        }
        return PlacementPlan.Create(timeline, additions);
    }
}
