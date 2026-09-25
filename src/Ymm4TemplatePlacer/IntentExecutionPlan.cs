using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>Source/context guards around the existing native-Undo PlacementPlan gateway.</summary>
public sealed class IntentExecutionPlan
{
    private readonly IReadOnlyList<PlacementSourceGeometry> geometries;
    private readonly IReadOnlyList<ResolvedIntentTime> results;
    private readonly IntentPalette palette;
    private readonly string paletteSnapshot;
    private readonly PlacementPlan plan;
    private readonly bool requireSettingsRevalidation;

    public bool Skipped => geometries.All(x => x.Skipped);
    public int Count => plan.Count;
    public int ResultCount => results.Count;
    public bool HasMultipleResults => ResultCount > 1;

    internal IntentSelectionContext Context => geometries[0].Context;
    internal Guid PaletteId => palette.Id;
    internal Guid SourceId => geometries[0].Source.SourceId;
    internal string PaletteSnapshot => paletteSnapshot;
    internal string SourceSemanticHash => geometries[0].Source.SemanticHash;
    internal string ResultSignature => string.Join(";", results
        .OrderBy(x => x.Skip)
        .ThenBy(x => x.Frame)
        .ThenBy(x => x.Length)
        .Select(x => $"{(x.Skip ? 1 : 0)}:{x.Frame}:{x.Length}"));

    private IntentExecutionPlan(
        IReadOnlyList<PlacementSourceGeometry> geometries,
        IReadOnlyList<ResolvedIntentTime> results,
        IntentPalette palette,
        PlacementPlan plan,
        bool requireSettingsRevalidation)
    {
        if (geometries.Count == 0 || results.Count == 0 || geometries.Count != results.Count)
            throw new InvalidOperationException("配置結果の計画数が不正です。配置していません。");
        this.geometries = geometries;
        this.results = results;
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
        var geometries = IntentPlacementGeometry.PrepareAlternatives(
            timeline, context, palette, tile, source, timeline.Items, out var results);
        context.ValidateCurrent(timeline);
        return Build(timeline, geometries, results, palette, false);
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
        var geometries = IntentPlacementGeometry.PrepareAlternatives(
            timeline, context, palette, tile, source, timeline.Items, out var results);
        context.ValidateCurrent(timeline);
        return Build(timeline, geometries, results, palette, true);
    }

    private static IntentExecutionPlan Build(
        Timeline timeline,
        IReadOnlyList<PlacementSourceGeometry> geometries,
        IReadOnlyList<ResolvedIntentTime> results,
        IntentPalette palette,
        bool requireSettingsRevalidation)
    {
        var plans = geometries.Select(x => PlacementPlan.Create(timeline, x.Items)).ToArray();
        var combined = plans.Length == 1 ? plans[0] : PlacementPlan.Combine(timeline, plans);
        return new(geometries, results, palette, combined, requireSettingsRevalidation);
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

        var current = PlacementSourceRegistry.Resolve(settings, SourceId);
        string semanticHash;
        if (current.Kind == PlacementSourceKind.Template)
            semanticHash = IntentAssociationTag.Hash(TemplateResolver.RequireBundle(current.Template!));
        else
            semanticHash = current.TachiePreset!.SemanticHash();
        if (semanticHash != SourceSemanticHash)
            throw new InvalidOperationException("計画後に配置Sourceが変更されました。配置していません。");

        return plan.Commit(timeline, undo);
    }

    private void ValidateCore(Timeline timeline)
    {
        Context.ValidateCurrent(timeline);
        foreach (var geometry in geometries)
        {
            geometry.Source.ValidateCurrent();
            IntentPlacementGeometry.ValidateCharacters(Context, geometry.Source);
        }
    }
}
