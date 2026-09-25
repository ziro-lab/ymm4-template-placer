using System.Text.Json.Serialization;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public enum CharacterLayerMode { Base, Front, Back }
public enum LayerSearchMode { Legacy, DoNotPlace, SearchUp, SearchDown }
public sealed record LayerModeChoice(CharacterLayerMode Value, string Label);
public sealed record LayerPolicy
{
    public bool UseTemplateLayer { get; init; } = true;
    public int Minimum { get; init; }
    public int Maximum { get; init; } = 99;
    public int Preferred { get; init; } = 15;
    // Absent in old JSON: preserve the original Template/range behavior exactly.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LayerSearchMode SearchMode { get; init; } = LayerSearchMode.Legacy;
    public void Validate()
    {
        if (!Enum.IsDefined(SearchMode))
            throw new InvalidOperationException("レイヤー探索設定が不正です。");
        if (Minimum < 0 || Maximum < Minimum || Maximum > 9999 || Preferred < Minimum || Preferred > Maximum)
            throw new InvalidOperationException("レイヤー探索範囲は0〜9999、優先レイヤーはその範囲内にしてください。9999はプラグインの探索上限です。");
    }
}
public static class LayerPlanner
{
    /// <summary>Plan against existing plus already-planned occupancy. Never move an occupant.</summary>
    public static int Find(int frame, int length, int templateLayer, LayerPolicy policy, CharacterLayerMode mode,
        Character? character, IEnumerable<IItem> occupancy, IItem? ignore = null)
    {
        PlacementMath.ValidateSpan(frame, length); policy.Validate();
        if (!Enum.IsDefined(mode) || templateLayer < 0) throw new InvalidOperationException("レイヤー設定が不正です。");
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
                throw new InvalidOperationException("指定した前面／背面方向の探索範囲に空きレイヤーがありません。範囲を広げるか基準配置を選んでください。反対方向へは配置していません。");
            }
        }
        // No same-Character context deliberately falls back to the normal Base policy.
        if (policy.SearchMode != LayerSearchMode.Legacy)
        {
            var first = policy.UseTemplateLayer ? templateLayer : policy.Preferred;
            if (first < policy.Minimum || first > policy.Maximum)
                throw new InvalidOperationException(policy.UseTemplateLayer
                    ? "元のレイヤーが保存済みの探索範囲外です。探索範囲を確認してください。"
                    : "指定レイヤーが保存済みの探索範囲外です。設定を確認してください。");
            if (Free(first)) return first;
            if (policy.SearchMode == LayerSearchMode.SearchUp)
                for (var layer = first - 1; layer >= policy.Minimum; layer--) if (Free(layer)) return layer;
            if (policy.SearchMode == LayerSearchMode.SearchDown)
                for (var layer = first + 1; layer <= policy.Maximum; layer++) if (Free(layer)) return layer;
            throw new InvalidOperationException(policy.SearchMode == LayerSearchMode.DoNotPlace
                ? $"指定レイヤー {first} は予定の長さの途中を含めて使用中です。配置していません。"
                : "指定方向の保存済み範囲に空きがありません。反対方向への配置や既存アイテムの移動・短縮はしていません。");
        }
        if (policy.UseTemplateLayer)
        {
            if (Free(templateLayer)) return templateLayer;
            throw new InvalidOperationException($"テンプレートのレイヤー {templateLayer} は予定の長さの途中を含めて使用中です。レイヤー範囲を使うか前面／背面を選んでください。");
        }
        if (Free(policy.Preferred)) return policy.Preferred;
        for (var layer = policy.Preferred + 1; layer <= policy.Maximum; layer++) if (Free(layer)) return layer;
        for (var layer = policy.Minimum; layer < policy.Preferred; layer++) if (Free(layer)) return layer;
        throw new InvalidOperationException("指定レイヤー範囲に空きがありません。既存アイテムは移動・短縮していません。");
    }
}
