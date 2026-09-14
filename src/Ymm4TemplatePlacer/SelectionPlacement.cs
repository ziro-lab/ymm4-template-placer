using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public static class TargetCompanionProfile
{
    public static (int Frame, int Length) Span(IItem target, SelectionPreset preset) =>
        SelectionPlacement.CheckedSpan((long)target.Frame + preset.StartOffset,
            (long)target.Frame + target.Length + preset.EndOffset);
}
public static class PointEmphasisProfile
{
    public static (int Frame, int Length) Span(IItem target, SelectionPreset preset)
    {
        // Integer frame anchors: fractional frames round down; End is the exclusive end.
        var start = (long)target.Frame + (long)target.Length * preset.AnchorPercent / 100 + preset.StartOffset;
        return SelectionPlacement.CheckedSpan(start, start + preset.Duration);
    }
}
public static class SelectionRangeProfile
{
    public static (int Frame, int Length) Span(IReadOnlyList<IItem> targets, SelectionPreset preset)
    {
        if (targets.Count < 2) throw new InvalidOperationException("範囲配置には2つ以上のItemを選択してください。");
        return SelectionPlacement.CheckedSpan(targets.Min(x => (long)x.Frame) - preset.HeadPadding,
            targets.Max(x => (long)x.Frame + x.Length) + preset.TailPadding);
    }
}

public sealed record SelectionPlacement(IItem Item, PlacementPlan Plan)
{
    public static (int Frame, int Length) CheckedSpan(long start, long end)
    {
        if (start < 0 || end <= start || end > int.MaxValue)
            throw new InvalidOperationException("配置範囲が不正です。開始は0以上、長さは1frame以上、終了は整数範囲内にしてください。Timelineは変更していません。");
        return ((int)start, (int)(end - start));
    }
    public static bool Supports(SelectionProfile profile, int count) => profile switch
    {
        SelectionProfile.TargetCompanion or SelectionProfile.PointEmphasis => count == 1,
        SelectionProfile.SelectionRange => count >= 2,
        SelectionProfile.Boundary => count == 2,
        _ => false
    };
    public static IReadOnlyList<SelectionProfileChoice> Profiles(int count) => count switch
    {
        1 => [new(SelectionProfile.TargetCompanion, "対象に合わせる"), new(SelectionProfile.PointEmphasis, "基準点を強調")],
        2 => [new(SelectionProfile.SelectionRange, "選択範囲を覆う"), new(SelectionProfile.Boundary, "2Itemの境界（許容差を確認）")],
        > 2 => [new(SelectionProfile.SelectionRange, "選択範囲を覆う")],
        _ => []
    };

    public static SelectionPlacement Create(Timeline timeline, LibraryEntry entry, SelectionPreset preset)
    {
        preset.Validate();
        var targets = timeline.SelectedItems.ToArray();
        if (!Supports(preset.Profile, targets.Length) || targets.Distinct().Count() != targets.Length)
            throw new InvalidOperationException("このProfileに必要な数のItemをTimelineで選択してください。対象・基準点は1つ、範囲は2つ以上、境界は2つだけです。");
        foreach (var target in targets)
        {
            if (!timeline.Items.Contains(target)) throw new InvalidOperationException("選択Itemが現在のTimelineにありません。選び直してください。");
            PlacementMath.ValidateSpan(target.Frame, target.Length);
        }
        var first = targets[0];
        var span = preset.Profile switch
        {
            SelectionProfile.TargetCompanion => TargetCompanionProfile.Span(first, preset),
            SelectionProfile.PointEmphasis => PointEmphasisProfile.Span(first, preset),
            SelectionProfile.SelectionRange => SelectionRangeProfile.Span(targets, preset),
            SelectionProfile.Boundary => BoundaryProfile.Span(targets, preset),
            _ => throw new InvalidOperationException("未対応の選択配置Profileです。")
        };
        var clone = TemplateResolver.Clone(entry);
        var sourceCharacter = ItemCharacters.Get(clone);
        foreach (var target in targets)
        {
            var character = ItemCharacters.Get(target);
            if (character != null && ((sourceCharacter != null && !Equals(sourceCharacter, character)) ||
                (entry.CharacterName != null && entry.CharacterName != character.Name)))
                throw new InvalidOperationException("Templateと対象のCharacterが違います。Characterを自動で書き換えず停止しました。");
        }
        clone.Frame = span.Frame; clone.Length = span.Length;
        clone.Layer = LayerPlanner.Find(clone.Frame, clone.Length, clone.Layer, preset.Layer,
            CharacterLayerMode.Base, sourceCharacter, timeline.Items);
        // Selection profiles add an independent item. Weak resync tags remain expression-only.
        clone.Remark = PluginRemarks.WithoutAssociation(clone.Remark);
        return new(clone, PlacementPlan.Create(timeline, [clone]));
    }
}
