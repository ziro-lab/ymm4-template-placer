using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public static class BoundaryProfile
{
    public static (int Frame, int Length) Span(IReadOnlyList<IItem> targets, SelectionPreset preset)
    {
        if (targets.Count != 2) throw new InvalidOperationException("境界配置は2つのアイテムだけを選択してください。");
        var pair = targets.OrderBy(x => x.Frame).ToArray();
        if (ReferenceEquals(pair[0], pair[1]) || pair[0].Frame == pair[1].Frame)
            throw new InvalidOperationException("開始位置が同じアイテムでは前後の境界を特定できません。推測せず停止しました。");
        var cut = (long)pair[0].Frame + pair[0].Length;
        var gap = (long)pair[1].Frame - cut;
        if (preset.Tolerance < 0 || Math.Abs(gap) > preset.Tolerance)
            throw new InvalidOperationException($"選択した2アイテムの境界差は{gap}フレームで、許容差{preset.Tolerance}を超えています。別の境界は検索しません。");
        // Agreed cut is the earlier item's exclusive end, even for an allowed small gap/overlap.
        var start = cut + preset.StartOffset;
        return SelectionPlacement.CheckedSpan(start, start + preset.Duration);
    }
}
