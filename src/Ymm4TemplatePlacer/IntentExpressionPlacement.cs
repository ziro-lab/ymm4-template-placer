using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed class IntentExpressionPlacement
{
    private readonly IReadOnlyList<IntentExpressionMutation> mutations;
    public PlacementPlan Plan { get; }
    public long NextSerial { get; }
    public int Skipped { get; }
    private IntentExpressionPlacement(PlacementPlan plan, long nextSerial, int skipped, IReadOnlyList<IntentExpressionMutation> mutations)
    { Plan = plan; NextSerial = nextSerial; Skipped = skipped; this.mutations = mutations; }
    public static IntentExpressionPlacement Create(Timeline timeline, IReadOnlyList<AssignmentRow> rows, PlacerSettings settings)
    {
        PlacementEngine.ValidateSnapshot(timeline, rows.Select(x => x.Target).ToArray());
        var allocator = new IntentAssociationSerialAllocator(timeline, settings.NextAssociationId);
        var mutations = new List<IntentExpressionMutation>(); var skipped = 0;
        foreach (var row in rows.Where(x => x.SelectedChoice.Template != null))
        {
            var mutation = IntentExpressionMutation.Create(timeline, row, row.SelectedChoice, settings, allocator, true);
            mutations.Add(mutation); if (mutation.Skipped) skipped++;
        }
        return new(PlacementPlan.Combine(timeline, mutations.Select(x => x.Plan).ToArray()), allocator.NextSerial, skipped, mutations);
    }
    public int Commit(Timeline timeline, UndoRedoManager undo, PlacerSettings settings)
    {
        foreach (var mutation in mutations) mutation.ValidateCurrent(timeline, settings);
        return Plan.Commit(timeline, undo);
    }
}
