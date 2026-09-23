using System.Reflection;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementSourceNormalTiles(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Placement Source P3 normal targeted tiles";
        var vm = ViewModel!;
        var settingsField = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var originalSettings = (PlacerSettings)settingsField.GetValue(vm)!;
        var originalResolver = vm.PresetTargetResolver;
        var originalItems = timeline.Items;
        var originalSelection = timeline.SelectedItems;

        var config = new P3Config();
        var character = new Character
        {
            Name = "PS P3 Character",
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
            "Preset Smile",
            Guid.Parse("31000000-0000-4000-8000-000000000001"));

        var template = Template("PlacementSource/P3/Template", [
            new TachieFaceItem(character) { Frame = 9, Length = 13, Layer = 4 }
        ]);
        ItemSettings.Default.Templates.Add(template);

        try
        {
            var templateSource = TemplateResolver.Reference(template, "Template Smile", character.Name);
            var templateEntry = new IntentEntry(templateSource.Id) { DisplayAlias = "Template Smile" };
            var presetEntry = new IntentEntry(presetSource.Id) { DisplayAlias = "Preset Smile" };
            var palette = new IntentPalette(
                Guid.Parse("31000000-0000-4000-8000-000000000010"),
                "P3 mixed Set",
                "通常配置",
                new IntentTargetContext
                {
                    ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
                    CharacterName = character.Name
                },
                new IntentRelation
                {
                    Duration = IntentDuration.TargetSpan,
                    Layer = new RelativeLayerPolicy
                    {
                        Direction = RelativeLayerDirection.Up,
                        Offset = 2,
                        Minimum = 0,
                        Maximum = 99
                    }
                },
                [templateEntry, presetEntry]);

            var fixture = PlacerSettingsStore.Copy(originalSettings);
            fixture.ExpressionBootstrapComplete = true;
            fixture.IntentPaletteRevision = 1;
            fixture.Library = [templateSource];
            fixture.TachiePresetSources = [presetSource];
            fixture.IntentPalettes = [palette];
            PlacerSettingsStore.Validate(fixture);

            var voice = new VoiceItem(character)
            {
                Frame = 100,
                Length = 40,
                Layer = 20,
                Serif = "P3 target"
            };
            var blocker = new TextItem
            {
                Frame = 100,
                Length = 40,
                Layer = 18,
                Remark = "P3 shared collision blocker"
            };

            settingsField.SetValue(vm, fixture);
            vm.PresetTargetResolver = Resolve;
            timeline.Items = [voice, blocker];
            timeline.SelectedItems = [voice];
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
            vm.ActivateIntentWorkspace();
            vm.SetLegacyWorkspace(false);
            vm.RefreshIntentWorkspace();
            await Idle();

            Assert(vm.IntentSets.Count == 1 &&
                vm.IntentTiles.Count == 2 &&
                vm.IntentTiles.All(x => x.Available) &&
                vm.IntentTiles.Select(x => x.Label).SequenceEqual(["Template Smile", "Preset Smile"]),
                "PLACEMENT_SOURCE P3 one Character-bound targeted Set exposes mixed Template and registered TachiePreset tiles");

            var baseline = Signature(timeline);
            var templateTile = vm.IntentTiles[0];
            var presetTile = vm.IntentTiles[1];

            var templateChanged = await vm.ExecuteIntentTileAsync(templateTile);
            var templateAdded = timeline.Items.Except(new IItem[] { voice, blocker }).Single();
            Assert(templateChanged == 1 &&
                templateAdded is TachieFaceItem &&
                templateAdded.Frame == 100 &&
                templateAdded.Length == 40 &&
                templateAdded.Layer == 17,
                "PLACEMENT_SOURCE P3 Template tile through async Source path uses the Set TargetSpan/up2/collision relation");
            await undo.UndoAsync();
            await Idle();
            Assert(Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P3 Template tile remains one native Undo unit through the new async targeted path");

            vm.RefreshIntentWorkspace();
            presetTile = vm.IntentTiles.Single(x => x.Label == "Preset Smile");
            var presetChanged = await vm.ExecuteIntentTileAsync(presetTile);
            var presetAdded = timeline.Items.Except(new IItem[] { voice, blocker }).Single() as TachieFaceItem;
            Assert(presetChanged == 1 &&
                presetAdded is {
                    Frame: 100,
                    Length: 40,
                    Layer: 17,
                    TachieFaceParameter: P3DirectFace { Preset: "Smile" }
                } &&
                !ReferenceEquals(presetAdded.TachieFaceParameter, character.TachieDefaultFaceParameter),
                "PLACEMENT_SOURCE P3 registered TachiePreset tile works outside expression list and uses the same Set geometry as Template");
            await undo.UndoAsync();
            await Idle();
            Assert(Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P3 preset tile placement is one native Undo unit");

            var downPalette = palette with
            {
                Relation = palette.Relation with
                {
                    Layer = new RelativeLayerPolicy
                    {
                        Direction = RelativeLayerDirection.Down,
                        Offset = 1,
                        Minimum = 0,
                        Maximum = 99
                    }
                }
            };
            fixture.IntentPalettes[0] = downPalette;
            vm.RefreshIntentWorkspace();
            presetTile = vm.IntentTiles.Single(x => x.Label == "Preset Smile");
            _ = await vm.ExecuteIntentTileAsync(presetTile);
            presetAdded = timeline.Items.Except(new IItem[] { voice, blocker }).Single() as TachieFaceItem;
            Assert(presetAdded is { Frame: 100, Length: 40, Layer: 21 },
                "PLACEMENT_SOURCE P3 changing the owning Set relation changes registered preset placement with no preset-only geometry");
            await undo.UndoAsync();
            await Idle();

            var noCharacterPalette = downPalette with
            {
                Id = Guid.Parse("31000000-0000-4000-8000-000000000011"),
                Target = downPalette.Target with { CharacterName = null },
                Entries = [presetEntry]
            };
            fixture.IntentPalettes = [noCharacterPalette];
            vm.RefreshIntentWorkspace();
            Assert(vm.IntentTiles.Count == 1 &&
                !vm.IntentTiles[0].Available &&
                vm.IntentTiles[0].Detail.Contains("キャラクター", StringComparison.Ordinal),
                "PLACEMENT_SOURCE P3 registered preset source is not admitted by a non-Character-bound targeted Set");

            fixture.IntentPalettes = [palette];
            vm.RefreshIntentWorkspace();
            var execution = await IntentExecutionPlan.CreateAsync(
                timeline, palette, presetEntry, fixture, Resolve);
            var settingsBeforeCommit = Signature(timeline);
            fixture.TachiePresetSources[0] = presetSource with { CandidateIdentity = "Neutral" };
            var staleRejected = false;
            try { _ = execution.Commit(timeline, undo, fixture); }
            catch (InvalidOperationException) { staleRejected = true; }
            Assert(staleRejected && Signature(timeline) == settingsBeforeCommit,
                "PLACEMENT_SOURCE P3 changing persisted preset Source semantics after planning rejects commit with zero Timeline writes");
            fixture.TachiePresetSources[0] = presetSource;

            Assert(Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P3 proof leaves Timeline at its opening state after Undo/failure paths");
            Log("PLACEMENT_SOURCE_P3=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(template);
            vm.PresetTargetResolver = originalResolver;
            settingsField.SetValue(vm, originalSettings);
            timeline.Items = originalItems;
            timeline.SelectedItems = originalSelection;
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
            vm.RefreshIntentWorkspace();
        }
    }
}
