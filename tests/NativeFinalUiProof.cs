using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void SaveNamedView(PlacerView view, string filename)
    {
        SaveView(view);
        File.Copy(Path.Combine(output, "native-plugin-ui.png"), Path.Combine(output, filename), true);
    }
    private static async Task VerifyFinalUi(Timeline timeline)
    {
        stage = "W12 final UI";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = []; vm.Refresh();
        var before = Signature(timeline);
        var nextId = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId;
        var oldKind = vm.ActivePaletteKind; var oldStyle = vm.ManualStylePalette?.Id;
        var voice = timeline.Items.OfType<VoiceItem>().First();
        var source = new TextItem { Frame = 0, Length = 20, Layer = 100, Remark = "final neutral fixture" };
        var template = Template("W12/Overview", [source]); ItemSettings.Default.Templates.Add(template);
        vm.Refresh(); vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "強調";
        var entry = vm.RegisterLibrary();
        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = "仕上げ確認"; var palette = vm.CreatePalette();
        ShowTask(view, "palette"); await Idle();
        Assert(vm.PaletteEntries.Count == 0 && vm.PaletteEmptyMessage.Contains("空", StringComparison.Ordinal), "W12 empty palette gives a concrete Library-add next action");
        SaveNamedView(view, "ui-palette-empty.png");
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
        vm.PaletteUseTemplateLayer = false; vm.PaletteMinimumText = "100"; vm.PaletteMaximumText = "102"; vm.PalettePreferredText = "100"; vm.SavePaletteLayer();
        vm.PalettePreferredText = "0100";
        Assert(vm.PaletteLayerDirty && !vm.QuickDropCommand.CanExecute(null), "W12 textual numeric edit is explicitly unsaved before the save action");
        view.PaletteSurface.DropSurface.LayerEditor.IsExpanded = true; await Idle();
        await InvokeSelectionButton(view.PaletteSurface.DropSurface.SaveLayerButton);
        vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        Assert(!vm.HasError && !vm.PaletteLayerDirty && vm.PalettePreferredText == "100" && vm.QuickDropCommand.CanExecute(null), "W12 actual Save normalizes equivalent numeric text and clears the dirty indicator");
        view.PaletteSurface.DropSurface.LayerEditor.IsExpanded = false;
        timeline.SelectedItems = [voice]; await Idle();
        Assert(vm.CurrentPalette?.Id == palette.Id, "W12 Style palette remains manually chosen while a Character Voice is selected");
        vm.ManualStylePalette = vm.StylePalettes.First(x => x.Id != palette.Id);
        Assert(vm.CurrentPalette?.Id == vm.ManualStylePalette?.Id && vm.CurrentPalette?.Id != palette.Id, "W12 explicit Style switching changes only the palette vocabulary");
        vm.ManualStylePalette = vm.StylePalettes.Single(x => x.Id == palette.Id);
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.TargetCompanion);
        var basePreset = vm.SelectedSelectionPreset!;
        vm.CopySelectionPreset(); var firstCopy = vm.SelectedSelectionPreset!;
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == basePreset.Id);
        vm.CopySelectionPreset(); var secondCopy = vm.SelectedSelectionPreset!;
        Assert(firstCopy.Id != secondCopy.Id && firstCopy.Name != secondCopy.Name, "W12 two copies of the same selection preset have distinguishable names and stable IDs");
        vm.DeleteSelectionPreset(); vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == firstCopy.Id); vm.DeleteSelectionPreset();
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == basePreset.Id);
        vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id);
        ShowTask(view, "selection"); await Idle();
        Assert(view.SelectionSurface.TemplateSelector.ItemTemplate != null && TextSearch.GetTextPath(view.SelectionSurface.TemplateSelector) == "DisplayName",
            "W12 selection Template chooser has a detailed source/Character template and short-name keyboard search");
        Assert(view.SelectionSurface.ProfileSelector.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.Status == BindingStatus.Active &&
            view.SelectionSurface.PlaceSelectionButton.GetBindingExpression(Button.CommandProperty)?.Status == BindingStatus.Active,
            "W12 real selection profile list and placement command bindings are active");
        ShowTask(view, "library"); await Idle();
        var search = view.LibrarySurface.LibrarySearchBox;
        search.Text = "W12/Overview"; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource(); await Idle();
        Assert(vm.LibraryEntries.Count == 1 && vm.LibraryEntries[0].Id == entry.Id, "W12 actual Library search matches a source name without altering its reference");
        search.Text = "no-result-79fba0"; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource(); await Idle();
        view.LibrarySurface.LibraryEmptyNotice.BringIntoView(); await Idle();
        Assert(vm.LibraryEntries.Count == 0 && view.LibrarySurface.LibraryEmptyNotice.IsVisible, "W12 empty Library search shows recovery guidance instead of a silent blank list");
        SaveNamedView(view, "ui-library-empty.png");
        search.Text = ""; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource(); await Idle();
        Assert(vm.LibraryEntries.Any(x => x.Id == entry.Id), "W12 clearing the search restores the same Library entry");
        var names = new[] { "expression", "selection", "palette", "library" };
        for (var tab = 0; tab < names.Length; tab++)
        {
            ShowTask(view, names[tab]); await Idle();
            if (tab == 1) Descendant<ScrollViewer>(view.SelectionSurface)?.ScrollToTop();
            if (tab == 3) Descendant<ScrollViewer>(view.LibrarySurface)?.ScrollToTop();
            await Idle();
            Assert(view.IsLoaded && TaskIsVisible(view, names[tab]) && view.Background != null && view.Foreground != null,
                "W12 native tab has a loaded, themed surface: " + names[tab]);
            SaveNamedView(view, "ui-" + names[tab] + "-normal.png");
        }
        var width = view.Width; var height = view.Height;
        try
        {
            view.Width = 360; view.Height = 320;
            for (var tab = 0; tab < names.Length; tab++)
            {
                ShowTask(view, names[tab]); await Idle(); view.UpdateLayout();
                Assert(view.ActualWidth >= 360 && view.ActualHeight >= 280, "W12 narrow native layout remains measurable: " + names[tab]);
                SaveNamedView(view, "ui-" + names[tab] + "-narrow.png");
            }
        }
        finally { view.Width = width; view.Height = height; await Idle(); }
        timeline.SelectedItems = []; ShowTask(view, "selection"); await Idle();
        Assert(vm.SelectionProfiles.Count == 0 && !vm.PlaceSelectionCommand.CanExecute(null) && vm.SelectionContext.Contains("タイムライン", StringComparison.Ordinal),
            "W12 empty Timeline selection names the correct selection surface and disables placement");
        SaveNamedView(view, "ui-selection-empty.png");
        vm.ActivePaletteKind = PaletteKind.Style; vm.ManualStylePalette = vm.StylePalettes.Single(x => x.Id == palette.Id); vm.DeleteCurrentPalette();
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == oldStyle); vm.ActivePaletteKind = oldKind;
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        ItemSettings.Default.Templates.Remove(template); vm.Refresh(); ShowTask(view, "expression"); await Idle();
        Assert(Signature(timeline) == before && new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId == nextId,
            "W12 UI review and settings edits do not mutate Timeline Items or allocate association IDs");
        Log("W12_UI=PASS");
    }
}
