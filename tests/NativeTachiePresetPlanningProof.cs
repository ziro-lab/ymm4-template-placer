using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyTachiePresetMutationPlanning(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P7 mutation planning";
        using var scope = new Round3Fixture(timeline, undo);
        var checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P7 " + name); checks.Add(name); }

        var config = new P3Config();
        var defaultFace = new P3DirectFace();
        var character = new Character
        {
            Name = "P7 planning",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = defaultFace
        };
        TachiePresetProbeTarget Resolve(Character c) =>
            new(c, config, typeof(P3Config), () => new P3DirectFace(),
                () => ReferenceEquals(c.TachieCharacterParameter, config));

        TachiePresetCandidateDescriptor candidate;
        using (var coordinator = new TachiePresetCapabilityCoordinator(Resolve))
        {
            candidate = (await coordinator.ScanAsync([character], () => true)).Single().Capability!.Candidates
                .Single(x => x.CandidateIdentity == "Smile");
        }
        Check(candidate.Confidence == TachiePresetCapabilityLevel.Strong,
            "planning fixture starts from a real Strong discovery result");

        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20, Serif = "P7 one" };
        var nextVoice = new VoiceItem(character) { Frame = 180, Length = 40, Layer = 20, Serif = "P7 two" };
        const long serial = 820001;
        voice.Remark = AssociationTag.TargetLine(serial);
        var templateTag = new IntentAssociationTag(
            Guid.Parse("10101010-1010-1010-1010-101010101010"),
            Guid.Parse("20202020-2020-2020-2020-202020202020"),
            Guid.Parse("30303030-3030-3030-3030-303030303030"),
            0, 1, new string('0', 64));
        var oldManaged = new TachieFaceItem(character)
        {
            Frame = 100,
            Length = 30,
            Layer = 6,
            Remark = PluginRemarks.Append(
                PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(serial)),
                templateTag.Line)
        };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.ExpressionBootstrapComplete = true;
        settings.LegacyWorkspace = false;
        scope.Apply(settings, [voice, nextVoice, oldManaged], []);
        await Idle();

        var choices = new TemplateChoice[] { new(null, "— 選択しない —"), TemplateChoice.Preset(candidate) };
        var row = new AssignmentRow(1,
            new VoiceSnapshot(voice, voice.CharacterName, voice.Frame, voice.Length, voice.Serif ?? "", voice.Layer),
            choices, true);
        row.SelectedChoice = choices[1];
        var nextRow = new AssignmentRow(2,
            new VoiceSnapshot(nextVoice, nextVoice.CharacterName, nextVoice.Frame, nextVoice.Length, nextVoice.Serif ?? "", nextVoice.Layer),
            choices, true);
        var rows = new[] { row, nextRow };
        var preset = new ExpressionPreset(
            Guid.NewGuid(),
            "P7 rule",
            ExpressionDuration.NextSameCharacter,
            100,
            2,
            -1,
            new LayerPolicy { UseTemplateLayer = false, Minimum = 4, Maximum = 10, Preferred = 6 });

        var signature = Signature(timeline);
        var settingsJson = JsonSerializer.Serialize(scope.Current);
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        bool DiskSame() => disk == null ? !File.Exists(PlacerSettingsStore.DefaultPath) :
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(disk);

        var mutation = await TachiePresetExpressionMutation.CreateAsync(
            timeline, rows, row, preset, candidate, Resolve, 1);
        Check(mutation.Plan.Count == 1 && mutation.Plan.RemoveCount == 1 && mutation.Plan.UpdateCount == 0 &&
            timeline.Items.Contains(oldManaged) && !timeline.Items.Contains(mutation.Addition),
            "complete replacement is preflighted without mutating Timeline");
        Check(mutation.Addition.Frame == 102 && mutation.Addition.Length == 77 && mutation.Addition.Layer == 6,
            "existing ExpressionPreset span and LayerPlanner semantics shape the fresh preset item");
        Check(mutation.Addition.TachieFaceParameter is P3DirectFace applied && applied.Preset == "Smile" &&
            !ReferenceEquals(mutation.Addition.TachieFaceParameter, defaultFace) &&
            TachiePresetPublicState.Hash(mutation.Addition.TachieFaceParameter) == mutation.StateHash,
            "fresh TachieFaceItem owns the newly applied fresh FaceParameter, never the discovery/default instance");
        Check(TachiePresetAssociationTag.Read(mutation.Addition.Remark, out var plannedTag) == AssociationTagState.Valid &&
            plannedTag!.CapabilityHash == TachiePresetAssociationTag.CapabilityIdentity(candidate.Fingerprint) &&
            plannedTag.CandidateHash == TachiePresetAssociationTag.CandidateIdentity(candidate) &&
            plannedTag.StateHash == mutation.StateHash &&
            AssociationTag.Source(mutation.Addition.Remark, out var source) == AssociationTagState.Valid && source!.Serial == serial,
            "planned item carries exact source serial, capability, candidate and applied-state identity");
        mutation.ValidateCurrent(timeline);
        Check(Signature(timeline) == signature && JsonSerializer.Serialize(scope.Current) == settingsJson && DiskSame(),
            "successful P7 planning and revalidation are Timeline/settings zero-write");

        var experimental = new TachiePresetCandidateDescriptor(
            candidate.Fingerprint, candidate.Route, candidate.CandidateIdentity, candidate.Label,
            TachiePresetCapabilityLevel.Experimental);
        row.RestoreSelectedChoice(TemplateChoice.Preset(experimental));
        var rejectedExperimental = false;
        try { _ = await TachiePresetExpressionMutation.CreateAsync(timeline, rows, row, preset, experimental, Resolve, 1); }
        catch (InvalidOperationException) { rejectedExperimental = true; }
        Check(rejectedExperimental && Signature(timeline) == signature,
            "Experimental candidate remains inspection-only and cannot enter mutation planning");
        row.RestoreSelectedChoice(choices[1]);

        config.Revision++;
        var staleRejected = false;
        try { _ = await TachiePresetExpressionMutation.CreateAsync(timeline, rows, row, preset, candidate, Resolve, 1); }
        catch (InvalidOperationException) { staleRejected = true; }
        finally { config.Revision--; }
        Check(staleRejected && Signature(timeline) == signature,
            "stale capability fingerprint fails before fresh-face application or Timeline mutation");

        var presetVoice = new VoiceItem(character) { Frame = 300, Length = 30, Layer = 20, Serif = "P7 edited" };
        const long presetSerial = 820002;
        presetVoice.Remark = AssociationTag.TargetLine(presetSerial);
        var liveFaceParameter = new P3DirectFace { Preset = "Neutral" };
        var wrongState = "public-v1:" + new string('f', 64);
        var currentTag = TachiePresetAssociationTag.Create(Guid.NewGuid(), 0, 1, candidate, wrongState);
        var currentPreset = new TachieFaceItem(character)
        {
            Frame = 300,
            Length = 30,
            Layer = 6,
            TachieFaceParameter = liveFaceParameter,
            Remark = PluginRemarks.Append(
                PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(presetSerial)),
                currentTag.Line)
        };
        var baseItems = timeline.Items;
        timeline.Items = baseItems.Add(presetVoice).Add(currentPreset);
        timeline.RefreshTimelineLengthAndMaxLayer();
        var presetRow = new AssignmentRow(3,
            new VoiceSnapshot(presetVoice, presetVoice.CharacterName, presetVoice.Frame, presetVoice.Length, presetVoice.Serif ?? "", presetVoice.Layer),
            choices, true);
        presetRow.SelectedChoice = choices[1];
        var stateMismatchRejected = false;
        try
        {
            _ = await TachiePresetExpressionMutation.CreateAsync(
                timeline, [presetRow], presetRow, preset, candidate, Resolve, 1);
        }
        catch (InvalidOperationException) { stateMismatchRejected = true; }
        finally
        {
            timeline.Items = baseItems;
            timeline.RefreshTimelineLengthAndMaxLayer();
        }
        Check(stateMismatchRejected,
            "managed preset whose live FaceParameter no longer matches StateHash fails closed before destructive replacement");

        Check(P3EditorFixture.Handlers.Count == 0 && Signature(timeline) == signature &&
            JsonSerializer.Serialize(scope.Current) == settingsJson && DiskSame(),
            "all P7 negative paths leave host/editor/Timeline/settings state unchanged");

        File.WriteAllText(Path.Combine(output, "tachie-preset-planning.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Planning/1",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_PLANNING_P7=PASS");
    }
}
