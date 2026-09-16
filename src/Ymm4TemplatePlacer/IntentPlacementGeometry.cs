using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record IntentGeometry(IntentSelectionContext Context, TemplateBundle Source, IReadOnlyList<IItem> Items, bool Skipped);

/// <summary>Shared geometry path for direct tiles, expression batches and explicit association Resync.</summary>
public static class IntentPlacementGeometry
{
    public static IntentGeometry Prepare(Timeline timeline, IntentSelectionContext context, IntentPalette palette,
        IntentEntry entry, IReadOnlyList<LibraryEntry> library, IEnumerable<IItem> occupancy)
    {
        palette.Target.Validate(); palette.Relation.Validate();
        if (!palette.Target.Matches(context) || !palette.Entries.Contains(entry))
            throw new InvalidOperationException("現在の対象にこのパレットは適用できません。設定・選択を確認してください。");
        var references = library.Where(x => x.Id == entry.LibraryEntryId).Take(2).ToArray();
        if (references.Length != 1) throw new InvalidOperationException("元テンプレートの登録を一意に特定できません。");
        var bundle = TemplateResolver.RequireBundle(references[0]);
        if (palette.Target.CharacterName != null && bundle.Items.Any(x => ItemCharacters.Name(x) is { } name && name != palette.Target.CharacterName))
            throw new InvalidOperationException("パレットのキャラクター条件と元テンプレートが一致しません。");
        ValidateCharacters(context, bundle);
        var time = IntentRelationResolver.Resolve(context, palette.Relation, entry, bundle.Span);
        if (time.Skip) return new(context, bundle, [], true);
        if (bundle.Items.Count > 1 && time.Length != bundle.Span)
            throw new InvalidOperationException("複数アイテムの内部の長さは変更しません。演出の設定で「テンプレート内の長さを維持」を選んでください。");
        var items = BundleLayerPlanner.Plan(bundle, time.Frame, bundle.Items.Count == 1 ? time.Length : null,
            context.MinimumLayer, context.MaximumLayer, palette.Relation.Layer, occupancy);
        RebindToSelectedCharacter(context, bundle, items);
        context.ValidateCurrent(timeline, false);
        return new(context, bundle, items, false);
    }
    private static void RebindToSelectedCharacter(IntentSelectionContext context, TemplateBundle bundle, IReadOnlyList<IItem> items)
    {
        if (bundle.CharacterName is not { } logicalName) return;
        var canonical = context.Selected.Select(x => ItemCharacters.Get(x.Item))
            .FirstOrDefault(x => x != null && string.Equals(x.Name, logicalName, StringComparison.Ordinal));
        if (canonical == null) return;
        foreach (var item in items) ItemCharacters.RebindIfSameLogicalCharacter(item, logicalName, canonical);
    }
    public static void ValidateCharacters(IntentSelectionContext context, TemplateBundle bundle) =>
        IntentCharacterRegistry.RequireUnambiguous(context.Selected.Select(x => x.CharacterName)
            .Concat(bundle.Items.Select(ItemCharacters.Name)).OfType<string>());
}
