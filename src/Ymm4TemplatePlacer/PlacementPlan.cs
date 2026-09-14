using System.Collections.Immutable;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed record PlannedItemUpdate(IItem Item, int Frame, int Length, int Layer, string Remark)
{
    public static PlannedItemUpdate RemarkOnly(IItem item, string remark) => new(item, item.Frame, item.Length, item.Layer, remark);
}

/// <summary>Preflighted add/update operation. No existing item is removed; Undo belongs to YMM4.</summary>
public sealed class PlacementPlan
{
    private readonly ImmutableList<IItem> before;
    private readonly ImmutableList<IItem> after;
    private readonly PlannedItemUpdate[] updates;
    private readonly (IItem Item, int Frame, int Length, int Layer, int Group, string Remark, Character? Character)[] observed;
    public int Count { get; }
    public int UpdateCount => updates.Length;

    private PlacementPlan(Timeline timeline, IReadOnlyList<IItem> additions, IReadOnlyList<PlannedItemUpdate>? itemUpdates)
    {
        before = timeline.Items;
        Count = additions.Count;
        updates = itemUpdates?.ToArray() ?? [];
        if (additions.Distinct().Count() != Count || additions.Any(before.Contains))
            throw new InvalidOperationException("追加Itemが独立していません。Timelineは変更していません。");
        if (updates.Select(x => x.Item).Distinct().Count() != updates.Length || updates.Any(x => !before.Contains(x.Item)))
            throw new InvalidOperationException("更新対象が現在のTimelineで一意ではありません。Timelineは変更していません。");
        foreach (var item in additions) PlacementMath.ValidateSpan(item.Frame, item.Length);
        foreach (var update in updates) PlacementMath.ValidateSpan(update.Frame, update.Length);
        if (additions.Any(x => x.Layer < 0) || updates.Any(x => x.Layer < 0))
            throw new InvalidOperationException("配置Layerは0以上にしてください。");
        var geometryUpdates = updates.Where(x => x.Frame != x.Item.Frame || x.Length != x.Item.Length || x.Layer != x.Item.Layer).ToArray();
        var moving = geometryUpdates.Select(x => x.Item).ToHashSet();
        var occupancy = before.Where(x => !moving.Contains(x)).Select(x => (x.Frame, x.Length, x.Layer)).ToList();
        void Reserve(int frame, int length, int layer)
        {
            if (occupancy.Any(x => x.Layer == layer && PlacementMath.Overlaps(x.Frame, x.Length, frame, length)))
                throw new InvalidOperationException($"配置先が重なります（Frame={frame}, Layer={layer}）。空きLayerを指定してください。Timelineは変更していません。");
            occupancy.Add((frame, length, layer));
        }
        foreach (var update in geometryUpdates) Reserve(update.Frame, update.Length, update.Layer);
        foreach (var item in additions) Reserve(item.Frame, item.Length, item.Layer);
        after = before.AddRange(additions);
        observed = before.Concat(additions).Select(x => (x, x.Frame, x.Length, x.Layer, x.Group, x.Remark, ItemCharacters.Get(x))).ToArray();
    }
    public static PlacementPlan Create(Timeline timeline, IReadOnlyList<IItem> additions, IReadOnlyList<PlannedItemUpdate>? updates = null) => new(timeline, additions, updates);
    public int Commit(Timeline timeline, UndoRedoManager undo)
    {
        if (!ReferenceEquals(before, timeline.Items) || observed.Any(x => x.Item.Frame != x.Frame || x.Item.Length != x.Length || x.Item.Layer != x.Layer ||
            x.Item.Group != x.Group || x.Item.Remark != x.Remark || !Equals(ItemCharacters.Get(x.Item), x.Character)))
            throw new InvalidOperationException("計画後にTimelineまたは配置Itemが変更されました。操作をやり直してください。");
        if (Count == 0 && updates.Length == 0) return 0;
        undo.Record();
        foreach (var update in updates)
        {
            update.Item.Frame = update.Frame; update.Item.Length = update.Length;
            update.Item.Layer = update.Layer; update.Item.Remark = update.Remark;
        }
        timeline.Items = after;
        timeline.RefreshTimelineLengthAndMaxLayer();
        undo.Record();
        return Count + updates.Length;
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
