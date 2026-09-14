using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public enum CharacterLayerMode { Base, Front, Back }
public sealed record LayerModeChoice(CharacterLayerMode Value, string Label);
public sealed record LayerPolicy
{
    public bool UseTemplateLayer { get; init; } = true;
    public int Minimum { get; init; }
    public int Maximum { get; init; } = 99;
    public int Preferred { get; init; } = 15;
    public void Validate()
    {
        if (Minimum < 0 || Maximum < Minimum || Maximum > 9999 || Preferred < Minimum || Preferred > Maximum)
            throw new InvalidOperationException("Layer探索範囲は0〜9999、優先Layerはその範囲内にしてください。9999はPluginの探索上限です。");
    }
}
public static class LayerPlanner
{
    /// <summary>Plan against existing plus already-planned occupancy. Never move an occupant.</summary>
    public static int Find(int frame, int length, int templateLayer, LayerPolicy policy, CharacterLayerMode mode,
        Character? character, IEnumerable<IItem> occupancy, IItem? ignore = null)
    {
        PlacementMath.ValidateSpan(frame, length); policy.Validate();
        if (!Enum.IsDefined(mode) || templateLayer < 0) throw new InvalidOperationException("Layer設定が不正です。");
        var items = occupancy.Where(x => !ReferenceEquals(x, ignore)).ToArray();
        bool Free(int layer) => !items.Any(x => x.Layer == layer && PlacementMath.Overlaps(frame, length, x.Frame, x.Length));
        if (mode != CharacterLayerMode.Base && character != null)
        {
            var context = items.Where(x => Equals(ItemCharacters.Get(x), character) && PlacementMath.Overlaps(frame, length, x.Frame, x.Length)).ToArray();
            if (context.Length > 0)
            {
                if (mode == CharacterLayerMode.Front)
                {
                    var start = Math.Max((long)context.Max(x => x.Layer) + 1, policy.Minimum);
                    for (var layer = start; layer <= policy.Maximum; layer++) if (Free((int)layer)) return (int)layer;
                }
                else
                {
                    var start = Math.Min((long)context.Min(x => x.Layer) - 1, policy.Maximum);
                    for (var layer = start; layer >= policy.Minimum; layer--) if (Free((int)layer)) return (int)layer;
                }
                throw new InvalidOperationException("指定した前面／背面方向の探索範囲に空きLayerがありません。範囲を広げるか基準配置を選んでください。反対方向へは配置していません。");
            }
        }
        // No same-Character context deliberately falls back to the normal Base policy.
        if (policy.UseTemplateLayer)
        {
            if (Free(templateLayer)) return templateLayer;
            throw new InvalidOperationException($"TemplateのLayer {templateLayer} は予定Lengthの途中を含めて使用中です。Layer範囲を使うか前面／背面を選んでください。");
        }
        if (Free(policy.Preferred)) return policy.Preferred;
        for (var layer = policy.Preferred + 1; layer <= policy.Maximum; layer++) if (Free(layer)) return layer;
        for (var layer = policy.Minimum; layer < policy.Preferred; layer++) if (Free(layer)) return layer;
        throw new InvalidOperationException("指定Layer範囲に空きがありません。既存Itemは移動・短縮していません。");
    }
}
