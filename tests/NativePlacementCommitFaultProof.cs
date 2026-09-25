using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementCommitFaultRecovery(Timeline timeline, UndoRedoManager undo)
    {
        stage = "AUDIT B08 PlacementPlan commit fault recovery";
        var openingItems = timeline.Items;
        var openingSelection = timeline.SelectedItems;

        try
        {
            foreach (var faultPoint in new[] { "after-updates", "after-items", "after-refresh" })
            {
                var existing = new TextItem
                {
                    Frame = 100,
                    Length = 20,
                    Layer = 10,
                    Remark = "B08 existing"
                };
                var addition = new TextItem
                {
                    Frame = 200,
                    Length = 15,
                    Layer = 11,
                    Remark = "B08 addition"
                };

                timeline.Items = [existing];
                timeline.SelectedItems = [existing];
                timeline.RefreshTimelineLengthAndMaxLayer();
                undo.Record();

                var baseline = Signature(timeline);
                var plan = PlacementPlan.Create(
                    timeline,
                    [addition],
                    [new PlannedItemUpdate(existing, 120, 25, 12, "B08 updated")]);

                PlacementPlan.ProofCommitFaultInjection = point =>
                {
                    if (point == faultPoint)
                        throw new InvalidOperationException("B08 intentional commit fault at " + point);
                };

                var failed = false;
                try { _ = plan.Commit(timeline, undo); }
                catch (InvalidOperationException ex) when (ex.Message.Contains("B08 intentional", StringComparison.Ordinal))
                {
                    failed = true;
                }
                finally
                {
                    PlacementPlan.ProofCommitFaultInjection = null;
                }

                Assert(failed &&
                    Signature(timeline) == baseline &&
                    timeline.Items.Count == 1 &&
                    ReferenceEquals(timeline.Items[0], existing) &&
                    existing is { Frame: 100, Length: 20, Layer: 10, Remark: "B08 existing" },
                    $"AUDIT_B08 {faultPoint} commit failure restores the exact pre-commit snapshot before returning the error");

                var committed = plan.Commit(timeline, undo);
                Assert(committed == 2 && Signature(timeline) != baseline,
                    $"AUDIT_B08 normal commit remains usable after recovered {faultPoint} failure");

                await undo.UndoAsync();
                await Idle();

                Assert(Signature(timeline) == baseline &&
                    timeline.Items.Count == 1 &&
                    ReferenceEquals(timeline.Items[0], existing),
                    $"AUDIT_B08 recovered {faultPoint} failure does not poison the next successful native Undo");
            }

            Log("AUDIT_B08=PASS");
        }
        finally
        {
            PlacementPlan.ProofCommitFaultInjection = null;
            timeline.Items = openingItems;
            timeline.SelectedItems = openingSelection;
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
        }
    }
}
