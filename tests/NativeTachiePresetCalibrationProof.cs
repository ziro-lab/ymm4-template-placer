#pragma warning disable CS0618
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private sealed class P12HiddenFace : P3ExpandedFace
    {
        [P12ChoiceEditor]
        public object ChoiceDummy { get; } = new();
    }

    private sealed class P12ChoiceEditorAttribute : PropertyEditorForTachieParameterAttribute
    {
        public override FrameworkElement Create() => P3EditorFixture.Create();
        public override void SetBindings(FrameworkElement control, object item, object propertyOwner, PropertyInfo propertyInfo) =>
            P3EditorFixture.Bind(control, propertyOwner);
        public override void ClearBindings(FrameworkElement control) => P3EditorFixture.Clear(control);
    }

    private sealed class P12OtherPluginMarker { }

    private static async Task VerifyTachiePresetCalibration(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P12 item-first discovery and assisted calibration";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var view = View!;
        var checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P12 " + name); checks.Add(name); }

        var opaque = new P3Config
        {
            PresetDefinitions = "//Neutral\n+part/a\n//Smile\n+part/b #2\n"
        };
        var opaqueNames = TachiePresetDiscovery.DirectNames(opaque, CancellationToken.None);
        Check(opaqueNames.SequenceEqual(new[] { "Neutral", "Smile" }),
            "direct fallback treats preset bodies as opaque non-empty content rather than requiring key=value syntax");

        var configA = new P3Config();
        var configB = new P3Config();
        var characterA = new Character
        {
            Name = "P12 A",
            TachieCharacterParameter = configA,
            TachieDefaultFaceParameter = new P12HiddenFace()
        };
        var characterB = new Character
        {
            Name = "P12 B",
            TachieCharacterParameter = configB,
            TachieDefaultFaceParameter = new P12HiddenFace()
        };

        TachiePresetProbeTarget Target(Character c, P3Config config, Type? pluginType = null) =>
            new(c, config, pluginType ?? typeof(P3Config), () => new P12HiddenFace(),
                () => ReferenceEquals(c.TachieCharacterParameter, config));
        TachiePresetProbeTarget Resolve(Character c) =>
            ReferenceEquals(c, characterA) ? Target(c, configA) :
            ReferenceEquals(c, characterB) ? Target(c, configB) :
            throw new InvalidOperationException("Unexpected P12 Character");

        var targetA = Resolve(characterA);
        var sample = targetA.CreateFreshFace(CancellationToken.None);
        var semantic = TachiePresetEditorSession.FindRoutes(sample);
        var broad = TachiePresetEditorSession.FindCalibrationRoutes(sample);
        Check(semantic.Count == 0 && broad.Count == 1 &&
            !broad[0].Property.Name.Contains("Preset", StringComparison.OrdinalIgnoreCase) &&
            !broad[0].AttributeType.Name.Contains("Preset", StringComparison.OrdinalIgnoreCase),
            "normal auto filter ignores a non-Preset-named editor while bounded calibration discovery can inspect it");

        var demonstratedFace = new P12HiddenFace { Mood = "Smile", Amount = 7 };
        var demonstrated = TachiePresetPublicState.Hash(demonstratedFace);
        var matched = await TachiePresetCalibrationEngine.MatchAsync(targetA, demonstrated, CancellationToken.None);
        Check(matched.Route == broad[0].Descriptor && matched.CandidateIdentity == "Smile" &&
            P3EditorFixture.Handlers.Count == 0,
            "one demonstrated expression state uniquely identifies the hidden public editor route and candidate with cleanup");

        var adapterSettings = PlacerSettingsStore.Copy(scope.Original);
        adapterSettings.TachiePresetAdapters.Clear();
        TachiePresetLearnedAdapterSettings.Upsert(adapterSettings, targetA, matched.Route);
        var targetB = Resolve(characterB);
        var mismatch = Target(characterB, configB, typeof(P12OtherPluginMarker));
        Check(TachiePresetLearnedAdapterSettings.Resolve(adapterSettings, targetB) == matched.Route &&
            TachiePresetLearnedAdapterSettings.Resolve(adapterSettings, mismatch) == null,
            "learned adapter is reusable across Characters sharing the plugin surface and invalidates for another plugin identity");

        var oldResolver = vm.PresetTargetResolver;
        var voiceA = new VoiceItem(characterA) { Frame = 100, Length = 30, Layer = 20, Serif = "P12 A" };
        var voiceB = new VoiceItem(characterB) { Frame = 180, Length = 30, Layer = 20, Serif = "P12 B" };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.ExpressionBootstrapComplete = true;
        settings.LegacyWorkspace = false;
        settings.TachiePresetAdapters.Clear();
        scope.Apply(settings, [voiceA, voiceB], []);
        vm.PresetTargetResolver = Resolve;
        vm.InvalidatePresetCapabilityCache();

        try
        {
            vm.IsTachiePresetExpressionSource = true;
            view.ExpressionTab.IsSelected = true;
            await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 2);

            var rowA = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voiceA));
            var rowB = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voiceB));
            Check(!rowA.HasCandidates && !rowB.HasCandidates &&
                vm.TachiePresetCalibrationCommand.CanExecute(rowA) &&
                view.TachiePresetCalibrationButton.IsVisible,
                "unknown non-semantic editor starts with no automatic candidates but exposes one assisted-recognition action");

            vm.TachiePresetCalibrationCommand.Execute(rowA);
            await Idle();
            var calibrationItem = vm.TachiePresetCalibrationItemForProof
                ?? throw new InvalidOperationException("P12 calibration item was not created.");
            Check(timeline.Items.Contains(calibrationItem) &&
                calibrationItem.TachieFaceParameter is P12HiddenFace &&
                !PlacementEngine.IsGenerated(calibrationItem) &&
                (calibrationItem.Remark ?? "").Contains("CWT_TPL:CAL=", StringComparison.Ordinal) &&
                vm.TachiePresetCalibrationActionText == "変更を読み取る",
                "first assisted action places one clearly marked manual expression item without creating a managed expression association");

            var changed = (P12HiddenFace)calibrationItem.TachieFaceParameter!;
            changed.Mood = "Smile";
            changed.Amount = 7;
            vm.TachiePresetCalibrationCommand.Execute(rowA);
            await vm.TachiePresetCalibrationCompletion;
            await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 2);

            rowA = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voiceA));
            rowB = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voiceB));
            var learned = scope.Current.TachiePresetAdapters.Single();
            var persisted = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().TachiePresetAdapters.Single();
            Check(!vm.HasError &&
                learned.PropertyIdentity == matched.Route.PropertyIdentity &&
                learned.EditorIdentity == matched.Route.EditorIdentity &&
                persisted == learned &&
                rowA.Choices.Count(x => x.TachiePreset != null) == 2 &&
                rowB.Choices.Count(x => x.TachiePreset != null) == 2 &&
                rowA.Choices.Any(x => x.TachiePreset?.CandidateIdentity == "Smile") &&
                rowB.Choices.Any(x => x.TachiePreset?.CandidateIdentity == "Smile"),
                "one assisted demonstration persists only the plugin-surface adapter and unlocks current candidate lists for multiple Characters");

            Check(timeline.Items.Contains(calibrationItem) &&
                timeline.Items.Contains(voiceA) && timeline.Items.Contains(voiceB) &&
                ManagedExpressionReader.Read(timeline, voiceA).Bundle == null &&
                ManagedExpressionReader.Read(timeline, voiceB).Bundle == null &&
                P3EditorFixture.Handlers.Count == 0,
                "calibration leaves the marked item user-removable, does not invent managed bundles, and leaves no editor binding alive");
        }
        finally
        {
            view.PaletteTab.IsSelected = true; await Idle();
            vm.IsTemplateExpressionSource = true;
            await vm.ExpressionLoadCompletion;
            vm.InvalidatePresetCapabilityCache();
            vm.PresetTargetResolver = oldResolver;
        }

        File.WriteAllText(Path.Combine(output, "tachie-preset-calibration.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Calibration/1",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_CALIBRATION_P12=PASS");
    }
}
