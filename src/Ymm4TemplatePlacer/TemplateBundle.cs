using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed record TemplateSourceResolution(TemplateReferenceState State, ItemTemplate? Template)
{
    public string Message => new TemplateResolution(State, Template, null).Message;
}

public sealed record BundleResolution(TemplateReferenceState State, TemplateBundle? Bundle)
{
    public string Message => State == TemplateReferenceState.Unsupported
        ? "⚠ 空または不正な複数アイテムテンプレートです。YMM4側で確認してください。"
        : new TemplateResolution(State, Bundle?.Template, null).Message;
}

/// <summary>A live source snapshot, never serialized into plugin settings.</summary>
public sealed class TemplateBundle
{
    private readonly (IItem Item, int Frame, int Length, int Layer, int Group, string Remark, Character? Character)[] observed;
    public LibraryEntry Entry { get; }
    public ItemTemplate Template { get; }
    public IReadOnlyList<IItem> Items { get; }
    public int OriginFrame { get; }
    public int MinimumLayer { get; }
    public int MaximumLayer { get; }
    public int Span { get; }
    public string? CharacterName { get; }
    public bool HasFace => Items.Any(x => x is TachieFaceItem);

    internal TemplateBundle(LibraryEntry entry, ItemTemplate template, IItem[] items)
    {
        Entry = entry; Template = template; Items = Array.AsReadOnly(items);
        OriginFrame = items.Min(x => x.Frame);
        MinimumLayer = items.Min(x => x.Layer); MaximumLayer = items.Max(x => x.Layer);
        Span = checked((int)(items.Max(x => (long)x.Frame + x.Length) - OriginFrame));
        var names = items.Select(x => ItemCharacters.Get(x)?.Name).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
        CharacterName = names.Length == 1 ? names[0] : null;
        observed = items.Select(x => (x, x.Frame, x.Length, x.Layer, x.Group, x.Remark, ItemCharacters.Get(x))).ToArray();
    }

    public void ValidateCurrent()
    {
        var resolved = TemplateResolver.ResolveTemplate(Entry);
        if (resolved.State != TemplateReferenceState.Resolved || !ReferenceEquals(resolved.Template, Template) ||
            !Template.Items.SequenceEqual(Items) || observed.Any(x => x.Item.Frame != x.Frame || x.Item.Length != x.Length ||
                x.Item.Layer != x.Layer || x.Item.Group != x.Group || x.Item.Remark != x.Remark ||
                !ReferenceEquals(ItemCharacters.Get(x.Item), x.Character)))
            throw new InvalidOperationException("計画後に元テンプレートが変更されました。配置せず停止しました。もう一度選んでください。");
    }

    public IReadOnlyList<IItem> CloneNormalized()
    {
        ValidateCurrent();
        var clones = new List<IItem>(Items.Count);
        foreach (var source in Items)
        {
            var clone = source.GetClone();
            if (clone == null || Items.Any(x => ReferenceEquals(x, clone)) || clones.Any(x => ReferenceEquals(x, clone)) ||
                clone.GetType() != source.GetType() || !ReferenceEquals(ItemCharacters.Get(source), ItemCharacters.Get(clone)))
                throw new InvalidOperationException("テンプレート全体を独立したアイテムとして複製できませんでした。配置していません。");
            clone.Frame = source.Frame - OriginFrame;
            clone.Layer = source.Layer - MinimumLayer;
            // Group IDs are scene-local. Atomicity comes from PlacementPlan/native Undo, not copied IDs.
            clone.Group = 0;
            clone.Remark = PluginRemarks.WithoutAssociation(clone.Remark);
            clones.Add(clone);
        }
        ValidateCurrent();
        return clones.AsReadOnly();
    }
}

public static partial class TemplateResolver
{
    public static TemplateSourceResolution ResolveTemplate(LibraryEntry entry)
    {
        var matches = ItemSettings.Default.Templates.Where(entry.Source.Matches).Take(2).ToArray();
        return matches.Length switch
        {
            0 => new(TemplateReferenceState.Missing, null),
            1 => new(TemplateReferenceState.Resolved, matches[0]),
            _ => new(TemplateReferenceState.Ambiguous, null)
        };
    }

    public static BundleResolution ResolveBundle(LibraryEntry entry)
    {
        var source = ResolveTemplate(entry);
        if (source.State != TemplateReferenceState.Resolved || source.Template == null) return new(source.State, null);
        var items = source.Template.Items.ToArray();
        if (items.Length == 0 || items.Length > 2048 || items.Distinct().Count() != items.Length ||
            items.Any(x => x == null || x.Frame < 0 || x.Length <= 0 || x.Layer < 0 || (long)x.Frame + x.Length > int.MaxValue))
            return new(TemplateReferenceState.Unsupported, null);
        var characters = items.Select(x => ItemCharacters.Get(x)?.Name).OfType<string>().ToArray();
        if (entry.CharacterName != null && characters.Any(x => !string.Equals(x, entry.CharacterName, StringComparison.Ordinal)))
            return new(TemplateReferenceState.CharacterMismatch, null);
        return new(TemplateReferenceState.Resolved, new TemplateBundle(entry, source.Template, items));
    }

    public static TemplateBundle RequireBundle(LibraryEntry entry)
    {
        var result = ResolveBundle(entry);
        return result.Bundle ?? throw new InvalidOperationException(result.Message);
    }

    public static IReadOnlyList<IItem> CloneBundle(LibraryEntry entry) => RequireBundle(entry).CloneNormalized();
}
