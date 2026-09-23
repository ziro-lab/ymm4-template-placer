using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementSourceExpressions(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Placement Source P4/P5 expression unification";
        var originalItems = timeline.Items;
        var originalSelection = timeline.SelectedItems;

        var config = new P3Config();
        var character = new Character
        {
            Name = "PS P45 Character",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = new P3DirectFace()
        };
        TachiePresetProbeTarget Resolve(Character c) =>
            new(c, config, typeof(P3Config), () => new P3DirectFace(),
                () => ReferenceEquals(c.TachieCharacterParameter, config));

        IReadOnlyList<TachiePresetCandidateDescriptor> candidates;
        using (var coordinator = new TachiePresetCapabilityCoordinator(Resolve))
            candidates = (await coordinator.ScanAsync([character], () => true))
                .Single().Capability!.Candidates;
        var smileCandidate = candidates.Single(x => x.CandidateIdentity == "Smile");
        var neutralCandidate = candidates.Single(x => x.CandidateIdentity == "Neutral");
        var target = Resolve(character);
        var smileSource = TachiePresetSourceEntry.From(
            target, smileCandidate, "Smile登録",
            Guid.Parse("45000000-0000-4000-8000-000000000001"));
        var neutralSource = TachiePresetSourceEntry.From(
            target, neutralCandidate, "Neutral登録",
            Guid.Parse("45000000-0000-4000-8000-000000000002"));

        var template = Template("PlacementSource/P45/Template", [
            new TachieFaceItem(character) { Frame = 8, Length = 12, Layer = 4 }
        ]);
        ItemSettings.Default.Templates.Add(template);

        try
        {
            var templateSource = TemplateResolver.Reference(
                template, "Template登録", character.Name);
            var templateEntry = new IntentEntry(templateSource.Id)
            {
                DisplayAlias = "Template登録"
            };
            var smileEntry = new IntentEntry(smileSource.Id)
            {
                DisplayAlias = "Smile登録"
            };
            var neutralEntry = new IntentEntry(neutralSource.Id)
            {
                DisplayAlias = "Neutral登録"
            };
            var palette = new IntentPalette(
                Guid.Parse("45000000-0000-4000-8000-000000000010"),
                "P45 Expression Set",
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
                [templateEntry, smileEntry, neutralEntry])
            { ExpressionCandidates = true };

            var settings = new PlacerSettings
            {
                IntentPaletteRevision = 1,
                ExpressionBootstrapComplete = true,
                Library = [templateSource],
                TachiePresetSources = [smileSource, neutralSource],
                IntentPalettes = [palette],
                NextAssociationId = 950001
            };
            SelectionPresetSettings.Upgrade(settings);
            IntentPaletteSettings.Upgrade(settings);
            PlacerSettingsStore.Validate(settings);

            var voice = new VoiceItem(character)
            {
                Frame = 100,
                Length = 40,
                Layer = 20,
                Serif = "P45 target"
            };
            var blocker = new TextItem
            {
                Frame = 100,
                Length = 40,
                Layer = 18,
                Remark = "P45 blocker"
            };
            timeline.Items = [voice, blocker];
            timeline.SelectedItems = [voice];
            timeline.RefreshTimelineLengthAndMaxLayer();
            undo.Record();
            var baseline = Signature(timeline);

            var voiceSnapshot = new VoiceSnapshot(
                voice, voice.CharacterName, voice.Frame, voice.Length,
                voice.Serif ?? "", voice.Layer);
            var registered = RegisteredPresetExpressionCatalog.Read(settings);
            Assert(registered.Count == 2 &&
                registered.Select(x => x.SourceId).SequenceEqual([smileSource.Id, neutralSource.Id]) &&
                registered.All(x => x.PaletteId == palette.Id),
                "PLACEMENT_SOURCE P4 Expression Set projects registered TachiePreset Source memberships in authoritative Set order");

            var captured = timeline.Items.Select(item =>
                item is VoiceItem v
                    ? new ExpressionCapturedItem(
                        item, true, v.CharacterName, item.Group, item.Remark ?? "")
                    : new ExpressionCapturedItem(
                        item, false, "", item.Group, item.Remark ?? ""))
                .ToArray();
            var capability = new TachiePresetCharacterCapability(
                character.Name,
                TachiePresetCapabilityResult.Supported(target.Fingerprint, candidates),
                null);
            var snapshot = new ExpressionHostSnapshot(
                1,
                timeline,
                [voiceSnapshot],
                captured,
                [],
                true,
                null,
                [],
                [])
            {
                SourceMode = ExpressionSourceMode.TachiePreset,
                PresetCapabilities = [capability],
                RegisteredPresetCandidates = registered
            };
            var prepared = ExpressionPreparation.Prepare(
                snapshot, CancellationToken.None);
            var preparedRow = prepared.Rows.Single();
            var registeredChoices = preparedRow.Choices
                .Where(x => x.RegisteredPreset != null)
                .ToArray();
            Assert(registeredChoices.Length == 2 &&
                registeredChoices.All(x => x.IsAvailable) &&
                registeredChoices.Select(x => x.RegisteredPreset!.SourceId)
                    .SequenceEqual([smileSource.Id, neutralSource.Id]) &&
                preparedRow.Choices.Count(x => x.TachiePreset != null) >= 2 &&
                Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P4 TachiePreset view keeps registered Set sources and legacy discovered preset choices as zero-write candidate projections");
            Log("PLACEMENT_SOURCE_P4=PASS");

            var row = new AssignmentRow(
                1, voiceSnapshot, preparedRow.Choices, true);
            var smileChoice = row.Choices.Single(
                x => x.RegisteredPreset?.SourceId == smileSource.Id);
            var neutralChoice = row.Choices.Single(
                x => x.RegisteredPreset?.SourceId == neutralSource.Id);

            var templateCatalog = IntentExpressionCatalog.Read(settings);
            var templateChoice = IntentExpressionCatalog
                .Choices(voiceSnapshot, templateCatalog)
                .Single(x => x.Template?.IntentSource?.Entry.SourceId == templateSource.Id);

            var allocator = new IntentAssociationSerialAllocator(
                timeline, settings.NextAssociationId);
            var templateMutation = IntentExpressionMutation.Create(
                timeline, row, templateChoice, settings, allocator, false);
            templateMutation.ValidateCurrent(timeline, settings);
            _ = templateMutation.Plan.Commit(timeline, undo);
            var firstTemplateItem = timeline.Items.Except(new IItem[] { voice, blocker }).Single();

            var templateAssociation = ManagedExpressionReader.Read(timeline, voice);
            Assert(templateAssociation.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.Template &&
                templateAssociation.Bundle.Descriptor.Template!.Palette == palette.Id &&
                templateAssociation.Bundle.Descriptor.Template.Entry == templateSource.Id,
                "PLACEMENT_SOURCE P5 baseline Template expression uses the existing v1 Template association unchanged");

            row.SelectedChoice = smileChoice;
            var smileMutation = await RegisteredPresetExpressionMutation.CreateAsync(
                timeline, row, smileChoice.RegisteredPreset!, settings,
                Resolve, allocator.NextSerial);
            smileMutation.ValidateCurrent(timeline, settings);
            _ = smileMutation.Plan.Commit(timeline, undo);

            var smileAssociation = ManagedExpressionReader.Read(timeline, voice);
            var smileDescriptor = smileAssociation.Bundle?.Descriptor.RegisteredTachiePreset;
            var smileFace = smileAssociation.Bundle?.Members.Single() as TachieFaceItem;
            Assert(smileAssociation.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.RegisteredTachiePreset &&
                smileDescriptor != null &&
                smileDescriptor.Palette == palette.Id &&
                smileDescriptor.Source == smileSource.Id &&
                smileDescriptor.SourceHash == smileSource.SemanticHash() &&
                smileFace is
                {
                    Frame: 100,
                    Length: 40,
                    Layer: 17,
                    TachieFaceParameter: P3DirectFace { Preset: "Smile" }
                } &&
                !timeline.Items.Contains(firstTemplateItem),
                "PLACEMENT_SOURCE P5 Template -> registered preset replacement removes only the managed Template bundle and applies Set-owned geometry");

            row.SelectedChoice = neutralChoice;
            var neutralMutation = await RegisteredPresetExpressionMutation.CreateAsync(
                timeline, row, neutralChoice.RegisteredPreset!, settings,
                Resolve, smileMutation.NextSerial);
            neutralMutation.ValidateCurrent(timeline, settings);
            _ = neutralMutation.Plan.Commit(timeline, undo);

            var neutralAssociation = ManagedExpressionReader.Read(timeline, voice);
            var neutralFace = neutralAssociation.Bundle?.Members.Single() as TachieFaceItem;
            Assert(neutralAssociation.Bundle?.Descriptor is
                { Kind: ManagedExpressionSourceKind.RegisteredTachiePreset, RegisteredTachiePreset: { } neutralDescriptor } &&
                neutralDescriptor.Source == neutralSource.Id &&
                neutralFace?.TachieFaceParameter is P3DirectFace { Preset: "Neutral" } &&
                neutralFace.Frame == 100 && neutralFace.Length == 40 && neutralFace.Layer == 17 &&
                !ReferenceEquals(neutralFace, smileFace),
                "PLACEMENT_SOURCE P5 registered preset -> registered preset replacement uses a fresh item and keeps the owning Set relation");

            var templateAgain = IntentExpressionMutation.Create(
                timeline, row, templateChoice, settings,
                new IntentAssociationSerialAllocator(timeline, neutralMutation.NextSerial),
                false);
            templateAgain.ValidateCurrent(timeline, settings);
            _ = templateAgain.Plan.Commit(timeline, undo);

            var finalTemplate = ManagedExpressionReader.Read(timeline, voice);
            Assert(finalTemplate.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.Template &&
                finalTemplate.Bundle.Descriptor.Template!.Palette == palette.Id &&
                finalTemplate.Bundle.Descriptor.Template.Entry == templateSource.Id,
                "PLACEMENT_SOURCE P5 registered preset -> Template replacement returns to the existing Template association path");

            await undo.UndoAsync();
            await undo.UndoAsync();
            await undo.UndoAsync();
            await undo.UndoAsync();
            Assert(Signature(timeline) == baseline,
                "PLACEMENT_SOURCE P5 four cross-source placements each remain one native Undo unit");

            row.SelectedChoice = smileChoice;
            var manualMutation = await RegisteredPresetExpressionMutation.CreateAsync(
                timeline, row, smileChoice.RegisteredPreset!, settings,
                Resolve, settings.NextAssociationId);
            manualMutation.ValidateCurrent(timeline, settings);
            _ = manualMutation.Plan.Commit(timeline, undo);
            var manualAssociation = ManagedExpressionReader.Read(timeline, voice);
            var manualFace = (TachieFaceItem)manualAssociation.Bundle!.Members.Single();
            ((P3DirectFace)manualFace.TachieFaceParameter!).Preset = "Neutral";
            var editedSignature = Signature(timeline);

            var manualRejected = false;
            try
            {
                _ = await RegisteredPresetExpressionMutation.CreateAsync(
                    timeline, row, neutralChoice.RegisteredPreset!, settings,
                    Resolve, manualMutation.NextSerial);
            }
            catch (InvalidOperationException) { manualRejected = true; }
            Assert(manualRejected && Signature(timeline) == editedSignature &&
                timeline.Items.Contains(manualFace),
                "PLACEMENT_SOURCE P5 manual FaceParameter edit breaks StateHash and blocks destructive registered-preset replacement");

            var oldTag = TachiePresetAssociationTag.Create(
                Guid.NewGuid(), 0, 1, smileCandidate,
                TachiePresetPublicState.TryHash(new P3DirectFace { Preset = "Smile" })!);
            var newTag = TachiePresetSourceAssociationTag.Create(
                Guid.NewGuid(), palette.Id, smileSource,
                TachiePresetPublicState.TryHash(new P3DirectFace { Preset = "Smile" })!);
            Assert(ManagedExpressionSourceDescriptor.Read(oldTag.Line, out var oldDescriptor) == AssociationTagState.Valid &&
                oldDescriptor?.Kind == ManagedExpressionSourceKind.TachiePreset &&
                ManagedExpressionSourceDescriptor.Read(newTag.Line, out var newDescriptor) == AssociationTagState.Valid &&
                newDescriptor?.Kind == ManagedExpressionSourceKind.RegisteredTachiePreset &&
                newTag.Line.Length <= 256,
                "PLACEMENT_SOURCE P5 old v0.5 preset association and new Set-owned preset association remain distinct and readable");

            Log("PLACEMENT_SOURCE_P5=PASS");
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
