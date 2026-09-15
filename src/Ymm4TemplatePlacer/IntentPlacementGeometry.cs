using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

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
        if (palette.Target.CharacterName != null && bundle.Items.Any(x => ItemCharacters.Get(x) is { } character && character.Name != palette.Target.CharacterName))
            throw new InvalidOperationException("パレットのキャラクター条件と元テンプレートが一致しません。");
        ValidateCharacters(context, bundle);
        var time = IntentRelationResolver.Resolve(context, palette.Relation, entry, bundle.Span);
        if (time.Skip) return new(context, bundle, [], true);
        if (bundle.Items.Count > 1 && time.Length != bundle.Span)
            throw new InvalidOperationException("複数アイテムの内部の長さは変更しません。演出の設定で「テンプレート内の長さを維持」を選んでください。");
        var items = BundleLayerPlanner.Plan(bundle, time.Frame, bundle.Items.Count == 1 ? time.Length : null,
            context.MinimumLayer, context.MaximumLayer, palette.Relation.Layer, occupancy);
        context.ValidateCurrent(timeline, false);
        return new(context, bundle, items, false);
    }
    public static void ValidateCharacters(IntentSelectionContext context, TemplateBundle bundle)
    {
        var names = context.Selected.Select(x => x.CharacterName).Concat(bundle.Items.Select(x => ItemCharacters.Get(x)?.Name))
            .OfType<string>().Distinct(StringComparer.Ordinal);
        foreach (var name in names)
        {
            // Template clones can legitimately hold a detached same-name Character. Only an actual duplicate host definition is ambiguous.
            if (CharacterSettings.Default.Characters.Where(x => x.Name == name).Distinct().Take(2).Count() > 1)
                throw new InvalidOperationException($"YMM4に同名キャラクター「{name}」が複数登録されています。一意にしてから配置してください。");
        }
    }
}
