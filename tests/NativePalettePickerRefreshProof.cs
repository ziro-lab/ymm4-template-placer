using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyPalettePickerRefresh(Timeline timeline)
    {
        stage = "WUX3 Palette picker refresh review";
        var vm = ViewModel!; var view = View!; var picker = view.PaletteSurface.PaletteSelector;
        timeline.SelectedItems = []; ShowTask(view, "palette"); await Idle();
        var original = Signature(timeline); var palette = vm.CurrentPalette!;
        var oldPreset = vm.SelectedExpressionPreset!.Id;
        var changes = 0;
        NotifyCollectionChangedEventHandler changed = (_, _) => changes++;
        vm.PaletteChoices.CollectionChanged += changed;
        try
        {
            vm.CopyExpressionPreset(); vm.ExpressionDraft.Name = "selector refresh probe"; vm.SaveExpressionPreset();
            vm.Refresh(); await Idle();
            Assert(changes == 0 && picker.SelectedItem is PaletteChoice shown && shown.Id == palette.Id && shown.Name == palette.Name,
                "WUX3 unrelated preset save and refresh retain the actual visible Palette name without resetting the picker");
            Assert(picker.GetBindingExpression(ComboBox.SelectedItemProperty)?.Status == BindingStatus.Active &&
                ReferenceEquals(picker.ItemsSource, vm.PaletteChoices), "WUX3 Palette binding remains active after a storage deep copy");
            vm.DeleteExpressionPreset(); vm.SelectedExpressionPreset = vm.ExpressionPresets.Single(x => x.Id == oldPreset); await Idle();
            Assert(changes == 0 && picker.SelectedItem is PaletteChoice restored && restored.Id == palette.Id,
                "WUX3 deletion of another task's preset neither clears nor switches the Palette picker");
        }
        finally { vm.PaletteChoices.CollectionChanged -= changed; }
        vm.PaletteNameDraft = palette.Name + "・名前確認"; vm.RenamePalette(); await Idle();
        Assert(picker.SelectedItem is PaletteChoice renamed && renamed.Id == palette.Id && renamed.Name == palette.Name + "・名前確認",
            "WUX3 an actual Palette rename updates the visible name while keeping the selected ID");
        vm.PaletteNameDraft = palette.Name; vm.RenamePalette(); await Idle();
        ShowTask(view, "selection"); await Idle(); ShowTask(view, "palette"); await Idle();
        Assert(picker.SelectedItem is PaletteChoice returned && returned.Id == palette.Id && returned.Name == palette.Name,
            "WUX3 returning from another task displays the selected Palette instead of an empty picker");
        SaveNamedView(view, "ux-palette-refresh-normal.png");
        var width = view.Width; var height = view.Height;
        try { view.Width = 360; view.Height = 320; await Idle(); SaveNamedView(view, "ux-palette-refresh-narrow.png"); }
        finally { view.Width = width; view.Height = height; await Idle(); }
        Assert(Signature(timeline) == original, "WUX3 picker refresh and rename checks leave all Timeline items unchanged");
        ShowTask(view, "expression"); await Idle(); Log("WUX3_PICKER=PASS");
    }
}
