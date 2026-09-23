using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementSourceResync(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Placement Source P6 registered preset geometry Resync";
        var originalItems = timeline.Items;
        var originalSelection = timeline.SelectedItems;

        var config = new P3Config();
        var character = new Character
        {
            Name = "PS P6 Character",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = new P3DirectFace()
        };
        TachiePresetProbeTarget Resolve(Character c) =>
            new(c, config, typeof(P3Config), () => new P3DirectFace(),
                () => ReferenceEquals(c.TachieCharacterParameter, config));

        TachiePresetCandidateDescriptor smileCandidate;
        using (var coordinator = new TachiePresetCapabilityCoordinator(Resolve))
            smileCandidate = (await coordinator.ScanAsync([character], () => true))
                .Single().Capability!.Candidates
                .Single(x => x.CandidateIdentity == "Smile");

        var source = TachiePresetSourceEntry.From(
            Resolve(character),
            smileCandidate,
            "P6 Smile",
            Guid.Parse("46000000-0000-4000-8000-000000000001"));
        var presetEntry = new IntentEntry(source.Id) { DisplayAlias = "P6 Smile" };
        var presetPalette = new IntentPalette(
            Guid.Parse("46000000-0000-4000-8000-000000000010"),
            "P6 Preset Set",
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
                    Offset = 2,
                    Minimum = 0,
                    Maximum = 99
                }
            },
            [presetEntry])
        { ExpressionCandidates = true };

        var template = Template("PlacementSource/P6/Template", [
            new TachieFaceItem(character) { Frame = 4, Length = 12, Layer = 3 }
        ]);
        ItemSettings.Default.Templates.Add(template);

        try
        {
            var templateSource = TemplateResolver.Reference(
                template, "P6 Template", character.Name);
            var templateEntry = new IntentEntry(templateSource.Id)
            {
                DisplayAlias = "P6 Template"
            };
            var templatePalette = new IntentPalette(
                Guid.Parse("46000000-0000-4000-8000-000000000020"),
                "P6 Template Set",
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
                [templateEntry])
            { ExpressionCandidates = true };

            var settings = new PlacerSettings
            {
                IntentPaletteRevision = 1,
                ExpressionBootstrapComplete = true,
                Library = [templateSource],
                TachiePresetSources = [source],
                IntentPalettes = [presetPalette, templatePalette],
                NextAssociationId = 960001
            };
            SelectionPresetSettings.Upgrade(settings);
            IntentPaletteSettings.Upgrade(settings);
            PlacerSettingsStore.Validate(settings);

            const long presetSerial = 960001;
            var presetVoice = new VoiceItem(character)
            {
                Frame = 100,
                Length = 40,
                Layer = 20,
                Serif = "P6 preset voice",
                Remark = AssociationTag.TargetLine(presetSerial)
            };
            var faceParameter = new P3DirectFace { Preset = "Smile" };
            var stateHash = TachiePresetPublicState.TryHash(faceParameter)!;
            var presetTag = TachiePresetSourceAssociationTag.Create(
                Guid.Parse("46000000-0000-4000-8000-000000000100"),
                presetPalette.Id,
                source,
                stateHash);
            var presetFace = new TachieFaceItem(character)
            {
                Frame = 100,
                Length = 40,
                Layer = 18,
                TachieFaceParameter = faceParameter,
                Remark = PluginRemarks.Append(
                    PluginRemarks.Append(
                        PlacementEngine.Marker,
                        AssociationTag.SourceLine(presetSerial)),
                    presetTag.Line)
            };

            const long templateSerial = 960002;
            var templateVoice = new VoiceItem(character)
            {
                Frame = 300,
                Length = 30,
                Layer = 30,
                Serif = "P6 template voice",
                Remark = AssociationTag.TargetLine(templateSerial)
            };
            var bundle = TemplateResolver.RequireBundle(templateSource);
            var templateTag = new IntentAssociationTag(
                Guid.Parse("46000000-0000-4000-8000-000000000200"),
                templatePalette.Id,
                templateSource.Id,
                0,
                1,
                IntentAssociationTag.Hash(bundle));
            var templateFace = new TachieFaceItem(character)
            {
                Frame = 300,
                Length = 30,
                Layer = 29,
                Remark = PluginRemarks.Append(
                    PluginRemarks.Append(
                        PlacementEngine.Marker,
                        AssociationTag.SourceLine(templateSerial)),
                    templateTag.Line)
            };

            timeline.Items = [presetVoice, presetFace, templateVoice, templateFace];
            timeline.SelectedItems = [presetFace];
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();

            // Change Set geometry only; neither source/content association changes.
            var changedPresetPalette = presetPalette with
            {
                Relation = presetPalette.Relation with
                {
                    StartOffset = 2,
                    EndOffset = -1,
                    Layer = new RelativeLayerPolicy
                    {
                        Direction = RelativeLayerDirection.Down,
                        Offset = 1,
                        Minimum = 0,
                        Maximum = 99
                    }
                }
            };
            settings.IntentPalettes[0] = changedPresetPalette;

            var baseline = Signature(timeline);
            var faceReference = presetFace.TachieFaceParameter;
            var faceRemark = presetFace.Remark;
            var staged = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetFace]);

            Assert(staged.Result.Plan.UpdateCount == 1 &&
                Signature(timeline) == baseline &&
                ReferenceEquals(presetFace.TachieFaceParameter, faceReference) &&
                presetFace.Remark == faceRemark,
                "PLACEMENT_SOURCE P6 registered preset Resync preflights geometry with zero live content/Timeline mutation");

            staged.ValidateCurrent(timeline, settings);
            _ = staged.Commit(timeline, undo, settings);
            Assert(presetFace.Frame == 102 &&
                presetFace.Length == 37 &&
                presetFace.Layer == 21 &&
                ReferenceEquals(presetFace.TachieFaceParameter, faceReference) &&
                TachiePresetPublicState.TryHash(presetFace.TachieFaceParameter!) == stateHash &&
                presetFace.Remark == faceRemark,
                "PLACEMENT_SOURCE P6 registered preset Resync updates only Frame/Length/Layer through current Set relation");

            await undo.UndoAsync();
            Assert(Signature(timeline) == baseline &&
                ReferenceEquals(presetFace.TachieFaceParameter, faceReference),
                "PLACEMENT_SOURCE P6 preset geometry Resync is one native Undo and never replaces FaceParameter");

            // Manual content edits invalidate StateHash and must never be repaired/reapplied.
            ((P3DirectFace)presetFace.TachieFaceParameter!).Preset = "Neutral";
            var manualSignature = Signature(timeline);
            var manual = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetFace]);
            Assert(manual.Result.Plan.UpdateCount == 0 &&
                manual.Result.Skipped.Count == 1 &&
                Signature(timeline) == manualSignature &&
                ReferenceEquals(presetFace.TachieFaceParameter, faceReference),
                "PLACEMENT_SOURCE P6 manually edited preset state is skipped instead of silently reapplying content");
            ((P3DirectFace)presetFace.TachieFaceParameter!).Preset = "Smile";

            // Persisted source semantic changes invalidate the old association.
            settings.TachiePresetSources[0] = source with { CandidateIdentity = "Neutral" };
            var sourceChanged = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetFace]);
            Assert(sourceChanged.Result.Plan.UpdateCount == 0 &&
                sourceChanged.Result.Skipped.Count == 1 &&
                Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P6 changed registered Source semantics fail closed instead of relinking by name");
            settings.TachiePresetSources[0] = source;

            // Missing source stays local and never guesses another preset.
            settings.TachiePresetSources.Clear();
            var sourceMissing = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetFace]);
            Assert(sourceMissing.Result.Plan.UpdateCount == 0 &&
                sourceMissing.Result.Skipped.Count == 1 &&
                Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P6 missing registered Source is a local skip with zero mutation");
            settings.TachiePresetSources.Add(source);

            // Voice selection resolves the same exact v2 group.
            timeline.SelectedItems = [presetVoice];
            var byVoice = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetVoice]);
            Assert(byVoice.Result.Plan.UpdateCount == 1,
                "PLACEMENT_SOURCE P6 selecting the associated Voice resolves the same registered preset v2 group");

            // Combine one Template v1 and one registered Preset v2 resync into one Undo unit.
            var changedTemplatePalette = templatePalette with
            {
                Relation = templatePalette.Relation with
                {
                    StartOffset = 3,
                    EndOffset = -2,
                    Layer = new RelativeLayerPolicy
                    {
                        Direction = RelativeLayerDirection.Down,
                        Offset = 2,
                        Minimum = 0,
                        Maximum = 99
                    }
                }
            };
            settings.IntentPalettes[1] = changedTemplatePalette;
            timeline.SelectedItems = [presetFace, templateFace];
            var combinedBaseline = Signature(timeline);

            var templateStage = IntentAssociationResync.Create(
                timeline, settings, ExpressionPreset.Default);
            var presetStage = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetFace, templateFace]);

            templateStage.ValidateCurrent(timeline, settings);
            presetStage.ValidateCurrent(timeline, settings);
            var combinedPlan = PlacementPlan.Combine(
                timeline,
                [templateStage.Result.Plan, presetStage.Result.Plan]);

            Assert(combinedPlan.UpdateCount == 2 &&
                Signature(timeline) == combinedBaseline,
                "PLACEMENT_SOURCE P6 Template v1 and registered Preset v2 Resync fully preflight before combined mutation");

            _ = combinedPlan.Commit(timeline, undo);
            Assert(
                presetFace.Frame == 102 && presetFace.Length == 37 && presetFace.Layer == 21 &&
                templateFace.Frame == 303 && templateFace.Length == 25 && templateFace.Layer == 32 &&
                ReferenceEquals(presetFace.TachieFaceParameter, faceReference),
                "PLACEMENT_SOURCE P6 mixed Template/Preset Resync uses each owning Set relation and preserves preset content");

            await undo.UndoAsync();
            Assert(Signature(timeline) == combinedBaseline,
                "PLACEMENT_SOURCE P6 mixed Template/Preset Resync returns both updates with one native Undo");

            // Grouping/copy ambiguity remains fail-closed.
            presetFace.Group = 42;
            var groupedSignature = Signature(timeline);
            var grouped = await RegisteredPresetAssociationResync.CreateAsync(
                timeline, settings, Resolve, [presetFace]);
            Assert(grouped.Result.Plan.UpdateCount == 0 &&
                grouped.Result.Skipped.Count == 1 &&
                Signature(timeline) == groupedSignature,
                "PLACEMENT_SOURCE P6 grouped registered preset item is skipped without partial geometry mutation");
            presetFace.Group = 0;

            Log("PLACEMENT_SOURCE_P6=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(template);
            timeline.Items = originalItems;
            timeline.SelectedItems = originalSelection;
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
        }
    }
}
