using System.Text.Json.Serialization;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public enum RelativeLayerDirection { Up, Down }
public enum LayerPlacementMode { RelativeToTarget = 0, Absolute = 1 }
public sealed record RelativeLayerPolicy
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LayerPlacementMode Mode { get; init; } = LayerPlacementMode.RelativeToTarget;
    public RelativeLayerDirection Direction { get; init; } = RelativeLayerDirection.Up;
    public int Offset { get; init; } = 1;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int AbsoluteLayer { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseSourceLayer { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SpecifiedLayerInitialized { get; init; }
    public int Minimum { get; init; }
    public int Maximum { get; init; } = 99;
    public void Validate()
    {
        if (!Enum.IsDefined(Mode) || !Enum.IsDefined(Direction) || Offset < 1 || Offset > 9999 ||
            AbsoluteLayer < 0 || AbsoluteLayer > 9999 || Minimum < 0 || Maximum < Minimum || Maximum > 9999 ||
            (Mode == LayerPlacementMode.Absolute && !UseSourceLayer && (AbsoluteLayer < Minimum || AbsoluteLayer > Maximum)))
            throw new InvalidOperationException("レイヤー配置は保存済み探索範囲内の0〜9999、上下の間隔は1〜9999段で設定してください。");
    }
}

/// <summary>Translates a whole normalized bundle; no member is independently relocated.</summary>
public static class BundleLayerPlanner
{
    public static IReadOnlyList<IItem> Plan(TemplateBundle source, int frame, int? singletonLength,
        int targetMinimumLayer, int targetMaximumLayer, RelativeLayerPolicy policy, IEnumerable<IItem> occupancy) =>
        Plan(MaterializedPlacementSource.FromTemplate(source), frame, singletonLength,
            targetMinimumLayer, targetMaximumLayer, policy, occupancy);

    internal static IReadOnlyList<IItem> Plan(MaterializedPlacementSource source, int frame, int? singletonLength,
        int targetMinimumLayer, int targetMaximumLayer, RelativeLayerPolicy policy, IEnumerable<IItem> occupancy)
    {
        ArgumentNullException.ThrowIfNull(source);
        policy.Validate();
        source.ValidateCurrent();
        if (targetMinimumLayer < 0 || targetMaximumLayer < targetMinimumLayer)
            throw new InvalidOperationException("基準アイテムのレイヤー範囲が不正です。");

        // MaterializedPlacementSource owns fresh normalized items for this one operation.
        // Geometry may mutate those items directly; no live source/template item is present here.
        var clones = source.Items;
        if (clones.Count != 1 && singletonLength.HasValue)
            throw new InvalidOperationException("複数アイテムの内部長さは変更できません。テンプレートの長さを維持してください。");
        foreach (var clone in clones)
        {
            var start = (long)frame + clone.Frame;
            var length = singletonLength ?? clone.Length;
            if (start < 0 || start > int.MaxValue) throw new InvalidOperationException("テンプレート全体の開始位置が範囲外です。");
            PlacementMath.ValidateSpan((int)start, length);
            clone.Frame = (int)start;
            clone.Length = length;
        }
        for (var i = 0; i < clones.Count; i++)
            for (var j = i + 1; j < clones.Count; j++)
                if (clones[i].Layer == clones[j].Layer &&
                    PlacementMath.Overlaps(clones[i].Frame, clones[i].Length, clones[j].Frame, clones[j].Length))
                    throw new InvalidOperationException("テンプレート内部で同じレイヤーのアイテムが重なっています。全件配置せず停止しました。");

        var occupied = occupancy.ToArray();
        var width = clones.Max(x => x.Layer);
        var step = policy.Direction == RelativeLayerDirection.Up ? -1 : 1;
        long first;
        if (policy.Mode == LayerPlacementMode.Absolute)
        {
            first = policy.UseSourceLayer ? source.BaseLayer : policy.AbsoluteLayer;
            if (first < policy.Minimum || first + width > policy.Maximum)
                throw new InvalidOperationException(policy.UseSourceLayer
                    ? "元のレイヤーでは配置Source全体が探索範囲に収まりません。探索範囲を確認してください。"
                    : "指定レイヤーでは配置Source全体が探索範囲に収まりません。");
        }
        else
        {
            first = policy.Direction == RelativeLayerDirection.Up
                ? (long)targetMinimumLayer - policy.Offset - width
                : (long)targetMaximumLayer + policy.Offset;
            // Clamp only farther in the requested direction; never move back toward/across the target band.
            first = step < 0 ? Math.Min(first, (long)policy.Maximum - width) : Math.Max(first, policy.Minimum);
        }
        for (var baseline = first; baseline >= policy.Minimum && baseline + width <= policy.Maximum; baseline += step)
        {
            var free = clones.All(clone => !occupied.Any(x => (long)x.Layer == baseline + clone.Layer &&
                PlacementMath.Overlaps(clone.Frame, clone.Length, x.Frame, x.Length)));
            if (!free) continue;
            foreach (var clone in clones) clone.Layer = checked((int)(baseline + clone.Layer));
            source.ValidateCurrent();
            return clones;
        }
        throw new InvalidOperationException("指定した方向の探索範囲にテンプレート全体を置ける空きがありません。範囲を広げてください。反対方向・一部だけには配置していません。");
    }
}
