using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private sealed class P9Config : TachieCharacterParameterBase
    {
        private string definitions = "//Neutral\nmood=neutral\n//Smile\nmood=smile\n";
        public string PresetDefinitions { get => definitions; set => Set(ref definitions, value); }
    }

    private static async Task VerifyTachiePresetFailureUx(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P9 failure and unavailable UX";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var view = View!;
        var checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P9 " + name); checks.Add(name); }

        var oldResolver = vm.PresetTargetResolver;
        var strongConfig = new P9Config();
        var experimentalConfig = new P9Config();
        var noneConfig = new P9Config();
        var otherConfig = new P9Config();
        var currentConfig = new P9Config();

        var strong = new Character { Name = "P9 Strong", TachieCharacterParameter = strongConfig };
        var experimental = new Character { Name = "P9 Experimental", TachieCharacterParameter = experimentalConfig };
        var none = new Character { Name = "P9 None", TachieCharacterParameter = noneConfig };
        var broken = new Character { Name = "P9 Broken" };
        var other = new Character { Name = "P9 Other", TachieCharacterParameter = otherConfig };
        var currentMissing = new Character { Name = "P9 Current Missing", TachieCharacterParameter = currentConfig };

        TachiePresetProbeTarget Target(Character c, object config, Func<object> face) =>
            new(c, config, typeof(P9Config), face, () => ReferenceEquals(c.TachieCharacterParameter, config));
        TachiePresetProbeTarget Resolve(Character c) => c.Name switch
        {
            "P9 Strong" => Target(c, strongConfig, () => new P3DirectFace()),
            "P9 Experimental" => Target(c, experimentalConfig, () => new P3UnstableFace()),
            "P9 None" => Target(c, noneConfig, () => new P3NoiseFace()),
            "P9 Broken" => throw new InvalidOperationException("P9 broken plugin fixture"),
            "P9 Other" => Target(c, otherConfig, () => new P3UnstableFace()),
            "P9 Current Missing" => Target(c, currentConfig, () => new P3DirectFace()),
            _ => throw new InvalidOperationException("Unexpected P9 Character")
        };
        vm.PresetTargetResolver = Resolve;
        vm.InvalidatePresetCapabilityCache();

        var strongVoice = new VoiceItem(strong) { Frame = 100, Length = 20, Layer = 20, Serif = "strong" };
        var experimentalVoice = new VoiceItem(experimental) { Frame = 140, Length = 20, Layer = 20, Serif = "experimental" };
        var noneVoice = new VoiceItem(none) { Frame = 180, Length = 20, Layer = 20, Serif = "none" };
        var brokenVoice = new VoiceItem(broken) { Frame = 220, Length = 20, Layer = 20, Serif = "broken" };
        var otherVoice = new VoiceItem(other) { Frame = 260, Length = 20, Layer = 20, Serif = "other" };
        var missingVoice = new VoiceItem(currentMissing) { Frame = 300, Length = 20, Layer = 20, Serif = "missing" };

        const long otherSerial = 840001;
        otherVoice.Remark = AssociationTag.TargetLine(otherSerial);
        var templateTag = new IntentAssociationTag(
            Guid.Parse("11111111-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("22222222-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("33333333-cccc-cccc-cccc-cccccccccccc"),
            0, 1, new string('0', 64));
        var otherManaged = new TachieFaceItem(other)
        {
            Frame = otherVoice.Frame, Length = 10, Layer = 5,
            Remark = PluginRemarks.Append(
                PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(otherSerial)),
                templateTag.Line)
        };

        var missingTarget = Resolve(currentMissing);
        var ghost = new TachiePresetCandidateDescriptor(
            missingTarget.Fingerprint,
            new TachiePresetRouteDescriptor(TachiePresetRouteKind.DirectNamedProperty,
                typeof(P3DirectFace).FullName + ".Preset"),
            "Ghost", "Ghost", TachiePresetCapabilityLevel.Strong);
        var ghostFaceParameter = new P3DirectFace { Preset = "Ghost" };
        var ghostState = TachiePresetPublicState.Hash(ghostFaceParameter);
        const long missingSerial = 840002;
        missingVoice.Remark = AssociationTag.TargetLine(missingSerial);
        var ghostTag = TachiePresetAssociationTag.Create(Guid.NewGuid(), 0, 1, ghost, ghostState);
        var missingManaged = new TachieFaceItem(currentMissing)
        {
            Frame = missingVoice.Frame, Length = 10, Layer = 6,
            TachieFaceParameter = ghostFaceParameter,
            Remark = PluginRemarks.Append(
                PluginRemarks.Append(PlacementEngine.Marker, AssociationTag.SourceLine(missingSerial)),
                ghostTag.Line)
        };

        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.ExpressionBootstrapComplete = true;
        settings.LegacyWorkspace = false;
        scope.Apply(settings,
            [strongVoice, experimentalVoice, noneVoice, brokenVoice, otherVoice, otherManaged, missingVoice, missingManaged],
            []);
        var baseline = Signature(timeline);
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
            vm.IsTachiePresetExpressionSource = true;
            view.ExpressionTab.IsSelected = true;
            await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 6);

            Check(vm.ExpressionSourceNotice.Contains("実験候補", StringComparison.Ordinal) &&
                vm.ExpressionSourceNotice.Contains("Preview", StringComparison.Ordinal) &&
                view.TachiePresetSourceNotice.IsVisible && view.VoiceGrid.IsVisible,
                "live preset UI explains that plausible Experimental candidates may be tried and judged in Preview");

            var strongRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, strongVoice));
            var experimentalRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, experimentalVoice));
            var noneRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, noneVoice));
            var brokenRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, brokenVoice));
            var otherRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, otherVoice));
            var missingRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, missingVoice));

            Check(strongRow.HasCandidates && strongRow.Choices.Count(x => x.TachiePreset != null) == 2,
                "healthy Character retains normal Strong candidates beside local failures");
            Check(experimentalRow.Choices.Where(x => x.TachiePreset != null).All(x =>
                    x.TachiePreset!.Confidence == TachiePresetCapabilityLevel.Experimental &&
                    x.Label.Contains("実験", StringComparison.Ordinal)),
                "Experimental confidence is visible on every Experimental candidate");

            var beforeExperimental = Signature(timeline);
            experimentalRow.SelectedChoice = experimentalRow.Choices.Single(x => x.TachiePreset?.CandidateIdentity == "Smile");
            await vm.TachiePresetApplyCompletion; await Idle();
            Check(vm.HasError && vm.Status.Contains("変更できませんでした", StringComparison.Ordinal) &&
                Signature(timeline) == beforeExperimental,
                "unstable Experimental candidate is tried through the safe fresh-item path and fails before Timeline mutation");

            Check(!noneRow.HasCandidates && noneRow.State == "候補なし" &&
                noneRow.SourceNotice.Contains("このキャラクターでは立ち絵プリセットを利用できません", StringComparison.Ordinal),
                "no-capability Character has a clear local no-candidate state");
            Check(!brokenRow.HasCandidates && brokenRow.State == "候補なし" &&
                brokenRow.SourceNotice.Contains("この行だけ利用できません", StringComparison.Ordinal) &&
                brokenRow.SourceNotice.Contains("P9 broken plugin fixture", StringComparison.Ordinal) &&
                strongRow.HasCandidates,
                "broken plugin failure is visibly contained to its Character without disabling healthy rows");

            Check(otherRow.SelectedChoice.IsCurrentOtherSource &&
                otherRow.SelectedChoice.Label.Contains("テンプレート由来", StringComparison.Ordinal) &&
                otherRow.SourceNotice.Contains("テンプレートから配置", StringComparison.Ordinal),
                "valid current other-source association has an explicit visible label and explanation");

            Check(!missingRow.SelectedChoice.IsAvailable &&
                missingRow.SelectedChoice.Label.Contains("候補が消えたか立ち絵設定が変わりました", StringComparison.Ordinal) &&
                missingRow.SourceNotice.Contains("プラグイン構成が変わった可能性", StringComparison.Ordinal),
                "managed same-source candidate disappearance is explicit and never healed by label");

            Check(!experimentalRow.SelectedChoice.HasCandidate && experimentalRow.SelectedChoice.IsAvailable,
                "failed Experimental trial restores the unmanaged row to live Timeline truth instead of retaining a failed candidate selection");
            var previousExperimentalFingerprints = experimentalRow.Choices.Where(x => x.TachiePreset != null)
                .Select(x => x.TachiePreset!.Fingerprint).ToHashSet();
            experimentalConfig.PresetDefinitions = "//Neutral\nmood=changed\n//Smile\nmood=changed-smile\n";
            for (var i = 0; i < 120; i++)
            {
                await Idle();
                await vm.ExpressionLoadCompletion;
                experimentalRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, experimentalVoice));
                if (experimentalRow.Choices.Any(x => x.TachiePreset != null &&
                    !previousExperimentalFingerprints.Contains(x.TachiePreset.Fingerprint))) break;
                await Task.Delay(10);
            }
            Check(!experimentalRow.SelectedChoice.HasCandidate && experimentalRow.SelectedChoice.IsAvailable &&
                experimentalRow.Choices.Any(x => x.IsAvailable && x.TachiePreset?.CandidateIdentity == "Smile" &&
                    !previousExperimentalFingerprints.Contains(x.TachiePreset.Fingerprint)),
                "configuration change exposes a fresh same-label Experimental candidate without reviving the failed prior selection");

            otherRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, otherVoice));
            otherRow.SelectedChoice = otherRow.Choices.Single(x => x.TachiePreset?.CandidateIdentity == "Smile");
            await vm.TachiePresetApplyCompletion; await Idle();
            Check(vm.HasError &&
                ManagedExpressionReader.Read(timeline, otherVoice).Bundle?.Descriptor.Kind == ManagedExpressionSourceKind.Template,
                "failed Experimental trial over other-source leaves the existing managed Template truth unchanged");
            otherConfig.PresetDefinitions = "//Neutral\nmood=other\n//Smile\nmood=other-smile\n";
            for (var i = 0; i < 120; i++)
            {
                await Idle();
                await vm.ExpressionLoadCompletion;
                otherRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, otherVoice));
                if (otherRow.SelectedChoice.IsCurrentOtherSource) break;
                await Task.Delay(10);
            }
            Check(otherRow.SelectedChoice.IsCurrentOtherSource &&
                otherRow.SelectedChoice.Label.Contains("テンプレート由来", StringComparison.Ordinal),
                "reload restores live managed other-source truth instead of preserving an uncommitted inspection choice");

            SaveNamedView(view, "tachie-preset-p9-failure-ux.png");
            var storedAfterInspection = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            Check(Signature(timeline) == baseline && JsonSerializer.Serialize(scope.Current) == settingsJson &&
                storedAfterInspection.ExpressionSourceMode == ExpressionSourceMode.TachiePreset &&
                WithoutSourceMode(storedAfterInspection) == WithoutSourceMode(storedBeforeSourceSwitch),
                "all P9 failure/unavailable UX paths preserve Timeline and product settings beyond the selected source preference");
        }
        finally
        {
            view.PaletteTab.IsSelected = true; await Idle();
            vm.CancelTachiePresetApplyForProof();
            vm.IsTemplateExpressionSource = true;
            await vm.ExpressionLoadCompletion;
            vm.InvalidatePresetCapabilityCache();
            vm.PresetTargetResolver = oldResolver;
        }

        File.WriteAllText(Path.Combine(output, "tachie-preset-failure-ux.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Failure-UX/1",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_FAILURE_UX_P9=PASS");
    }
}
