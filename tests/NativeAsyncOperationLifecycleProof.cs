using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyAsyncOperationLifecycle(Timeline timeline, UndoRedoManager undo)
    {
        stage = "AUDIT B01 async placement/resync task lifetime";
        var vm = ViewModel!;
        var view = View!;
        var originalResolver = vm.PresetTargetResolver;

        using var scope = new Round3Fixture(timeline, undo);
        var config = new P3Config();
        var character = new Character
        {
            Name = "Audit B01",
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

        var source = TachiePresetSourceEntry.From(
            Resolve(character),
            candidate,
            "Audit B01 Smile",
            Guid.Parse("83000000-0000-4000-8000-000000000001"));
        var entry = new IntentEntry(source.Id) { DisplayAlias = "Audit B01 Smile" };
        var palette = new IntentPalette(
            Guid.Parse("83000000-0000-4000-8000-000000000010"),
            "Audit B01 Set",
            "表情",
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
                    Offset = 1,
                    Minimum = 0,
                    Maximum = 99
                }
            },
            [entry])
        { ExpressionCandidates = true };

        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.ExpressionBootstrapComplete = true;
        fixture.IntentPaletteRevision = 1;
        fixture.Library = [];
        fixture.TachiePresetSources = [source];
        fixture.IntentPalettes = [palette];
        fixture.Palettes = [];

        try
        {
            var voice = new VoiceItem(character)
            {
                Frame = 100,
                Length = 40,
                Layer = 20,
                Serif = "audit placement"
            };
            scope.Apply(fixture, [voice], [voice], 100);
            view.PaletteTab.IsSelected = true;
            await Idle();

            var placementBefore = Signature(timeline);
            var switched = false;
            vm.PresetTargetResolver = c =>
            {
                var target = Resolve(c);
                if (!switched)
                {
                    switched = true;
                    view.ExpressionTab.IsSelected = true;
                }
                return target;
            };

            var placementCancelled = false;
            try { _ = await vm.ExecuteIntentTileAsync(vm.IntentTiles.Single()); }
            catch (OperationCanceledException) { placementCancelled = true; }
            await Idle();

            Assert(placementCancelled && switched && Signature(timeline) == placementBefore,
                "AUDIT_B01 targeted async placement cancels with zero writes when the initiating task/tab changes before commit");

            view.PaletteTab.IsSelected = true;
            vm.PresetTargetResolver = Resolve;
            await Idle();

            const long serial = 830001;
            var resyncVoice = new VoiceItem(character)
            {
                Frame = 300,
                Length = 50,
                Layer = 20,
                Serif = "audit resync",
                Remark = AssociationTag.TargetLine(serial)
            };
            var faceParameter = new P3DirectFace { Preset = "Smile" };
            var stateHash = TachiePresetPublicState.TryHash(faceParameter)!;
            var group = Guid.Parse("83000000-0000-4000-8000-000000000100");
            var presetTag = TachiePresetSourceAssociationTag.Create(
                group, palette.Id, source, stateHash);
            var resyncFace = new TachieFaceItem(character)
            {
                Frame = 300,
                Length = 50,
                Layer = 19,
                TachieFaceParameter = faceParameter,
                Remark = PluginRemarks.Append(
                    PluginRemarks.Append(
                        PlacementEngine.Marker,
                        AssociationTag.SourceLine(serial)),
                    presetTag.Line)
            };

            scope.Apply(fixture, [resyncVoice, resyncFace], [resyncFace], 300);
            view.ExpressionTab.IsSelected = true;
            await Idle();

            var resyncBefore = Signature(timeline);
            var deactivated = false;
            vm.PresetTargetResolver = c =>
            {
                var target = Resolve(c);
                if (!deactivated)
                {
                    deactivated = true;
                    vm.DeactivateIntentWorkspace();
                }
                return target;
            };

            var resyncCancelled = false;
            try { _ = await vm.ResyncAsync(); }
            catch (OperationCanceledException) { resyncCancelled = true; }
            await Idle();

            Assert(resyncCancelled && deactivated && Signature(timeline) == resyncBefore,
                "AUDIT_B01 registered-preset async Resync cancels with zero writes when the Tool workspace lifetime ends before commit");

            Log("AUDIT_B01=PASS");
        }
        finally
        {
            vm.PresetTargetResolver = originalResolver;
            vm.ActivateIntentWorkspace();
            view.PaletteTab.IsSelected = true;
            await Idle();
        }
    }
}
