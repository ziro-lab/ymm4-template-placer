using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyPresetSelectorRefresh(Timeline timeline)
    {
        stage = "W12 preset selector refresh";
        var vm = ViewModel!; var view = View!;
        var signature = Signature(timeline);
        timeline.SelectedItems = []; view.MainTabs.SelectedIndex = 0; await Idle();
        var expression = vm.SelectedExpressionPreset!;
        Assert(view.PresetSurface.PresetSelector.SelectedItem is ExpressionPreset visible && visible.Id == expression.Id && visible.Name == expression.Name,
            "W12 actual expression selector displays the current saved preset after unrelated settings refresh");
        vm.CopyExpressionPreset(); var expressionCopy = vm.SelectedExpressionPreset!; await Idle();
        Assert(view.PresetSurface.PresetSelector.SelectedItem is ExpressionPreset copied && copied.Id == expressionCopy.Id,
            "W12 actual expression selector follows a newly copied preset");
        vm.Refresh(); await Idle();
        Assert(view.PresetSurface.PresetSelector.SelectedItem is ExpressionPreset refreshed && refreshed.Id == expressionCopy.Id,
            "W12 expression selector survives a no-change collection refresh");
        view.PresetSurface.PresetSelector.SelectedItem = vm.ExpressionPresets.Single(x => x.Id == expression.Id); await Idle();
        Assert(vm.SelectedExpressionPreset!.Id == expression.Id && view.PresetSurface.PresetSelector.SelectedItem is ExpressionPreset selected && selected.Id == expression.Id,
            "W12 actual expression ComboBox selection updates and retains the saved current ID");
        view.PresetSurface.PresetSelector.SelectedItem = vm.ExpressionPresets.Single(x => x.Id == expressionCopy.Id); await Idle();
        vm.DeleteExpressionPreset(); await Idle();
        Assert(view.PresetSurface.PresetSelector.SelectedItem is ExpressionPreset afterDelete && afterDelete.Id == vm.SelectedExpressionPreset!.Id,
            "W12 deleting an expression copy leaves a visible valid selection");
        vm.SelectedExpressionPreset = vm.ExpressionPresets.Single(x => x.Id == expression.Id);
        timeline.SelectedItems = [timeline.Items.OfType<VoiceItem>().First()];
        view.MainTabs.SelectedIndex = 1; await Idle();
        var selection = vm.SelectedSelectionPreset!;
        Assert(view.SelectionSurface.SelectionPresetSelector.SelectedItem is SelectionPreset shown && shown.Id == selection.Id && shown.Name == selection.Name,
            "W12 actual selection-preset ComboBox displays its saved name and ID");
        vm.CopySelectionPreset(); var copy = vm.SelectedSelectionPreset!; await Idle();
        Assert(view.SelectionSurface.SelectionPresetSelector.SelectedItem is SelectionPreset selectionCopy && selectionCopy.Id == copy.Id,
            "W12 selection-preset ComboBox follows a new saved copy");
        vm.Refresh(); await Idle();
        Assert(view.SelectionSurface.SelectionPresetSelector.SelectedItem is SelectionPreset afterRefresh && afterRefresh.Id == copy.Id,
            "W12 selection-preset ComboBox remains selected through an unchanged refresh");
        view.SelectionSurface.SelectionPresetSelector.SelectedItem = vm.SelectionPresets.Single(x => x.Id == selection.Id); await Idle();
        Assert(vm.SelectedSelectionPreset!.Id == selection.Id && view.SelectionSurface.SelectionPresetSelector.SelectedItem is SelectionPreset changed && changed.Id == selection.Id,
            "W12 actual selection-preset ComboBox change is correctly two-way");
        view.SelectionSurface.SelectionPresetSelector.SelectedItem = vm.SelectionPresets.Single(x => x.Id == copy.Id); await Idle();
        vm.DeleteSelectionPreset(); await Idle();
        Assert(view.SelectionSurface.SelectionPresetSelector.SelectedItem is SelectionPreset remaining && remaining.Id == vm.SelectedSelectionPreset!.Id,
            "W12 deleting a selection copy leaves a visible valid current preset");
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == selection.Id);
        timeline.SelectedItems = []; view.MainTabs.SelectedIndex = 0; await Idle();
        Assert(Signature(timeline) == signature, "W12 selector changes and refresh never mutate native Timeline Items");
        Log("W12_SELECTORS=PASS");
    }
}
