using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyTachiePresetSourceModeFoundation(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P1 source-mode foundation";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!;
        var character = new Character { Name = "P1 Source Mode" };
        var source = scope.AddTemplate("P1/source-mode", new TachieFaceItem(character) { Length = 12, Layer = 4 });
        var set = new IntentPalette(Guid.NewGuid(), "P1表情", "表情",
            new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name },
            new(), [new(source.Id) { UseTemplateDuration = true }]) { ExpressionCandidates = true };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [source]; settings.IntentPalettes = [set];
        settings.ExpressionBootstrapComplete = true; settings.IntentPaletteRevision = 1; settings.LegacyWorkspace = false;
        var voice = new VoiceItem(character) { Frame = 120, Length = 40, Layer = 20, Serif = "source mode" };
        scope.Apply(settings, [voice], [voice]); await Idle();
        view.ExpressionTab.IsSelected = true; await Idle(); await WaitExpressionRows(vm, 1);
        Assert(vm.IsTemplateExpressionSource && !vm.IsTachiePresetExpressionSource &&
            view.TemplateExpressionSource.IsChecked == true && view.TachiePresetExpressionSource.IsChecked != true,
            "TP-P1 default persisted source mode is Template");
        static byte[]? Disk() => File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        static bool Same(byte[]? a, byte[]? b) => a == null ? b == null : b != null && a.SequenceEqual(b);
        static PlacerSettings StoredSettings() => new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        static string WithoutSourceMode(PlacerSettings value)
        {
            var copy = PlacerSettingsStore.Copy(value);
            copy.ExpressionSourceMode = ExpressionSourceMode.Template;
            return JsonSerializer.Serialize(copy);
        }
        var signature = Signature(timeline); var baselineSettings = StoredSettings(); var bytes = Disk();
        var stableRows = vm.Rows.ToArray();
        using var entered = new ManualResetEventSlim(false); using var gate = new ManualResetEventSlim(false);
        ExpressionPreparation.ProofPrepareEntered = entered; ExpressionPreparation.ProofPrepareGate = gate;
        try
        {
            view.PaletteTab.IsSelected = true; await Idle(); view.ExpressionTab.IsSelected = true; await Idle();
            for (var i = 0; i < 200 && !entered.IsSet; i++) { await Task.Delay(10); await Idle(); }
            Assert(entered.IsSet && vm.IsExpressionLoading, "TP-P1 source switch starts with real Template work in flight");
            vm.IsTachiePresetExpressionSource = true; await Idle();
            Assert(vm.IsTachiePresetExpressionSource && vm.IsExpressionLoading && !vm.CanEditExpressionRows &&
                !vm.PlaceCommand.CanExecute(null) && !vm.ExportCommand.CanExecute(null) &&
                !vm.ImportCommand.CanExecute(null) && !vm.ResyncCommand.CanExecute(null),
                "TP-P1/P4 switching cancels Template work and admits only the new source load");
            Assert(!view.VoiceGrid.IsVisible && !view.ExcelEditor.IsVisible && view.TachiePresetRowsPlaceholder.IsVisible &&
                view.TachiePresetPlacementRuleLabel.IsVisible && view.TachiePresetSourceNotice.IsVisible,
                "TP-P1/P4 stale Template choices are hidden until the current-source projection is ready");
            gate.Set(); await vm.ExpressionLoadCompletion; await Idle();
            var presetStored = StoredSettings();
            Assert(vm.Rows.SequenceEqual(stableRows) && vm.Rows.All(r => r.Choices.All(c => c.Template == null)),
                "TP-P1/P4 stale Template work cannot publish Template choices after switching");
            Assert(Signature(timeline) == signature,
                "TP-P1 source switch is Timeline zero-write");
            Assert(presetStored.ExpressionSourceMode == ExpressionSourceMode.TachiePreset,
                "TP-P1 TachiePreset source preference is persisted");
            var beforeNode = JsonSerializer.SerializeToNode(baselineSettings)!.AsObject();
            var afterNode = JsonSerializer.SerializeToNode(presetStored)!.AsObject();
            beforeNode.Remove(nameof(PlacerSettings.ExpressionSourceMode));
            afterNode.Remove(nameof(PlacerSettings.ExpressionSourceMode));
            var changedTopLevel = beforeNode.Select(x => x.Key).Union(afterNode.Select(x => x.Key), StringComparer.Ordinal)
                .Where(key =>
                {
                    beforeNode.TryGetPropertyValue(key, out var beforeValue);
                    afterNode.TryGetPropertyValue(key, out var afterValue);
                    return !JsonNode.DeepEquals(beforeValue, afterValue);
                })
                .Order(StringComparer.Ordinal).ToArray();
            Log("TP-P1 persisted-other-diff=" + (changedTopLevel.Length == 0 ? "<none>" : string.Join(",", changedTopLevel)));
            Assert(changedTopLevel.Length == 0,
                "TP-P1 source preference persistence preserves all other stored settings");

            PlacerViewModel? persistedPreset = null;
            try
            {
                persistedPreset = new();
                Assert(persistedPreset.IsTachiePresetExpressionSource && !persistedPreset.IsTemplateExpressionSource,
                    "TP-P1 a new Tool instance restores persisted TachiePreset source mode");
            }
            finally { persistedPreset?.Dispose(); ViewModel = vm; }
        }
        finally { gate.Set(); ExpressionPreparation.ProofPrepareGate = null; ExpressionPreparation.ProofPrepareEntered = null; }
        vm.IsTemplateExpressionSource = true; await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 1);
        var templateStored = StoredSettings();
        Assert(view.VoiceGrid.IsVisible && view.ExcelEditor.IsVisible && Signature(timeline) == signature &&
            templateStored.ExpressionSourceMode == ExpressionSourceMode.Template &&
            WithoutSourceMode(templateStored) == WithoutSourceMode(baselineSettings),
            "TP-P1 returning Template restores the existing path and persists only the selected mode");
        PlacerViewModel? fresh = null;
        try
        {
            fresh = new();
            Assert(fresh.IsTemplateExpressionSource && !fresh.IsTachiePresetExpressionSource,
                "TP-P1 a new Tool instance restores persisted Template source mode");
        }
        finally { fresh?.Dispose(); ViewModel = vm; }
        var book = Path.Combine(output, "tachie-preset-p1-pending.xlsx");
        vm.ExportTo(book); EditCell(book, "F2", vm.Rows.Single().Choices.Single(x => x.Template != null).Template!.Name); vm.ImportFrom(book);
        Assert(vm.PlaceCommand.CanExecute(null), "TP-P1 real imported Template assignment is protected pending work");
        signature = Signature(timeline); bytes = Disk();
        vm.IsTachiePresetExpressionSource = true; await Idle();
        Assert(vm.IsTemplateExpressionSource && !vm.IsTachiePresetExpressionSource && vm.Status.Contains("未配置", StringComparison.Ordinal) &&
            Signature(timeline) == signature && Same(bytes, Disk()),
            "TP-P1 pending Template/Excel assignment blocks source entry without discard or preference write");
        Log("TACHIE_PRESET_SOURCE_MODE_P1=PASS");
    }
}
