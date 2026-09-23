using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record IntentGeometry(IntentSelectionContext Context, TemplateBundle Source, IReadOnlyList<IItem> Items, bool Skipped);
internal sealed record PlacementSourceGeometry(IntentSelectionContext Context, MaterializedPlacementSource Source, IReadOnlyList<IItem> Items, bool Skipped);

/// <summary>Shared geometry path for direct tiles, expression batches and explicit association Resync.</summary>
public static class IntentPlacementGeometry
{
    public static IntentGeometry Prepare(Timeline timeline, IntentSelectionContext context, IntentPalette palette,
        IntentEntry entry, IReadOnlyList<LibraryEntry> library, IEnumerable<IItem> occupancy)
    {
        var references = library.Where(x => x.Id == entry.SourceId).Take(2).ToArray();
        if (references.Length != 1) throw new InvalidOperationException("元テンプレートの登録を一意に特定できません。");
        var bundle = TemplateResolver.RequireBundle(references[0]);
        var materialized = MaterializedPlacementSource.FromTemplate(bundle);
        var planned = Prepare(timeline, context, palette, entry, materialized, occupancy);
        return new(context, bundle, planned.Items, planned.Skipped);
    }

    internal static PlacementSourceGeometry Prepare(Timeline timeline, IntentSelectionContext context, IntentPalette palette,
        IntentEntry entry, MaterializedPlacementSource source, IEnumerable<IItem> occupancy)
    {
        ArgumentNullException.ThrowIfNull(source);
        palette.Target.Validate();
        palette.Relation.Validate();
        if (!palette.Target.Matches(context) || !palette.Entries.Contains(entry))
            throw new InvalidOperationException("現在の対象にこのパレットは適用できません。設定・選択を確認してください。");
        if (entry.SourceId != source.SourceId)
            throw new InvalidOperationException("セットの演出と配置Sourceが一致しません。設定を読み直してください。");
        if (palette.Target.CharacterName != null && source.CharacterName != null &&
            !string.Equals(source.CharacterName, palette.Target.CharacterName, StringComparison.Ordinal))
            throw new InvalidOperationException("パレットのキャラクター条件と配置Sourceが一致しません。");

        ValidateCharacters(context, source);
        source.ValidateCurrent();
        var time = IntentRelationResolver.Resolve(context, palette.Relation, entry, source.Span);
        if (time.Skip) return new(context, source, [], true);
        if (source.Items.Count > 1 && time.Length != source.Span)
            throw new InvalidOperationException("複数アイテムの内部の長さは変更しません。演出の設定で「テンプレート内の長さを維持」を選んでください。");
        var items = BundleLayerPlanner.Plan(source, time.Frame, source.Items.Count == 1 ? time.Length : null,
            context.MinimumLayer, context.MaximumLayer, palette.Relation.Layer, occupancy);
        RebindToSelectedCharacter(context, source.CharacterName, items);
        context.ValidateCurrent(timeline, false);
        source.ValidateCurrent();
        return new(context, source, items, false);
    }

    private static void RebindToSelectedCharacter(IntentSelectionContext context, string? logicalName, IReadOnlyList<IItem> items)
    {
        if (logicalName == null) return;
        var canonical = context.Selected.Select(x => ItemCharacters.Get(x.Item))
            .FirstOrDefault(x => x != null && string.Equals(x.Name, logicalName, StringComparison.Ordinal));
        if (canonical == null) return;
        foreach (var item in items) ItemCharacters.RebindIfSameLogicalCharacter(item, logicalName, canonical);
    }

    public static void ValidateCharacters(IntentSelectionContext context, TemplateBundle bundle) =>
        IntentCharacterRegistry.RequireUnambiguous(context.Selected.Select(x => x.CharacterName)
            .Concat(bundle.Items.Select(ItemCharacters.Name)).OfType<string>());

    internal static void ValidateCharacters(IntentSelectionContext context, MaterializedPlacementSource source) =>
        IntentCharacterRegistry.RequireUnambiguous(context.Selected.Select(x => x.CharacterName)
            .Concat(source.Items.Select(ItemCharacters.Name)).OfType<string>());
}
