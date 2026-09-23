using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>Source/context guards around the existing native-Undo PlacementPlan gateway.</summary>
public sealed class IntentExecutionPlan
{
    private readonly PlacementSourceGeometry geometry;
    private readonly IntentPalette palette;
    private readonly string paletteSnapshot;
    private readonly PlacementPlan plan;
    private readonly bool requireSettingsRevalidation;
    public bool Skipped => geometry.Skipped;
    public int Count => plan.Count;

    private IntentExecutionPlan(
        PlacementSourceGeometry geometry,
        IntentPalette palette,
        PlacementPlan plan,
        bool requireSettingsRevalidation)
    {
        this.geometry = geometry;
        this.palette = palette;
        this.plan = plan;
        this.requireSettingsRevalidation = requireSettingsRevalidation;
        paletteSnapshot = JsonSerializer.Serialize(palette);
    }

    public static IntentExecutionPlan Create(
        Timeline timeline,
        IntentPalette palette,
        IntentEntry tile,
        IReadOnlyList<LibraryEntry> library)
    {
        var context = IntentSelectionContext.Capture(timeline);
        var reference = library.SingleOrDefault(x => x.Id == tile.SourceId)
            ?? throw new InvalidOperationException("元テンプレートの登録を一意に特定できません。");
        var source = MaterializedPlacementSource.FromTemplate(TemplateResolver.RequireBundle(reference));
        var geometry = IntentPlacementGeometry.Prepare(timeline, context, palette, tile, source, timeline.Items);
        context.ValidateCurrent(timeline);
        return new(geometry, palette, PlacementPlan.Create(timeline, geometry.Items), false);
    }

    internal static async Task<IntentExecutionPlan> CreateAsync(
        Timeline timeline,
        IntentPalette palette,
        IntentEntry tile,
        PlacerSettings settings,
        Func<Character, TachiePresetProbeTarget>? presetResolver = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var context = IntentSelectionContext.Capture(timeline);
        var registration = PlacementSourceRegistry.Resolve(settings, tile.SourceId);

        MaterializedPlacementSource source;
        if (registration.Kind == PlacementSourceKind.Template)
        {
            source = MaterializedPlacementSource.FromTemplate(
                TemplateResolver.RequireBundle(registration.Template!));
        }
        else
        {
            var preset = registration.TachiePreset!;
            var characters = context.Selected.Select(x => ItemCharacters.Get(x.Item))
                .Where(x => x != null && string.Equals(x.Name, preset.CharacterName, StringComparison.Ordinal))
                .Cast<Character>()
                .Distinct((IEqualityComparer<Character>)ReferenceEqualityComparer.Instance)
                .Take(2)
                .ToArray();
            if (characters.Length != 1)
                throw new InvalidOperationException("立ち絵プリセットSourceの対象キャラクターを現在の選択から一意に特定できません。");
            source = await TachiePresetSourceMaterializer.MaterializeAsync(
                preset, characters[0], presetResolver, token);
        }

        context.ValidateCurrent(timeline);
        var geometry = IntentPlacementGeometry.Prepare(
            timeline, context, palette, tile, source, timeline.Items);
        context.ValidateCurrent(timeline);
        return new(geometry, palette, PlacementPlan.Create(timeline, geometry.Items), true);
    }

    public int Commit(Timeline timeline, UndoRedoManager undo)
    {
        if (requireSettingsRevalidation)
            throw new InvalidOperationException("この配置は現在の設定を再確認してから確定してください。");
        ValidateCore(timeline);
        if (JsonSerializer.Serialize(palette) != paletteSnapshot)
            throw new InvalidOperationException("計画後にパレット設定が変更されました。配置していません。");
        return plan.Commit(timeline, undo);
    }

    internal int Commit(Timeline timeline, UndoRedoManager undo, PlacerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateCore(timeline);
        var currentPalette = settings.IntentPalettes.SingleOrDefault(x => x.Id == palette.Id);
        if (currentPalette == null || JsonSerializer.Serialize(currentPalette) != paletteSnapshot)
            throw new InvalidOperationException("計画後にパレット設定が変更されました。配置していません。");

        var current = PlacementSourceRegistry.Resolve(settings, geometry.Source.SourceId);
        string semanticHash;
        if (current.Kind == PlacementSourceKind.Template)
            semanticHash = IntentAssociationTag.Hash(TemplateResolver.RequireBundle(current.Template!));
        else
            semanticHash = current.TachiePreset!.SemanticHash();
        if (semanticHash != geometry.Source.SemanticHash)
            throw new InvalidOperationException("計画後に配置Sourceが変更されました。配置していません。");

        return plan.Commit(timeline, undo);
    }

    private void ValidateCore(Timeline timeline)
    {
        geometry.Context.ValidateCurrent(timeline);
        geometry.Source.ValidateCurrent();
        IntentPlacementGeometry.ValidateCharacters(geometry.Context, geometry.Source);
    }
}
