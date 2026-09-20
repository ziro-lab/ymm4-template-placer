using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

/// <summary>Source/context guards around the existing native-Undo PlacementPlan gateway.</summary>
public sealed class IntentExecutionPlan
{
    private readonly IntentGeometry geometry;
    private readonly IntentPalette palette;
    private readonly string paletteSnapshot;
    private readonly PlacementPlan plan;
    public bool Skipped => geometry.Skipped;
    public int Count => plan.Count;
    private IntentExecutionPlan(IntentGeometry geometry, IntentPalette palette, PlacementPlan plan)
    {
        this.geometry = geometry; this.palette = palette; this.plan = plan;
        paletteSnapshot = JsonSerializer.Serialize(palette);
    }
    public static IntentExecutionPlan Create(Timeline timeline, IntentPalette palette, IntentEntry tile, IReadOnlyList<LibraryEntry> library)
    {
        var context = IntentSelectionContext.Capture(timeline);
        var geometry = IntentPlacementGeometry.Prepare(timeline, context, palette, tile, library, timeline.Items);
        context.ValidateCurrent(timeline);
        return new(geometry, palette, PlacementPlan.Create(timeline, geometry.Items));
    }
    public int Commit(Timeline timeline, UndoRedoManager undo)
    {
        geometry.Context.ValidateCurrent(timeline); geometry.Source.ValidateCurrent();
        if (JsonSerializer.Serialize(palette) != paletteSnapshot) throw new InvalidOperationException("計画後にパレット設定が変更されました。配置していません。");
        IntentPlacementGeometry.ValidateCharacters(geometry.Context, geometry.Source);
        return plan.Commit(timeline, undo);
    }
}
