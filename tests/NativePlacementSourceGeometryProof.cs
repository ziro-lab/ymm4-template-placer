using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyPlacementSourceGeometry(Timeline timeline)
    {
        stage = "Placement Source P1 source-neutral geometry";
        var signature = Signature(timeline);
        var guardCalls = 0;

        var singleton = new MaterializedPlacementSource(
            Guid.Parse("10000000-0000-4000-8000-000000000001"),
            PlacementSourceKind.TachiePreset,
            [new TextItem { Frame = 0, Length = 10, Layer = 0 }],
            10,
            null,
            new string('a', 64),
            () => guardCalls++);
        var occupied = new TextItem { Frame = 100, Length = 30, Layer = 18 };
        var planned = BundleLayerPlanner.Plan(
            singleton,
            frame: 100,
            singletonLength: 30,
            targetMinimumLayer: 20,
            targetMaximumLayer: 20,
            new RelativeLayerPolicy
            {
                Direction = RelativeLayerDirection.Up,
                Offset = 2,
                Minimum = 0,
                Maximum = 99
            },
            [occupied]);

        Assert(planned.Count == 1 &&
            planned[0].Frame == 100 &&
            planned[0].Length == 30 &&
            planned[0].Layer == 17 &&
            guardCalls >= 2,
            "PLACEMENT_SOURCE P1 fresh non-Template singleton uses the shared frame/length/collision geometry and current-source guards");

        var multi = new MaterializedPlacementSource(
            Guid.Parse("10000000-0000-4000-8000-000000000002"),
            PlacementSourceKind.Template,
            [
                new TextItem { Frame = 0, Length = 10, Layer = 0 },
                new TextItem { Frame = 5, Length = 20, Layer = 1 }
            ],
            25,
            null,
            new string('b', 64),
            () => { });
        var multiPlanned = BundleLayerPlanner.Plan(
            multi,
            frame: 200,
            singletonLength: null,
            targetMinimumLayer: 20,
            targetMaximumLayer: 20,
            new RelativeLayerPolicy
            {
                Direction = RelativeLayerDirection.Down,
                Offset = 1,
                Minimum = 0,
                Maximum = 99
            },
            []);
        Assert(multiPlanned.Count == 2 &&
            multiPlanned[0].Frame == 200 && multiPlanned[0].Length == 10 && multiPlanned[0].Layer == 21 &&
            multiPlanned[1].Frame == 205 && multiPlanned[1].Length == 20 && multiPlanned[1].Layer == 22,
            "PLACEMENT_SOURCE P1 shared geometry preserves normalized multi-item relative frame/layer structure");

        var legacyPolicy = JsonSerializer.Deserialize<RelativeLayerPolicy>("{}")!;
        Assert(legacyPolicy.Mode == LayerPlacementMode.RelativeToTarget &&
            legacyPolicy.Offset == 1 && legacyPolicy.AbsoluteLayer == 0 &&
            legacyPolicy.Minimum == 0 && legacyPolicy.Maximum == 99,
            "PLACEMENT_RULE P2 old layer JSON defaults to the exact existing relative placement semantics");
        var defaultPolicyJson = JsonSerializer.Serialize(new RelativeLayerPolicy());
        Assert(!defaultPolicyJson.Contains("\"Mode\"", StringComparison.Ordinal) &&
            !defaultPolicyJson.Contains("\"AbsoluteLayer\"", StringComparison.Ordinal),
            "PLACEMENT_RULE P2 default relative policy does not add new placement-mode fields to persisted JSON");
        var invalidAbsoluteBoundsRejected = false;
        try
        {
            new RelativeLayerPolicy
            {
                Mode = LayerPlacementMode.Absolute,
                AbsoluteLayer = 50,
                Minimum = 60,
                Maximum = 99
            }.Validate();
        }
        catch (InvalidOperationException) { invalidAbsoluteBoundsRejected = true; }
        Assert(invalidAbsoluteBoundsRejected,
            "PLACEMENT_RULE P2 absolute base outside the saved search bounds is rejected before persistence/execution");

        var absolutePreset = new MaterializedPlacementSource(
            Guid.Parse("10000000-0000-4000-8000-000000000003"),
            PlacementSourceKind.TachiePreset,
            [new TextItem { Frame = 0, Length = 20, Layer = 0 }],
            20,
            null,
            new string('d', 64),
            () => { });
        var absoluteOccupied = new TextItem { Frame = 100, Length = 20, Layer = 50 };
        var absolutePresetPlan = BundleLayerPlanner.Plan(
            absolutePreset,
            frame: 100,
            singletonLength: 20,
            targetMinimumLayer: 20,
            targetMaximumLayer: 20,
            new RelativeLayerPolicy
            {
                Mode = LayerPlacementMode.Absolute,
                AbsoluteLayer = 50,
                Direction = RelativeLayerDirection.Up,
                Minimum = 0,
                Maximum = 99
            },
            [absoluteOccupied]);
        Assert(absolutePresetPlan.Single().Layer == 49,
            "PLACEMENT_RULE P2 absolute preset source starts at the requested layer and collision search moves only Up");

        var absoluteMulti = new MaterializedPlacementSource(
            Guid.Parse("10000000-0000-4000-8000-000000000004"),
            PlacementSourceKind.Template,
            [
                new TextItem { Frame = 0, Length = 10, Layer = 0 },
                new TextItem { Frame = 3, Length = 12, Layer = 1 }
            ],
            15,
            null,
            new string('e', 64),
            () => { });
        var absoluteMultiPlan = BundleLayerPlanner.Plan(
            absoluteMulti,
            frame: 300,
            singletonLength: null,
            targetMinimumLayer: 1,
            targetMaximumLayer: 1,
            new RelativeLayerPolicy
            {
                Mode = LayerPlacementMode.Absolute,
                AbsoluteLayer = 40,
                Direction = RelativeLayerDirection.Down,
                Minimum = 0,
                Maximum = 99
            },
            []);
        Assert(absoluteMultiPlan[0].Layer == 40 && absoluteMultiPlan[1].Layer == 41,
            "PLACEMENT_RULE P2 absolute multi-item source preserves normalized internal layer offsets");

        var noRoom = new MaterializedPlacementSource(
            Guid.Parse("10000000-0000-4000-8000-000000000005"),
            PlacementSourceKind.Template,
            [
                new TextItem { Frame = 0, Length = 10, Layer = 0 },
                new TextItem { Frame = 0, Length = 10, Layer = 1 }
            ],
            10,
            null,
            new string('f', 64),
            () => { });
        var absoluteRejected = false;
        try
        {
            _ = BundleLayerPlanner.Plan(
                noRoom,
                frame: 400,
                singletonLength: null,
                targetMinimumLayer: 20,
                targetMaximumLayer: 20,
                new RelativeLayerPolicy
                {
                    Mode = LayerPlacementMode.Absolute,
                    AbsoluteLayer = 99,
                    Direction = RelativeLayerDirection.Down,
                    Minimum = 0,
                    Maximum = 99
                },
                []);
        }
        catch (InvalidOperationException) { absoluteRejected = true; }
        Assert(absoluteRejected && Signature(timeline) == signature,
            "PLACEMENT_RULE P2 absolute source that cannot fit the saved bounds fails with zero Timeline mutation");
        Log("PLACEMENT_RULE_P2=PASS");

        var rejectedUnnormalized = false;
        try
        {
            _ = new MaterializedPlacementSource(
                Guid.NewGuid(),
                PlacementSourceKind.TachiePreset,
                [new TextItem { Frame = 1, Length = 10, Layer = 0 }],
                11,
                null,
                new string('c', 64),
                () => { });
        }
        catch (InvalidOperationException) { rejectedUnnormalized = true; }
        Assert(rejectedUnnormalized,
            "PLACEMENT_SOURCE P1 source-neutral planner admission rejects non-normalized materialized content");

        var template = Template("PlacementSource/P1", [
            new TextItem { Frame = 10, Length = 12, Layer = 6 },
            new TextItem { Frame = 15, Length = 7, Layer = 7 }
        ]);
        ItemSettings.Default.Templates.Add(template);
        try
        {
            var entry = TemplateResolver.Reference(template, "P1 Template", null);
            var bundle = TemplateResolver.RequireBundle(entry);
            var policy = new RelativeLayerPolicy
            {
                Direction = RelativeLayerDirection.Down,
                Offset = 1,
                Minimum = 0,
                Maximum = 99
            };
            var wrapped = BundleLayerPlanner.Plan(bundle, 300, null, 20, 20, policy, []);
            var generic = BundleLayerPlanner.Plan(MaterializedPlacementSource.FromTemplate(bundle), 300, null, 20, 20, policy, []);
            Assert(wrapped.Select(x => (x.GetType(), x.Frame, x.Length, x.Layer)).SequenceEqual(
                    generic.Select(x => (x.GetType(), x.Frame, x.Length, x.Layer))),
                "PLACEMENT_SOURCE P1 existing TemplateBundle wrapper and source-neutral materialized path produce identical geometry");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(template);
        }

        Assert(Signature(timeline) == signature,
            "PLACEMENT_SOURCE P1 geometry seam proof performs zero Timeline writes");
        Log("PLACEMENT_SOURCE_P1=PASS");
    }
}
