using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyRelativeFoundations(Timeline timeline, UndoRedoManager undo)
    {
        var before = timeline.Items; var selected = timeline.SelectedItems;
        var character = new Character { Name = "R1 Bundle" };
        var target = new VoiceItem(character) { Frame = 100, Length = 80, Layer = 20 };
        var a = new TachieFaceItem(character) { Frame = 30, Length = 25, Layer = 8, Group = 42, Remark = "manual source" };
        var b = new TachieFaceItem(character) { Frame = 45, Length = 35, Layer = 10, Group = 42 };
        var template = Template("R1/Bundle", [a, b]);
        ItemSettings.Default.Templates.Add(template);
        try
        {
            timeline.Items = [target]; timeline.SelectedItems = [target]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            var entry = TemplateResolver.Reference(template, "Bundle", character.Name);
            Assert(TemplateResolver.ResolveTemplate(entry).Template == template && TemplateResolver.Resolve(entry).State == TemplateReferenceState.Unsupported,
                "R1/R2 exact Template resolution is independent of count; legacy singleton API stays conservative");
            var bundle = TemplateResolver.RequireBundle(entry);
            Assert(bundle.Items.Count == 2 && bundle.OriginFrame == 30 && bundle.Span == 50 && bundle.MinimumLayer == 8 && bundle.MaximumLayer == 10,
                "R1/R2 multi-item native fixture captures frame/layer origin and bounding span");
            var clones = bundle.CloneNormalized();
            Assert(clones[0].Frame == 0 && clones[1].Frame == 15 && clones[0].Layer == 0 && clones[1].Layer == 2 &&
                clones[0].Length == 25 && clones[1].Length == 35 && clones.All(x => x.Group == 0) && !clones.Any(template.Items.Contains),
                "R2 clones normalize minimum Frame, preserve internal geometry, and do not reuse source/group objects");
            clones[0].Length = 7; Assert(a.Length == 25 && a.Frame == 30 && a.Layer == 8 && a.Group == 42,
                "R2 editing a clone leaves the live Template unchanged");
            var duplicate = Template(template.Name, [new TachieFaceItem(character) { Length = 10 }]);
            ItemSettings.Default.Templates.Add(duplicate);
            try { Assert(TemplateResolver.ResolveBundle(entry).State == TemplateReferenceState.Ambiguous, "R2 duplicate exact locator never resolves a first bundle"); }
            finally { ItemSettings.Default.Templates.Remove(duplicate); }
            ItemSettings.Default.Templates.Remove(template);
            Assert(TemplateResolver.ResolveBundle(entry).State == TemplateReferenceState.Missing, "R2 missing source has no fuzzy bundle recovery");
            ItemSettings.Default.Templates.Add(template);
            var lateBlocker = new TachieFaceItem(character) { Frame = 130, Length = 10, Layer = 19 };
            timeline.Items = timeline.Items.Add(lateBlocker);
            var up = new RelativeLayerPolicy { Direction = RelativeLayerDirection.Up, Minimum = 0, Maximum = 40 };
            var planned = BundleLayerPlanner.Plan(bundle, 100, null, 20, 20, up, timeline.Items);
            Assert(planned[0].Layer == 16 && planned[1].Layer == 18 && planned[0].Frame == 100 && planned[1].Frame == 115,
                "R3 上 means smaller Layer; a late member blocker moves the ENTIRE bundle farther up");
            var signature = Signature(timeline); var operation = PlacementPlan.Create(timeline, planned);
            Assert(Signature(timeline) == signature && operation.Count == 2, "R3 bundle planning is zero-write");
            Assert(operation.Commit(timeline, undo) == 2 && timeline.Items.Contains(target) && timeline.Items.Contains(lateBlocker),
                "R3 full bundle commits add-only through existing PlacementPlan");
            var placed = Signature(timeline); await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == signature, "R3 one native Undo restores whole bundle action");
            await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "R3 one native Redo restores whole bundle action");
            await undo.UndoAsync(); await Idle();
            var down = up with { Direction = RelativeLayerDirection.Down };
            var downBlocker = new TachieFaceItem(character) { Frame = 125, Length = 10, Layer = 23 };
            var downPlan = BundleLayerPlanner.Plan(bundle, 100, null, 20, 20, down, timeline.Items.Append(downBlocker));
            Assert(downPlan[0].Layer == 22 && downPlan[1].Layer == 24, "R3 下 means larger Layer and preserves common delta during collision escape");
            RejectWithoutMutation(timeline, () => BundleLayerPlanner.Plan(bundle, 100, null, 1, 1, up, timeline.Items),
                "R3 insufficient upper space rejects atomically without wrapping below");
            RejectWithoutMutation(timeline, () => BundleLayerPlanner.Plan(bundle, 100, null, 40, 40, down, timeline.Items),
                "R3 insufficient lower space rejects atomically without wrapping above");
            RejectWithoutMutation(timeline, () => BundleLayerPlanner.Plan(bundle, int.MaxValue - 5, null, 20, 20, up, timeline.Items),
                "R3 bundle time overflow is rejected before partial mutation");
            a.Length++;
            RejectWithoutMutation(timeline, bundle.ValidateCurrent, "R2 source mutation invalidates a captured bundle"); a.Length--;
            Assert(a.Remark == "manual source" && b.Frame == 45 && b.Layer == 10 && b.Length == 35,
                "R3 source content and relative geometry remain unchanged after native placement/Undo");
            Log("R1=PASS\nR2=PASS\nR3=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(template); timeline.Items = before; timeline.SelectedItems = selected;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        }
    }
}
