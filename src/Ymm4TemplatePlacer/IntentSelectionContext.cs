using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record IntentContextItem(IItem Item, string TypeKey, string? CharacterName, int Frame, int Length, int Layer)
{
    public long End => (long)Frame + Length;
    public static IntentContextItem Capture(IItem item) => new(item, IntentSelectionContext.TypeKey(item.GetType()), ItemCharacters.Get(item)?.Name, item.Frame, item.Length, item.Layer);
    public bool IsCurrent => TypeKey == IntentSelectionContext.TypeKey(Item.GetType()) && CharacterName == ItemCharacters.Get(Item)?.Name &&
        Frame == Item.Frame && Length == Item.Length && Layer == Item.Layer;
}

/// <summary>One public selection snapshot also freezes the neighborhood used by relation lookup.</summary>
public sealed class IntentSelectionContext
{
    private readonly Timeline owner;
    private readonly object timelineItems;
    public IReadOnlyList<IntentContextItem> Selected { get; }
    public IReadOnlyList<IntentContextItem> Scene { get; }
    public int Start => Selected.Select(x => x.Frame).DefaultIfEmpty(0).Min();
    public long End => Selected.Select(x => x.End).DefaultIfEmpty(0).Max();
    public int MinimumLayer => Selected.Select(x => x.Layer).DefaultIfEmpty(0).Min();
    public int MaximumLayer => Selected.Select(x => x.Layer).DefaultIfEmpty(0).Max();
    public string? UniformType => Selected.Select(x => x.TypeKey).Distinct(StringComparer.Ordinal).Count() == 1 ? Selected[0].TypeKey : null;
    public string? UniformCharacter => Selected.Count != 0 && Selected[0].CharacterName != null && Selected.All(x => x.CharacterName == Selected[0].CharacterName) ? Selected[0].CharacterName : null;
    public static string TypeKey(Type type) => $"{type.FullName ?? type.Name}, {type.Assembly.GetName().Name}";
    private IntentSelectionContext(Timeline timeline, IReadOnlyList<IItem> selected)
    {
        owner = timeline; timelineItems = timeline.Items;
        if (selected.Count > 2048 || selected.Distinct().Count() != selected.Count || selected.Any(x => !timeline.Items.Contains(x)))
            throw new InvalidOperationException("選択アイテムが現在のシーンに一意に存在しません。選び直してください。");
        var scene = timeline.Items.Select(IntentContextItem.Capture).ToArray();
        Scene = Array.AsReadOnly(scene);
        var wanted = selected.ToHashSet();
        Selected = Array.AsReadOnly(scene.Where(x => wanted.Contains(x.Item)).OrderBy(x => x.Frame).ThenBy(x => x.Layer).ToArray());
        foreach (var item in Selected) PlacementMath.ValidateSpan(item.Frame, item.Length);
    }
    public static IntentSelectionContext Capture(Timeline timeline) => new(timeline, timeline.SelectedItems.ToArray());
    public static IntentSelectionContext ForItems(Timeline timeline, IReadOnlyList<IItem> selected) => new(timeline, selected);
    public void ValidateCurrent(Timeline timeline, bool requireSelection = true)
    {
        if (!ReferenceEquals(owner, timeline) || !ReferenceEquals(timelineItems, timeline.Items) || Scene.Any(x => !x.IsCurrent) ||
            (requireSelection && (timeline.SelectedItems.Count != Selected.Count || !timeline.SelectedItems.ToHashSet().SetEquals(Selected.Select(x => x.Item)))))
            throw new InvalidOperationException("基準アイテム・周囲のアイテム・選択が変わりました。配置せず停止しました。もう一度クリックしてください。");
    }
}
