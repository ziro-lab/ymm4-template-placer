using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyTachiePresetImmediateReplacement(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P8 immediate trial and cross-source replacement";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var view = View!;
        var checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P8 " + name); checks.Add(name); }

        var oldResolver = vm.PresetTargetResolver;
        var config = new P3Config();
        var defaultFace = new P3DirectFace();
        var character = new Character
        {
            Name = "P8 cross source",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = defaultFace
        };
        TachiePresetProbeTarget Resolve(Character c) =>
            new(c, config, typeof(P3Config), () => new P3DirectFace(),
                () => ReferenceEquals(c.TachieCharacterParameter, config));
        vm.PresetTargetResolver = Resolve;
        vm.InvalidatePresetCapabilityCache();

        var templateSource = scope.AddTemplate("P8/template",
            new TachieFaceItem(character) { Length = 12, Layer = 5, Remark = "p8-template" });
        var palette = new IntentPalette(Guid.NewGuid(), "P8 Template", "表情",
            new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name },
            new(), [new(templateSource.Id) { UseTemplateDuration = true }]) { ExpressionCandidates = true };
        var rule = new ExpressionPreset(
            Guid.NewGuid(), "P8 rule", ExpressionDuration.VoiceSpan, 90, 0, 0,
            new LayerPolicy { UseTemplateLayer = false, Minimum = 4, Maximum = 10, Preferred = 6 });
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [templateSource];
        settings.IntentPalettes = [palette];
        settings.ExpressionPresets = [rule];
        settings.CurrentExpressionPresetId = rule.Id;
        settings.ExpressionBootstrapComplete = true;
        settings.IntentPaletteRevision = 1;
        settings.LegacyWorkspace = false;

        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20, Serif = "P8 one" };
        var secondVoice = new VoiceItem(character) { Frame = 240, Length = 40, Layer = 20, Serif = "P8 two" };
        var manual = new TextItem { Frame = 500, Length = 24, Layer = 70, Remark = "p8-manual" };
        scope.Apply(settings, [voice, secondVoice, manual], []);
        var settingsJson = JsonSerializer.Serialize(scope.Current);
        var storedBeforeSourceSwitch = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        static string WithoutSourceMode(PlacerSettings value)
        {
            var copy = PlacerSettingsStore.Copy(value);
            copy.ExpressionSourceMode = ExpressionSourceMode.Template;
            return JsonSerializer.Serialize(copy);
        }

        try
        {
            view.ExpressionTab.IsSelected = true;
            await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 2);
            var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            var row2 = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, secondVoice));

            row.SelectedChoice = row.Choices.Single(x =>
                x.Template?.IntentSource?.Entry.LibraryEntryId == templateSource.Id);
            await Idle();
            var templateBaseline = Signature(timeline);
            Check(ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.Template && timeline.Items.Contains(manual),
                "Template baseline is exact before Preset trial");

            vm.IsTachiePresetExpressionSource = true;
            await vm.ExpressionLoadCompletion; await Idle();
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            row2 = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, secondVoice));
            Check(row.SelectedChoice.IsCurrentOtherSource,
                "source switch reports live Template association without mutation");

            row.SelectedChoice = row.Choices.Single(x => x.TachiePreset?.CandidateIdentity == "Smile");
            await vm.TachiePresetApplyCompletion; await Idle();
            var smile = ManagedExpressionReader.Read(timeline, voice);
            var smileTag = smile.Bundle?.Descriptor.TachiePreset;
            Check(!vm.HasError && smile.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.TachiePreset &&
                smileTag != null &&
                smileTag.CandidateHash == TachiePresetAssociationTag.CandidateIdentity(row.SelectedChoice.TachiePreset!) &&
                timeline.Items.Contains(manual),
                "Template -> TachiePreset replacement is exact and preserves unrelated content");
            Check(!vm.IsExpressionLoading && vm.ExpressionRowsMatchSource &&
                row.SelectedChoice.TachiePreset?.CandidateIdentity == "Smile",
                "owned preset replacement keeps the authoritative row ready for the next immediate choice");

            row.SelectedChoice = row.Choices.Single(x => x.TachiePreset?.CandidateIdentity == "Neutral");
            await vm.TachiePresetApplyCompletion; await Idle();
            var neutral = ManagedExpressionReader.Read(timeline, voice);
            var neutralTag = neutral.Bundle?.Descriptor.TachiePreset;
            Check(!vm.HasError && neutral.Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.TachiePreset &&
                neutralTag != null &&
                neutralTag.CandidateHash == TachiePresetAssociationTag.CandidateIdentity(row.SelectedChoice.TachiePreset!) &&
                neutralTag.CandidateHash != smileTag!.CandidateHash,
                "TachiePreset -> TachiePreset performs exact whole-bundle replacement");

            row.SelectedChoice = row.Choices.First(x =>
                !x.HasCandidate && x.IsAvailable && !x.IsCurrentOtherSource && !x.IsInvalidAssociation);
            await vm.TachiePresetApplyCompletion; await Idle();
            var none = ManagedExpressionReader.Read(timeline, voice);
            var finalNone = Signature(timeline);
            Check(!vm.HasError && none.Serial.HasValue && none.Bundle == null &&
                timeline.Items.Contains(manual) && manual.Remark == "p8-manual",
                "TachiePreset -> none removes only the managed bundle");

            vm.SetExpressionRowContext(row2);
            await undo.UndoAsync(); await Idle();
            Check(Signature(timeline) == templateBaseline &&
                ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.Template,
                "one Undo restores initial Template before Preset -> Preset -> none trial");
            await undo.RedoAsync(); await Idle();
            Check(Signature(timeline) == finalNone &&
                ManagedExpressionReader.Read(timeline, voice).Bundle == null,
                "one Redo restores final none state");
            await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 2);
            Check(!vm.IsExpressionLoading && vm.ExpressionRowsMatchSource,
                "post-Redo preset rows are live before accepting the next user choice");

            vm.SetExpressionRowContext(row);
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            row.SelectedChoice = row.Choices.Single(x => x.TachiePreset?.CandidateIdentity == "Smile");
            await vm.TachiePresetApplyCompletion; await Idle();
            var presetBeforeTemplate = Signature(timeline);
            Check(ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.TachiePreset,
                "Preset state is live before returning to Template source");

            vm.IsTemplateExpressionSource = true;
            await vm.ExpressionLoadCompletion; await Idle();
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            row2 = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, secondVoice));
            Check(row.SelectedChoice.IsCurrentOtherSource,
                "Template source reports current Preset as other-source");

            row.SelectedChoice = row.Choices.Single(x =>
                x.Template?.IntentSource?.Entry.LibraryEntryId == templateSource.Id);
            await Idle();
            var templateAfterPreset = Signature(timeline);
            Check(!vm.HasError &&
                ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.Template,
                "TachiePreset -> Template uses the same exact managed seam");

            vm.SetExpressionRowContext(row2);
            await undo.UndoAsync(); await Idle();
            Check(Signature(timeline) == presetBeforeTemplate &&
                ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.TachiePreset,
                "one Undo restores Preset after Preset -> Template");
            await undo.RedoAsync(); await Idle();
            Check(Signature(timeline) == templateAfterPreset &&
                ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.Template,
                "Redo restores exact Template replacement");

            vm.IsTachiePresetExpressionSource = true;
            await vm.ExpressionLoadCompletion; await Idle();
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            row.SelectedChoice = row.Choices.Single(x => x.TachiePreset?.CandidateIdentity == "Smile");
            var beforePresetTrial = templateAfterPreset;
            await vm.TachiePresetApplyCompletion; await Idle();
            var presetTrialState = Signature(timeline);
            var manualLength = manual.Length;
            manual.Length++;
            undo.Record();
            await undo.UndoAsync(); await Idle();
            Check(Signature(timeline) == presetTrialState && manual.Length == manualLength,
                "first Undo after unrelated edit affects only external edit");
            await undo.UndoAsync(); await Idle();
            Check(Signature(timeline) == beforePresetTrial &&
                ManagedExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Kind ==
                    ManagedExpressionSourceKind.Template,
                "second Undo restores pre-Preset state; unrelated edit was not captured");

            var storedAfterTrials = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            Check(JsonSerializer.Serialize(scope.Current) == settingsJson &&
                storedAfterTrials.ExpressionSourceMode == ExpressionSourceMode.TachiePreset &&
                WithoutSourceMode(storedAfterTrials) == WithoutSourceMode(storedBeforeSourceSwitch),
                "immediate trials persist no candidate identity or product settings beyond the selected source preference");
        }
        finally
        {
            view.PaletteTab.IsSelected = true; await Idle();
            vm.CloseExpressionTrialSession();
            vm.IsTemplateExpressionSource = true;
            await vm.ExpressionLoadCompletion;
            vm.InvalidatePresetCapabilityCache();
            vm.PresetTargetResolver = oldResolver;
        }

        File.WriteAllText(Path.Combine(output, "tachie-preset-immediate.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Immediate/1",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_IMMEDIATE_P8=PASS");
    }
}
