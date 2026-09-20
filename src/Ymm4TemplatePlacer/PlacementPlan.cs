using System.Collections.Immutable;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed record PlannedItemUpdate(IItem Item, int Frame, int Length, int Layer, string Remark)
{
    public static PlannedItemUpdate RemarkOnly(IItem item, string remark) => new(item, item.Frame, item.Length, item.Layer, remark);
}

/// <summary>
/// Preflighted add/update operation. Removal is opt-in and is used only by the specialist expression replacement path.
/// Ordinary placement callers remain add-only. Native Undo belongs to YMM4.
/// </summary>
public sealed class PlacementPlan
{
    private readonly ImmutableList<IItem> before;
    private readonly ImmutableList<IItem> after;
    private readonly IItem[] additions;
    private readonly PlannedItemUpdate[] updates;
    private readonly IItem[] removals;
    private readonly (IItem Item, int Frame, int Length, int Layer, int Group, string Remark, Character? Character)[] observed;
    public int Count => additions.Length;
    public int UpdateCount => updates.Length;
    public int RemoveCount => removals.Length;
    public int ChangeCount => Count + UpdateCount + RemoveCount;
    private PlacementPlan(Timeline timeline, IReadOnlyList<IItem> itemAdditions, IReadOnlyList<PlannedItemUpdate>? itemUpdates, IReadOnlyList<IItem>? itemRemovals)
    {
        before = timeline.Items; additions = itemAdditions.ToArray(); updates = itemUpdates?.ToArray() ?? []; removals = itemRemovals?.ToArray() ?? [];
        if (additions.Distinct().Count() != additions.Length || additions.Any(before.Contains))
            throw new InvalidOperationException("追加アイテムが独立していません。タイムラインは変更していません。");
        if (updates.Select(x => x.Item).Distinct().Count() != updates.Length || updates.Any(x => !before.Contains(x.Item)))
            throw new InvalidOperationException("更新対象が現在のタイムラインで一意ではありません。タイムラインは変更していません。");
        if (removals.Distinct().Count() != removals.Length || removals.Any(x => !before.Contains(x)) || updates.Any(x => removals.Contains(x.Item)))
            throw new InvalidOperationException("削除対象が現在のタイムラインで一意ではないか、更新対象と重複しています。タイムラインは変更していません。");
        foreach (var item in additions) PlacementMath.ValidateSpan(item.Frame, item.Length);
        foreach (var update in updates) PlacementMath.ValidateSpan(update.Frame, update.Length);
        if (additions.Any(x => x.Layer < 0) || updates.Any(x => x.Layer < 0)) throw new InvalidOperationException("配置レイヤーは0以上にしてください。");
        var geometryUpdates = updates.Where(x => x.Frame != x.Item.Frame || x.Length != x.Item.Length || x.Layer != x.Item.Layer).ToArray();
        var moving = geometryUpdates.Select(x => x.Item).Concat(removals).ToHashSet();
        var occupancy = before.Where(x => !moving.Contains(x)).Select(x => (x.Frame, x.Length, x.Layer)).ToList();
        void Reserve(int frame, int length, int layer)
        {
            if (occupancy.Any(x => x.Layer == layer && PlacementMath.Overlaps(x.Frame, x.Length, frame, length)))
                throw new InvalidOperationException($"配置先が重なります（開始={frame}, レイヤー={layer}）。空きレイヤーを指定してください。タイムラインは変更していません。");
            occupancy.Add((frame, length, layer));
        }
        foreach (var update in geometryUpdates) Reserve(update.Frame, update.Length, update.Layer);
        foreach (var item in additions) Reserve(item.Frame, item.Length, item.Layer);
        var removing = removals.ToHashSet();
        after = before.Where(x => !removing.Contains(x)).ToImmutableList().AddRange(additions);
        observed = before.Concat(additions).Select(x => (x, x.Frame, x.Length, x.Layer, x.Group, x.Remark, ItemCharacters.Get(x))).ToArray();
    }
    public static PlacementPlan Create(Timeline timeline, IReadOnlyList<IItem> additions, IReadOnlyList<PlannedItemUpdate>? updates = null,
        IReadOnlyList<IItem>? removals = null) => new(timeline, additions, updates, removals);
    public static PlacementPlan Combine(Timeline timeline, IReadOnlyList<PlacementPlan> plans)
    {
        foreach (var plan in plans) plan.ValidateCurrent(timeline);
        return Create(timeline, plans.SelectMany(x => x.additions).ToArray(), plans.SelectMany(x => x.updates).ToArray(),
            plans.SelectMany(x => x.removals).ToArray());
    }
    private void ValidateCurrent(Timeline timeline)
    {
        if (!ReferenceEquals(before, timeline.Items) || observed.Any(x => x.Item.Frame != x.Frame || x.Item.Length != x.Length || x.Item.Layer != x.Layer ||
            x.Item.Group != x.Group || x.Item.Remark != x.Remark || !Equals(ItemCharacters.Get(x.Item), x.Character)))
            throw new InvalidOperationException("計画後にタイムラインまたは配置アイテムが変更されました。操作をやり直してください。");
    }
    private int Apply(Timeline timeline)
    {
        foreach (var update in updates)
        {
            update.Item.Frame = update.Frame; update.Item.Length = update.Length;
            update.Item.Layer = update.Layer; update.Item.Remark = update.Remark;
        }
        timeline.Items = after; timeline.RefreshTimelineLengthAndMaxLayer();
        return ChangeCount;
    }
    public int Commit(Timeline timeline, UndoRedoManager undo)
    {
        ValidateCurrent(timeline);
        if (ChangeCount == 0) return 0;
        undo.Record();
        var count = Apply(timeline);
        undo.Record();
        return count;
    }
    internal int CommitWithinOpenRecord(Timeline timeline)
    {
        ValidateCurrent(timeline);
        return ChangeCount == 0 ? 0 : Apply(timeline);
    }
}
public static class PlacementMath
{
    public static void ValidateSpan(int frame, int length)
    {
        if (frame < 0 || length <= 0 || (long)frame + length > int.MaxValue)
            throw new InvalidOperationException("配置の開始位置 / 長さが範囲外です。開始位置は0以上、長さは1以上にしてください。");
    }
    public static bool Overlaps(int frameA, int lengthA, int frameB, int lengthB) =>
        (long)frameA < (long)frameB + lengthB && (long)frameB < (long)frameA + lengthA;
}
