using System.Collections.Immutable;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>A completely preflighted add operation. No existing item is removed.</summary>
public sealed class PlacementPlan
{
    private readonly ImmutableList<IItem> before;
    private readonly ImmutableList<IItem> after;
    private readonly (IItem Item, int Frame, int Length, int Layer)[] observed;
    public int Count { get; }

    private PlacementPlan(Timeline timeline, IReadOnlyList<IItem> additions)
    {
        before = timeline.Items;
        Count = additions.Count;
        if (additions.Distinct().Count() != Count || additions.Any(before.Contains))
            throw new InvalidOperationException("追加Itemが独立していません。Timelineは変更していません。");
        foreach (var item in additions) PlacementMath.ValidateSpan(item.Frame, item.Length);
        if (additions.Any(x => x.Layer < 0)) throw new InvalidOperationException("配置Layerは0以上にしてください。");
        var occupancy = before.ToList();
        foreach (var item in additions)
        {
            if (occupancy.Any(x => x.Layer == item.Layer && PlacementMath.Overlaps(x.Frame, x.Length, item.Frame, item.Length)))
                throw new InvalidOperationException($"配置先が重なります（Frame={item.Frame}, Layer={item.Layer}）。空きLayerを指定してください。Timelineは変更していません。");
            occupancy.Add(item);
        }
        after = before.AddRange(additions);
        observed = before.Concat(additions).Select(x => (x, x.Frame, x.Length, x.Layer)).ToArray();
    }
    public static PlacementPlan Create(Timeline timeline, IReadOnlyList<IItem> additions) => new(timeline, additions);
    public int Commit(Timeline timeline, UndoRedoManager undo)
    {
        if (!ReferenceEquals(before, timeline.Items) || observed.Any(x => x.Item.Frame != x.Frame || x.Item.Length != x.Length || x.Item.Layer != x.Layer))
            throw new InvalidOperationException("計画後にTimelineまたは配置Itemが変更されました。配置をやり直してください。");
        if (Count == 0) return 0;
        undo.Record();
        timeline.Items = after;
        timeline.RefreshTimelineLengthAndMaxLayer();
        undo.Record();
        return Count;
    }
}

public static class PlacementMath
{
    public static void ValidateSpan(int frame, int length)
    {
        if (frame < 0 || length <= 0 || (long)frame + length > int.MaxValue)
            throw new InvalidOperationException("配置Frame / Lengthが範囲外です。Frameは0以上、Lengthは1以上にしてください。");
    }
    public static bool Overlaps(int frameA, int lengthA, int frameB, int lengthB) =>
        (long)frameA < (long)frameB + lengthB && (long)frameB < (long)frameA + lengthA;
}
