using System.IO;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyDirectTemplateAddition(Timeline timeline, UndoRedoManager undo)
    {
        stage = "WUX1 direct Template addition";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var original = Signature(timeline);
        Assert(vm.LibraryEntries.Count == 0 && vm.CharacterPalettes.Count == 0 && vm.StylePalettes.Count == 0,
            "WUX1 first-use fixture has no Library or Palette setup");
        var source = new TextItem { Length = 17, Layer = 110, Remark = "UX fixture source" };
        var template = Template("UX1/Flash", [source]); ItemSettings.Default.Templates.Add(template);
        view.MainTabs.SelectedIndex = 2; await Idle();
        Assert(view.PaletteSurface.AddTemplateButton.IsVisible && view.PaletteSurface.AddTemplateButton.IsEnabled,
            "WUX1 empty Palette exposes an enabled direct add action without opening management");
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        var add = view.TemplateAdditionSurface;
        Assert(vm.IsAddingTemplate && add.IsVisible && !view.MainTabs.IsVisible, "WUX1 add opens a focused secondary task and preserves the previous task");
        add.SourceList.SelectedItem = template; await Idle();
        add.NameBox.Text = "フラッシュ"; add.NameBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
        Assert(vm.AddTemplateDisplayName == "フラッシュ" && add.AddButton.IsEnabled, "WUX1 source and optional short name are real two-way-bound inputs");
        SaveNamedView(view, "ux-add-template-normal.png");
        var width = view.Width; var height = view.Height;
        try { view.Width = 360; view.Height = 360; await Idle(); SaveNamedView(view, "ux-add-template-narrow.png"); }
        finally { view.Width = width; view.Height = height; await Idle(); }
        await InvokeSelectionButton(add.AddButton);
        var firstPalette = vm.CurrentPalette!; var entry = vm.SelectedPaletteEntry!.Entry!;
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(!vm.HasError && !vm.IsAddingTemplate && saved.Library.Count == 1 && saved.Palettes.Count == 1 &&
            saved.Palettes.Single().LibraryEntryIds.Single() == entry.Id && entry.DisplayName == "フラッシュ",
            "WUX1 first-use single action atomically creates reference plus Palette membership");
        Assert(source.Remark == "UX fixture source" && source.Length == 17 && Signature(timeline) == original,
            "WUX1 organizing a Template never mutates source or Timeline");
        timeline.CurrentFrame = 1200; undo.Record();
        await InvokeSelectionButton(view.PaletteSurface.DropSurface.DropButton);
        var placed = timeline.Items.OfType<TextItem>().Single(x => x.Frame == 1200);
        Assert(!vm.HasError && placed.Length == 17 && !ReferenceEquals(placed, source) && !placed.Remark.Contains("CWT_TPL:", StringComparison.Ordinal),
            "WUX1 first-use Palette -> source -> add -> actual Quick Drop preserves intrinsic duration and independence");
        var dropped = Signature(timeline);
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == original, "WUX1 first-use placement has native Undo");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == dropped, "WUX1 first-use placement has native Redo");
        await undo.UndoAsync(); await Idle();
        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteName = "UX1 second"; var second = vm.CreatePalette();
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton); add.SourceList.SelectedItem = template; await Idle();
        Assert(vm.AddTemplateUsesExisting && add.NameBox.IsReadOnly && vm.AddTemplateDisplayName == "フラッシュ", "WUX1 an exact unique registration is reused without another naming decision");
        await InvokeSelectionButton(add.AddButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Library.Count == 1 && saved.Palettes.Single(x => x.Id == firstPalette.Id).LibraryEntryIds.Single() == entry.Id &&
            saved.Palettes.Single(x => x.Id == second.Id).LibraryEntryIds.Single() == entry.Id,
            "WUX1 adding the same source to a second Palette reuses the same stable reference");
        vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "duplicate plugin ref"; var duplicate = vm.RegisterLibrary();
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton); add.SourceList.SelectedItem = template; await Idle();
        var settingsBefore = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(add.AddButton);
        Assert(vm.HasError && vm.IsAddingTemplate && ReferenceEquals(vm.AddTemplateSource, template) &&
            File.ReadAllText(PlacerSettingsStore.DefaultPath) == settingsBefore && Signature(timeline) == original,
            "WUX1 multiple plugin registrations for one exact source stop without writes or loss of input");
        await InvokeSelectionButton(add.CancelButton);
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == duplicate.Id); vm.UnregisterLibrary();
        vm.DeleteCurrentPalette(); vm.ManualStylePalette = vm.StylePalettes.Single(x => x.Id == firstPalette.Id); vm.DeleteCurrentPalette();
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        // A live duplicate and a deleted source must also stop before creating even a Library entry.
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton); add.SourceList.SelectedItem = template; await Idle();
        var twin = Template(template.Name, [new TextItem { Length = 18, Layer = 110 }]); ItemSettings.Default.Templates.Add(twin);
        settingsBefore = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(add.AddButton);
        Assert(vm.HasError && vm.IsAddingTemplate && File.ReadAllText(PlacerSettingsStore.DefaultPath) == settingsBefore,
            "WUX1 ambiguous live source is rejected before registration or membership mutation");
        ItemSettings.Default.Templates.Remove(twin); ItemSettings.Default.Templates.Remove(template);
        await InvokeSelectionButton(add.AddButton);
        Assert(vm.HasError && ReferenceEquals(vm.AddTemplateSource, template) && File.ReadAllText(PlacerSettingsStore.DefaultPath) == settingsBefore,
            "WUX1 source removed while adding is not silently repaired and input remains available");
        await InvokeSelectionButton(add.CancelButton); vm.Refresh(); view.MainTabs.SelectedIndex = 0; await Idle();
        Assert(Signature(timeline) == original && vm.LibraryEntries.Count == 0 && vm.StylePalettes.Count == 0,
            "WUX1 fixture cleanup leaves the frozen regression ladder's starting state intact");
        Log("WUX1=PASS");
    }
}
