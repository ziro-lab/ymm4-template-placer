using System.Reflection;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyNeighborAmbiguityFanout(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Neighbor ambiguity result fan-out";
        var vm = ViewModel!;

        using (var scope = new Round3Fixture(timeline, undo))
        {
            var source = scope.AddTemplate("NeighborFanout/Template", new TextItem { Frame = 0, Length = 10, Layer = 0 });
            var character = new Character { Name = "Neighbor Fanout" };
            var target = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20, Serif = "target" };
            var shortNext = new VoiceItem(character) { Frame = 200, Length = 20, Layer = 20, Serif = "next short" };
            var longNext = new VoiceItem(character) { Frame = 200, Length = 60, Layer = 21, Serif = "next long" };
            var entry = new IntentEntry(source.Id);
            var targetRule = new IntentTargetContext
            {
                ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
                CharacterName = character.Name
            };
            var relation = new IntentRelation
            {
                Anchor = IntentAnchor.SelectedStart,
                Duration = IntentDuration.UntilRelated,
                Neighbor = IntentNeighbor.NextSameTypeAndCharacter,
                NeighborEdge = IntentNeighborEdge.End,
                Fallback = IntentFallback.DoNotPlace,
                Layer = new RelativeLayerPolicy
                {
                    Direction = RelativeLayerDirection.Up,
                    Offset = 1,
                    Minimum = 0,
                    Maximum = 99
                }
            };
            var palette = new IntentPalette(
                Guid.Parse("81000000-0000-4000-8000-000000000001"),
                "Neighbor fan-out",
                "周囲候補",
                targetRule,
                relation,
                [entry]);
            var settings = PlacerSettingsStore.Copy(scope.Original);
            settings.ExpressionBootstrapComplete = true;
            settings.IntentPaletteRevision = 1;
            settings.Library = [source];
            settings.TachiePresetSources = [];
            settings.IntentPalettes = [palette];
            settings.Palettes = [];
            scope.Apply(settings, [target, shortNext, longNext], [target], 100);
            await Idle();

            var context = IntentSelectionContext.Capture(timeline);
            var startResults = IntentNeighborResultResolver.Resolve(
                context,
                relation with { NeighborEdge = IntentNeighborEdge.Start },
                entry,
                10);
            Assert(startResults.Count == 1 &&
                startResults[0] == new ResolvedIntentTime(false, 100, 100),
                "NEIGHBOR_AMBIGUITY P0 same-rank candidates that produce one final start-edge result collapse to immediate placement");

            var endResults = IntentNeighborResultResolver.Resolve(context, relation, entry, 10);
            Assert(endResults.Count == 2 &&
                endResults.OrderBy(x => x.Length).Select(x => x.Length).SequenceEqual([120, 160]),
                "NEIGHBOR_AMBIGUITY P0 same-rank candidates with different end edges expose two distinct final results");

            var strictRejected = false;
            try { _ = IntentRelationResolver.Resolve(context, relation, entry, 10); }
            catch (InvalidOperationException) { strictRejected = true; }
            Assert(strictRejected,
                "NEIGHBOR_AMBIGUITY P0 strict single-result resolver remains fail-closed for the same ambiguous Neighbor state");

            var previousTarget = new VoiceItem(character) { Frame = 300, Length = 30, Layer = 20 };
            var previousNearEnd = new VoiceItem(character) { Frame = 200, Length = 80, Layer = 20 };
            var previousFarEnd = new VoiceItem(character) { Frame = 200, Length = 20, Layer = 21 };
            timeline.Items = [previousNearEnd, previousFarEnd, previousTarget];
            timeline.SelectedItems = [previousTarget];
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
            var previousContext = IntentSelectionContext.Capture(timeline);
            var previousResults = IntentNeighborResultResolver.Resolve(
                previousContext,
                new IntentRelation
                {
                    Anchor = IntentAnchor.RelatedEnd,
                    Alignment = IntentAlignment.StartAtAnchor,
                    Duration = IntentDuration.Fixed,
                    FixedDuration = 10,
                    Neighbor = IntentNeighbor.PreviousSameTypeAndCharacter,
                    MaximumNeighborGap = 30
                },
                entry,
                10);
            Assert(previousResults.Count == 1 &&
                previousResults[0] == new ResolvedIntentTime(false, 280, 10),
                "NEIGHBOR_AMBIGUITY P0 MaximumNeighborGap filters tied previous candidates before ambiguity is evaluated");

            var many = Enumerable.Range(1, 33)
                .Select(i => (IItem)new VoiceItem(character) { Frame = 400, Length = i, Layer = i })
                .ToArray();
            var manyTarget = new VoiceItem(character) { Frame = 350, Length = 20, Layer = 50 };
            timeline.Items = [manyTarget, .. many];
            timeline.SelectedItems = [manyTarget];
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
            var bounded = false;
            try
            {
                _ = IntentNeighborResultResolver.Resolve(
                    IntentSelectionContext.Capture(timeline),
                    new IntentRelation
                    {
                        Anchor = IntentAnchor.RelatedEnd,
                        Duration = IntentDuration.Fixed,
                        FixedDuration = 5,
                        Neighbor = IntentNeighbor.NextSameTypeAndCharacter
                    },
                    entry,
                    10);
            }
            catch (InvalidOperationException) { bounded = true; }
            Assert(bounded,
                "NEIGHBOR_AMBIGUITY P0 more than 32 distinct results is rejected before placement fan-out");
            Log("NEIGHBOR_AMBIGUITY_P0=PASS");

            scope.Apply(settings, [target, shortNext, longNext], [target], 100);
            await Idle();
            var opening = Signature(timeline);
            var originalItems = timeline.Items.ToArray();
            var firstTile = vm.IntentTiles.Single();
            var first = await vm.ExecuteIntentTileAsync(firstTile);
            Assert(first == 0 && Signature(timeline) == opening &&
                vm.Status.Contains("配置候補が2通り", StringComparison.Ordinal) &&
                vm.Status.Contains("もう一度", StringComparison.Ordinal),
                "NEIGHBOR_AMBIGUITY P1 first multi-result execution performs zero writes and arms explicit second execution");

            var secondTile = vm.IntentTiles.Single();
            var second = await vm.ExecuteIntentTileAsync(secondTile);
            var added = timeline.Items.Except(originalItems).OrderBy(x => x.Length).ToArray();
            Assert(second == 2 && added.Length == 2 &&
                added[0] is TextItem { Frame: 100, Length: 120, Layer: 19 } &&
                added[1] is TextItem { Frame: 100, Length: 160, Layer: 18 } &&
                vm.Status.Contains("2通り配置", StringComparison.Ordinal),
                "NEIGHBOR_AMBIGUITY P1 exact second execution places both distinct results and later result avoids earlier planned occupancy");
            var placedSignature = Signature(timeline);
            await undo.UndoAsync();
            await Idle();
            Assert(Signature(timeline) == opening,
                "NEIGHBOR_AMBIGUITY P2 full two-result fan-out is one native Undo");
            await undo.RedoAsync();
            await Idle();
            Assert(Signature(timeline) == placedSignature,
                "NEIGHBOR_AMBIGUITY P2 one native Redo restores the exact full fan-out");
            await undo.UndoAsync();
            await Idle();

            vm.RefreshIntentWorkspace();
            firstTile = vm.IntentTiles.Single();
            first = await vm.ExecuteIntentTileAsync(firstTile);
            Assert(first == 0 && Signature(timeline) == opening,
                "NEIGHBOR_AMBIGUITY P1 ambiguity can be armed again after Undo");
            longNext.Length = 70;
            var changedOpening = Signature(timeline);
            secondTile = vm.IntentTiles.Single();
            second = await vm.ExecuteIntentTileAsync(secondTile);
            Assert(second == 0 && Signature(timeline) == changedOpening &&
                vm.Status.Contains("配置候補が2通り", StringComparison.Ordinal),
                "NEIGHBOR_AMBIGUITY P1 changed Neighbor geometry cannot consume the old confirmation and becomes a new first execution");
            longNext.Length = 60;

            var tightPalette = palette with
            {
                Relation = relation with
                {
                    Layer = new RelativeLayerPolicy
                    {
                        Direction = RelativeLayerDirection.Up,
                        Offset = 1,
                        Minimum = 19,
                        Maximum = 19
                    }
                }
            };
            var tightSettings = PlacerSettingsStore.Copy(settings);
            tightSettings.IntentPalettes = [tightPalette];
            scope.Apply(tightSettings, [target, shortNext, longNext], [target], 100);
            await Idle();
            var tightOpening = Signature(timeline);
            var failedAll = false;
            try { _ = await vm.ExecuteIntentTileAsync(vm.IntentTiles.Single()); }
            catch (InvalidOperationException) { failedAll = true; }
            Assert(failedAll && Signature(timeline) == tightOpening,
                "NEIGHBOR_AMBIGUITY P2 one alternative without a saved-policy layer causes zero-write failure instead of partial placement");
            Log("NEIGHBOR_AMBIGUITY_P1=PASS");
            Log("NEIGHBOR_AMBIGUITY_P2=PASS");
        }

        var originalResolver = vm.PresetTargetResolver;
        try
        {
            using var scope = new Round3Fixture(timeline, undo);
            var config = new P3Config();
            var character = new Character
            {
                Name = "Neighbor Fanout Preset",
                TachieCharacterParameter = config,
                TachieDefaultFaceParameter = new P3DirectFace()
            };
            TachiePresetProbeTarget Resolve(Character c) =>
                new(c, config, typeof(P3Config), () => new P3DirectFace(),
                    () => ReferenceEquals(c.TachieCharacterParameter, config));

            TachiePresetCandidateDescriptor candidate;
            using (var coordinator = new TachiePresetCapabilityCoordinator(Resolve))
                candidate = (await coordinator.ScanAsync([character], () => true)).Single().Capability!.Candidates
                    .Single(x => x.CandidateIdentity == "Smile");

            var presetSource = TachiePresetSourceEntry.From(
                Resolve(character),
                candidate,
                "Fanout Smile",
                Guid.Parse("82000000-0000-4000-8000-000000000001"));
            var entry = new IntentEntry(presetSource.Id);
            var palette = new IntentPalette(
                Guid.Parse("82000000-0000-4000-8000-000000000002"),
                "Preset fan-out",
                "周囲候補",
                new IntentTargetContext
                {
                    ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
                    CharacterName = character.Name
                },
                new IntentRelation
                {
                    Anchor = IntentAnchor.SelectedStart,
                    Duration = IntentDuration.UntilRelated,
                    Neighbor = IntentNeighbor.NextSameTypeAndCharacter,
                    NeighborEdge = IntentNeighborEdge.End,
                    Fallback = IntentFallback.DoNotPlace,
                    Layer = new RelativeLayerPolicy
                    {
                        Direction = RelativeLayerDirection.Up,
                        Offset = 1,
                        Minimum = 0,
                        Maximum = 99
                    }
                },
                [entry]);
            var settings = PlacerSettingsStore.Copy(scope.Original);
            settings.ExpressionBootstrapComplete = true;
            settings.IntentPaletteRevision = 1;
            settings.Library = [];
            settings.TachiePresetSources = [presetSource];
            settings.IntentPalettes = [palette];
            settings.Palettes = [];

            var target = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20 };
            var shortNext = new VoiceItem(character) { Frame = 200, Length = 20, Layer = 20 };
            var longNext = new VoiceItem(character) { Frame = 200, Length = 60, Layer = 21 };
            scope.Apply(settings, [target, shortNext, longNext], [target], 100);
            vm.PresetTargetResolver = Resolve;
            vm.RefreshIntentWorkspace();
            await Idle();

            var opening = Signature(timeline);
            var originals = timeline.Items.ToArray();
            var first = await vm.ExecuteIntentTileAsync(vm.IntentTiles.Single());
            Assert(first == 0 && Signature(timeline) == opening,
                "NEIGHBOR_AMBIGUITY P3 registered Tachie Preset ambiguity first execution is zero-write");
            var second = await vm.ExecuteIntentTileAsync(vm.IntentTiles.Single());
            var added = timeline.Items.Except(originals).OfType<TachieFaceItem>().OrderBy(x => x.Length).ToArray();
            Assert(second == 2 && added.Length == 2 &&
                !ReferenceEquals(added[0], added[1]) &&
                added[0] is { Frame: 100, Length: 120, Layer: 19, TachieFaceParameter: P3DirectFace { Preset: "Smile" } } &&
                added[1] is { Frame: 100, Length: 160, Layer: 18, TachieFaceParameter: P3DirectFace { Preset: "Smile" } },
                "NEIGHBOR_AMBIGUITY P3 registered Tachie Preset Source forks independent pending Items for both results");
            await undo.UndoAsync();
            await Idle();
            Assert(Signature(timeline) == opening,
                "NEIGHBOR_AMBIGUITY P3 preset fan-out is one native Undo");
            Log("NEIGHBOR_AMBIGUITY_P3=PASS");
        }
        finally
        {
            vm.PresetTargetResolver = originalResolver;
        }
    }
}
