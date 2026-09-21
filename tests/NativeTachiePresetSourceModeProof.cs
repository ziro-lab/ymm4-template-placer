using System.IO;
using System.Reflection;
using System.Text.Json;
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
        var vm = ViewModel!;
        var view = View!;
        var character = new Character { Name = "P1 Source Mode" };
        var source = scope.AddTemplate("P1/source-mode", new TachieFaceItem(character) { Length = 12, Layer = 4 });
        var target = new IntentTargetContext
        {
            ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
            CharacterName = character.Name
        };
        var set = new IntentPalette(Guid.NewGuid(), "P1表情", "表情", target, new IntentRelation(),
            [new IntentEntry(source.Id) { UseTemplateDuration = true }])
        { ExpressionCandidates = true };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [source];
        settings.IntentPalettes = [set];
        settings.ExpressionBootstrapComplete = true;
        settings.IntentPaletteRevision = Math.Max(1, settings.IntentPaletteRevision + 1);
        settings.LegacyWorkspace = false;

        var voice = new VoiceItem(character) { Frame = 120, Length = 40, Layer = 20, Serif = "source mode" };
        scope.Apply(settings, [voice], [voice]);
        await Idle();
        view.ExpressionTab.IsSelected = true;
        await Idle();
        await WaitExpressionRows(vm, 1);

        Assert(vm.IsTemplateExpressionSource && !vm.IsTachiePresetExpressionSource &&
            view.TemplateExpressionSource.IsChecked == true && view.TachiePresetExpressionSource.IsChecked != true,
            "TP-P1 starts every live Tool session in Template source mode");

        static byte[]? DiskSettings() => File.Exists(PlacerSettingsStore.DefaultPath)
            ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        static bool SameBytes(byte[]? left, byte[]? right) =>
            left == null ? right == null : right != null && left.SequenceEqual(right);

        var timelineBefore = Signature(timeline);
        var settingsBefore = JsonSerializer.Serialize(scope.Current);
        var diskBefore = DiskSettings();
        var stableRows = vm.Rows.ToArray();

        using var entered = new ManualResetEventSlim(false);
        using var gate = new ManualResetEventSlim(false);
        ExpressionPreparation.ProofPrepareEntered = entered;
        ExpressionPreparation.ProofPrepareGate = gate;
        try
        {
            view.PaletteTab.IsSelected = true;
            await Idle();
            view.ExpressionTab.IsSelected = true;
            await Idle();
            for (var i = 0; i < 200 && !entered.IsSet; i++) { await Task.Delay(10); await Idle(); }
            Assert(entered.IsSet && vm.IsExpressionLoading,
                "TP-P1 source switch test starts a real in-flight Template preparation");

            vm.IsTachiePresetExpressionSource = true;
            await Idle();
            Assert(vm.IsTachiePresetExpressionSource && !vm.IsExpressionLoading &&
                !vm.CanEditExpressionRows && !vm.RefreshCommand.CanExecute(null) &&
                !vm.PlaceCommand.CanExecute(null) && !vm.ExportCommand.CanExecute(null) &&
                !vm.ImportCommand.CanExecute(null) && !vm.ResyncCommand.CanExecute(null),
                "TP-P1 entering Tachie Preset cancels Template work and gates Template-only commands");

            Assert(!view.VoiceGrid.IsVisible && !view.ExcelEditor.IsVisible &&
                view.TachiePresetRowsPlaceholder.IsVisible && view.TachiePresetPlacementRuleLabel.IsVisible &&
                view.TachiePresetSourceNotice.IsVisible,
                "TP-P1 Tachie Preset source never presents cached Template choices or Excel as preset content");

            gate.Set();
            await Task.Delay(50);
            await Idle();
            Assert(vm.Rows.SequenceEqual(stableRows) && Signature(timeline) == timelineBefore &&
                JsonSerializer.Serialize(scope.Current) == settingsBefore && SameBytes(diskBefore, DiskSettings()),
                "TP-P1 canceled/stale Template preparation cannot publish or write Timeline/settings after source switch");
        }
        finally
        {
            gate.Set();
            ExpressionPreparation.ProofPrepareGate = null;
            ExpressionPreparation.ProofPrepareEntered = null;
        }

        vm.IsTemplateExpressionSource = true;
        await Idle();
        await WaitExpressionRows(vm, 1);
        Assert(vm.IsTemplateExpressionSource && view.VoiceGrid.IsVisible && view.ExcelEditor.IsVisible &&
            Signature(timeline) == timelineBefore && JsonSerializer.Serialize(scope.Current) == settingsBefore &&
            SameBytes(diskBefore, DiskSettings()),
            "TP-P1 returning Template restores the existing row path with zero Timeline/settings writes");

        PlacerViewModel? fresh = null;
        try
        {
            fresh = new PlacerViewModel();
            Assert(fresh.IsTemplateExpressionSource && !fresh.IsTachiePresetExpressionSource,
                "TP-P1 source mode is session-local and a fresh ViewModel starts Template");
        }
        finally
        {
            fresh?.Dispose();
            ViewModel = vm;
        }

        var book = Path.Combine(output, "tachie-preset-p1-pending.xlsx");
        vm.ExportTo(book);
        var assigned = vm.Rows.Single().Choices.Single(x => x.Template != null).Template!.Name;
        EditCell(book, "F2", assigned);
        vm.ImportFrom(book);
        Assert(vm.PlaceCommand.CanExecute(null),
            "TP-P1 imported Template assignment is protected pending work before placement");

        timelineBefore = Signature(timeline);
        settingsBefore = JsonSerializer.Serialize(scope.Current);
        diskBefore = DiskSettings();
        vm.IsTachiePresetExpressionSource = true;
        await Idle();

        Assert(vm.IsTemplateExpressionSource && !vm.IsTachiePresetExpressionSource &&
            vm.Status.Contains("未配置", StringComparison.Ordinal) &&
            Signature(timeline) == timelineBefore && JsonSerializer.Serialize(scope.Current) == settingsBefore &&
            SameBytes(diskBefore, DiskSettings()),
            "TP-P1 protected pending Template/Excel work blocks Tachie Preset entry without discarding or writing anything");

        Log("TACHIE_PRESET_SOURCE_MODE_P1=PASS");
    }
}
