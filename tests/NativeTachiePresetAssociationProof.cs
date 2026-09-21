using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyTachiePresetAssociationSeam(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P6 managed association seam";
        using var scope = new Round3Fixture(timeline, undo);
        var checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P6 " + name); checks.Add(name); }

        var character = new Character { Name = "P6 association" };
        var fingerprint = new TachiePresetCapabilityFingerprint(
            "YMM4 4.55.1.1 Lite",
            character.Name,
            new string('c', 64),
            "Fixture.Plugin",
            "Fixture.Face",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var candidate = new TachiePresetCandidateDescriptor(
            fingerprint,
            new TachiePresetRouteDescriptor(TachiePresetRouteKind.DirectNamedProperty, "Fixture.Face.Preset"),
            "Smile",
            "Smile",
            TachiePresetCapabilityLevel.Strong);
        var stateHash = new string('d', 64);
        var presetGroup = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var presetTag = TachiePresetAssociationTag.Create(presetGroup, 0, 1, candidate, stateHash);

        Check(presetTag.Line.Length <= 256 &&
            TachiePresetAssociationTag.Read(presetTag.Line, out var parsedPreset) == AssociationTagState.Valid &&
            parsedPreset == presetTag,
            "versioned preset descriptor is canonical, bounded and roundtrips exactly");
        Check(TachiePresetAssociationTag.CapabilityIdentity(fingerprint) == presetTag.CapabilityHash &&
            TachiePresetAssociationTag.CandidateIdentity(candidate) == presetTag.CandidateHash,
            "preset descriptor binds the exact immutable capability and route/candidate identity");
        Check(TachiePresetAssociationTag.Read(presetTag.Line + "\n" + presetTag.Line, out _) == AssociationTagState.Invalid &&
            TachiePresetAssociationTag.Read(presetTag.Line.Replace(stateHash, stateHash.ToUpperInvariant(), StringComparison.Ordinal), out _) == AssociationTagState.Invalid,
            "duplicate and noncanonical preset descriptors fail closed");

        var templateGroup = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var palette = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var entry = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var geometryHash = new string('0', 64);
        var templateTag = new IntentAssociationTag(templateGroup, palette, entry, 0, 1, geometryHash);
        var expectedTemplateLine = "CWT_TPL:B=1;11111111111111111111111111111111;22222222222222222222222222222222;33333333333333333333333333333333;0;1;" + geometryHash;
        Check(templateTag.Line == expectedTemplateLine &&
            IntentAssociationTag.Read(expectedTemplateLine, out var parsedTemplate) == AssociationTagState.Valid &&
            parsedTemplate == templateTag,
            "existing Template IntentAssociationTag bytes remain unchanged");

        var templateVoice = new VoiceItem(character) { Frame = 100, Length = 20, Layer = 20, Serif = "template" };
        var presetVoice = new VoiceItem(character) { Frame = 200, Length = 20, Layer = 20, Serif = "preset" };
        var manualVoice = new VoiceItem(character) { Frame = 300, Length = 20, Layer = 20, Serif = "manual" };
        const long templateSerial = 810001;
        const long presetSerial = 810002;
        templateVoice.Remark = AssociationTag.TargetLine(templateSerial);
        presetVoice.Remark = AssociationTag.TargetLine(presetSerial);
        var templateFace = new TachieFaceItem(character)
        {
            Frame = 100, Length = 10, Layer = 4,
            Remark = PluginRemarks.Append(PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(templateSerial)), templateTag.Line)
        };
        var presetFace = new TachieFaceItem(character)
        {
            Frame = 200, Length = 10, Layer = 4,
            Remark = PluginRemarks.Append(PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(presetSerial)), presetTag.Line)
        };
        var manualFace = new TachieFaceItem(character)
        {
            Frame = 300, Length = 10, Layer = 4, Remark = "manual face"
        };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.ExpressionBootstrapComplete = true; settings.LegacyWorkspace = false;
        scope.Apply(settings, [templateVoice, templateFace, presetVoice, presetFace, manualVoice, manualFace], []);
        await Idle();

        var signature = Signature(timeline);
        var settingsJson = JsonSerializer.Serialize(scope.Current);
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        bool DiskSame() => disk == null ? !File.Exists(PlacerSettingsStore.DefaultPath) :
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(disk);

        var templateAssociation = ManagedExpressionReader.Read(timeline, templateVoice);
        var presetAssociation = ManagedExpressionReader.Read(timeline, presetVoice);
        var manualAssociation = ManagedExpressionReader.Read(timeline, manualVoice);
        Check(templateAssociation.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.Template &&
            templateAssociation.Bundle.Descriptor.Template == templateTag &&
            templateAssociation.Bundle.Members.Count == 1 && ReferenceEquals(templateAssociation.Bundle.Members[0], templateFace),
            "common live reader preserves exact Template source identity and members");
        Check(presetAssociation.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.TachiePreset &&
            presetAssociation.Bundle.Descriptor.TachiePreset == presetTag &&
            presetAssociation.Bundle.Members.Count == 1 && ReferenceEquals(presetAssociation.Bundle.Members[0], presetFace),
            "common live reader recognizes exact TachiePreset source identity and members");
        Check(manualAssociation.Serial == null && manualAssociation.Bundle == null,
            "manual unassociated expression content remains outside managed ownership");

        var compatibility = ManagedIntentExpressionReader.Read(timeline, templateVoice);
        Check(compatibility.Bundle?.Descriptor == templateTag &&
            compatibility.Bundle.Members.Count == 1 && ReferenceEquals(compatibility.Bundle.Members[0], templateFace),
            "existing Template reader compatibility wrapper preserves the historical descriptor");
        RejectWithoutMutation(timeline,
            () => { _ = ManagedIntentExpressionReader.Read(timeline, presetVoice); },
            "TP-P6 Template mutation wrapper refuses a TachiePreset bundle before P7/P8");

        ExpressionCapturedItem Capture(IItem item) => new(
            item,
            item is VoiceItem,
            item is VoiceItem voice ? voice.CharacterName : "",
            item.Group,
            item.Remark ?? "");
        var captured = timeline.Items.Select(Capture).ToArray();
        var index = ExpressionAssociationIndex.Build(captured, CancellationToken.None);
        Check(index.Read(templateVoice).Descriptor?.Kind == ManagedExpressionSourceKind.Template &&
            index.Read(presetVoice).Descriptor?.Kind == ManagedExpressionSourceKind.TachiePreset &&
            !index.Read(templateVoice).IsError && !index.Read(presetVoice).IsError,
            "background association index carries the same discriminated source union");

        var capability = TachiePresetCapabilityResult.Supported(fingerprint, [candidate]);
        var presetSnapshot = new ExpressionHostSnapshot(
            1,
            timeline,
            [new VoiceSnapshot(presetVoice, presetVoice.CharacterName, presetVoice.Frame, presetVoice.Length, presetVoice.Serif ?? "", presetVoice.Layer)],
            captured,
            [],
            true,
            null,
            [],
            [])
        {
            SourceMode = ExpressionSourceMode.TachiePreset,
            PresetCapabilities = [new(character.Name, capability, null)]
        };
        var presetPrepared = ExpressionPreparation.Prepare(presetSnapshot, CancellationToken.None).Rows.Single();
        Check(presetPrepared.Selected?.TachiePreset == candidate &&
            !presetPrepared.Selected.IsCurrentOtherSource && presetPrepared.Selected.IsAvailable,
            "preset projection resolves a current managed TachiePreset through exact capability/candidate hashes");

        var templateSnapshot = presetSnapshot with
        {
            SourceMode = ExpressionSourceMode.Template,
            PresetCapabilities = []
        };
        var templatePrepared = ExpressionPreparation.Prepare(templateSnapshot, CancellationToken.None).Rows.Single();
        Check(templatePrepared.Selected?.IsCurrentOtherSource == true &&
            templatePrepared.Selected.TachiePreset == null &&
            templatePrepared.SourceNotice?.Contains("立ち絵プリセット", StringComparison.Ordinal) == true,
            "Template projection reports a valid current TachiePreset as other-source instead of corrupt");

        var alternate = new TachiePresetCandidateDescriptor(
            fingerprint,
            candidate.Route,
            "Neutral",
            "Neutral",
            TachiePresetCapabilityLevel.Strong);
        var missingSnapshot = presetSnapshot with
        {
            PresetCapabilities = [new(character.Name, TachiePresetCapabilityResult.Supported(fingerprint, [alternate]), null)]
        };
        var missingPrepared = ExpressionPreparation.Prepare(missingSnapshot, CancellationToken.None).Rows.Single();
        Check(missingPrepared.Selected?.IsAvailable == false &&
            !missingPrepared.Selected.IsCurrentOtherSource &&
            missingPrepared.SourceNotice?.Contains("一意に再確認", StringComparison.Ordinal) == true,
            "same-source managed preset becomes explicit unavailable when its exact descriptor cannot be re-resolved");

        var basePresetRemark = presetFace.Remark;
        var baseItems = timeline.Items;
        void ExpectReaderFailure(string remark, string name)
        {
            presetFace.Remark = remark;
            var failed = false;
            try { _ = ManagedExpressionReader.Read(timeline, presetVoice); }
            catch (InvalidOperationException) { failed = true; }
            finally { presetFace.Remark = basePresetRemark; }
            Check(failed, name);
        }

        ExpectReaderFailure(
            PluginRemarks.Append(basePresetRemark, new IntentAssociationTag(presetGroup, palette, entry, 0, 1, geometryHash).Line),
            "mixed Template/TachiePreset source tags fail closed");
        ExpectReaderFailure(
            basePresetRemark + "\n" + presetTag.Line,
            "duplicate TachiePreset source tags fail closed");
        ExpectReaderFailure(
            PluginRemarks.Append(PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(presetSerial)), "user data only"),
            "missing source-specific descriptor fails closed");
        var countTwo = TachiePresetAssociationTag.Create(presetGroup, 0, 2, candidate, stateHash);
        ExpectReaderFailure(
            PluginRemarks.Append(PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(presetSerial)), countTwo.Line),
            "missing managed member fails closed");

        var copied = new TachieFaceItem(character)
        {
            Frame = presetFace.Frame + 1,
            Length = presetFace.Length,
            Layer = presetFace.Layer + 1,
            Remark = basePresetRemark
        };
        timeline.Items = baseItems.Add(copied);
        var copiedFailed = false;
        try { _ = ManagedExpressionReader.Read(timeline, presetVoice); }
        catch (InvalidOperationException) { copiedFailed = true; }
        finally { timeline.Items = baseItems; timeline.RefreshTimelineLengthAndMaxLayer(); }
        Check(copiedFailed, "copied duplicate managed member fails closed");

        var mixedGroup = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var templatePart = new IntentAssociationTag(mixedGroup, palette, entry, 0, 2, geometryHash);
        var presetPart = TachiePresetAssociationTag.Create(mixedGroup, 1, 2, candidate, stateHash);
        var second = new TachieFaceItem(character)
        {
            Frame = presetFace.Frame + 1, Length = 10, Layer = 5,
            Remark = PluginRemarks.Append(PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(presetSerial)), presetPart.Line)
        };
        presetFace.Remark = PluginRemarks.Append(PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(presetSerial)), templatePart.Line);
        timeline.Items = baseItems.Add(second);
        var crossKindFailed = false;
        try { _ = ManagedExpressionReader.Read(timeline, presetVoice); }
        catch (InvalidOperationException) { crossKindFailed = true; }
        finally
        {
            presetFace.Remark = basePresetRemark;
            timeline.Items = baseItems;
            timeline.RefreshTimelineLengthAndMaxLayer();
        }
        Check(crossKindFailed, "one managed group cannot mix Template and TachiePreset source kinds");

        var cleaned = PluginRemarks.WithoutAssociation("user\n" + presetTag.Line + "\ninline " + TachiePresetAssociationTag.Prefix + "keep");
        Check(cleaned.Contains("user", StringComparison.Ordinal) &&
            !cleaned.Split('\n').Any(x => x == presetTag.Line) &&
            cleaned.Contains("inline " + TachiePresetAssociationTag.Prefix + "keep", StringComparison.Ordinal),
            "association cleanup strips only complete preset tag lines and preserves user prose");

        await Idle();
        Check(Signature(timeline) == signature &&
            JsonSerializer.Serialize(scope.Current) == settingsJson &&
            DiskSame(),
            "P6 reader/index/projection proofs finish with zero product Timeline/settings mutation");

        File.WriteAllText(Path.Combine(output, "tachie-preset-association.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Association/1",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_ASSOCIATION_P6=PASS");
    }
}
