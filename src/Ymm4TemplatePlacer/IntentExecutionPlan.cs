using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>Source/context guards around the existing native-Undo PlacementPlan gateway.</summary>
public sealed class IntentExecutionPlan
{
    private readonly IntentSelectionContext context;
    private readonly TemplateBundle source;
    private readonly IntentPalette palette;
    private readonly string paletteSnapshot;
    private readonly PlacementPlan plan;
    public bool Skipped { get; }
    public int Count => plan.Count;
    private IntentExecutionPlan(IntentSelectionContext context, TemplateBundle source, IntentPalette palette, PlacementPlan plan, bool skipped)
    {
        this.context = context; this.source = source; this.palette = palette; this.plan = plan; Skipped = skipped;
        paletteSnapshot = JsonSerializer.Serialize(palette);
    }
    public static IntentExecutionPlan Create(Timeline timeline, IntentPalette palette, IntentEntry tile, IReadOnlyList<LibraryEntry> library)
    {
        palette.Target.Validate(); palette.Relation.Validate();
        var context = IntentSelectionContext.Capture(timeline);
        if (!palette.Target.Matches(context) || !palette.Entries.Contains(tile))
            throw new InvalidOperationException("現在の選択にこのパレットは適用できません。アイテムとパレットを選び直してください。");
        var entries = library.Where(x => x.Id == tile.LibraryEntryId).Take(2).ToArray();
        if (entries.Length != 1) throw new InvalidOperationException("演出の登録を一意に特定できません。設定で元テンプレートを確認してください。");
        var bundle = TemplateResolver.RequireBundle(entries[0]);
        if (palette.Target.CharacterName != null && bundle.Items.Any(x => ItemCharacters.Get(x) is Character character && character.Name != palette.Target.CharacterName))
            throw new InvalidOperationException("パレットのキャラクター条件と元テンプレートが一致しません。");
        ValidateCharacterIdentity(timeline, context, bundle);
        var time = IntentRelationResolver.Resolve(context, palette.Relation, tile, bundle.Span);
        if (time.Skip) return new(context, bundle, palette, PlacementPlan.Create(timeline, []), true);
        if (bundle.Items.Count > 1 && time.Length != bundle.Span)
            throw new InvalidOperationException("複数アイテムの内部の長さは変更しません。演出の設定で「テンプレートの長さを維持」を選んでください。");
        var items = BundleLayerPlanner.Plan(bundle, time.Frame, bundle.Items.Count == 1 ? time.Length : null,
            context.MinimumLayer, context.MaximumLayer, palette.Relation.Layer, timeline.Items);
        context.ValidateCurrent(timeline);
        return new(context, bundle, palette, PlacementPlan.Create(timeline, items), false);
    }
    public int Commit(Timeline timeline, UndoRedoManager undo)
    {
        context.ValidateCurrent(timeline); source.ValidateCurrent();
        if (JsonSerializer.Serialize(palette) != paletteSnapshot) throw new InvalidOperationException("計画後にパレット設定が変更されました。配置していません。");
        ValidateCharacterIdentity(timeline, context, source);
        return plan.Commit(timeline, undo);
    }
    private static void ValidateCharacterIdentity(Timeline timeline, IntentSelectionContext context, TemplateBundle bundle)
    {
        foreach (var name in bundle.Items.Select(x => ItemCharacters.Get(x)?.Name).OfType<string>().Distinct(StringComparer.Ordinal))
        {
            var characters = timeline.Items.Select(ItemCharacters.Get).OfType<Character>().Where(x => x.Name == name).Distinct().Take(2).ToArray();
            if (characters.Length > 1)
                throw new InvalidOperationException($"シーン内の「{name}」に異なるキャラクター定義が複数あります。キャラクターを一意にしてから配置してください。");
        }
    }
}
