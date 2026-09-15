using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static Border? FindDragHandle(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Border border && Equals(border.Tag, "PaletteDragHandle")) return border;
            var nested = FindDragHandle(child); if (nested != null) return nested;
        }
        return null;
    }

    private static async Task VerifyPaletteOrdering(Timeline timeline)
    {
        stage = "WUX10 Palette ordering";
        var vm = ViewModel!; var view = View!; var panel = view.PaletteSurface;
        timeline.SelectedItems = []; vm.Refresh(); ShowTask(view, "palette"); await Idle();
        var beforeTimeline = Signature(timeline);
        var beforeSettings = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var row = vm.Rows.First(x => x.Character == "TestA");
        var character = row.Target.Voice.Character ?? throw new InvalidOperationException("WUX10 TestA Character missing.");
        var characterPalette = vm.PaletteChoices.Single(x => x.Kind == PaletteKind.Character && x.CharacterName == "TestA");
        vm.SelectedPalette = characterPalette; await Idle();
        var originalIds = vm.CurrentPalette!.LibraryEntryIds.ToArray();
        var sources = new[]
        {
            Template("WUX10/First", [new TachieFaceItem(character) { Length = 21, Layer = 201 }]),
            Template("WUX10/Second", [new TachieFaceItem(character) { Length = 22, Layer = 202 }]),
            Template("WUX10/Third", [new TachieFaceItem(character) { Length = 23, Layer = 203 }])
        };
        foreach (var source in sources) ItemSettings.Default.Templates.Add(source);
        vm.Refresh(); vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == characterPalette.Id);
        await InvokeSelectionButton(panel.AddTemplateButton);
        foreach (var source in sources) await SetAddSource(view.TemplateAdditionSurface, source);
        await InvokeSelectionButton(view.TemplateAdditionSurface.AddButton);
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var entries = sources.Select(source => saved.Library.Single(x => x.Source == TemplateLocator.Capture(source))).ToArray();
        var ids = entries.Select(x => x.Id).ToArray();
        var savedCharacter = saved.Palettes.Single(x => x.Id == characterPalette.Id);
        Assert(savedCharacter.LibraryEntryIds.Take(originalIds.Length).SequenceEqual(originalIds) && savedCharacter.LibraryEntryIds.TakeLast(3).SequenceEqual(ids),
            "WUX10 fixture appends three exact entries without disturbing the existing Character Palette order");

        // A second Palette proves ordering is local, not a property of the shared LibraryEntry.
        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = "WUX10 mirror"; var mirror = vm.CreatePalette();
        foreach (var entry in entries)
        {
            vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
        }
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == characterPalette.Id); await Idle();
        vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == ids[1]);
        panel.PaletteEditor.IsExpanded = true; await Idle();
        Assert(panel.MoveUpButton.IsEnabled && panel.MoveDownButton.IsEnabled, "WUX10 a middle Palette item exposes both fallback move actions");
        await InvokeSelectionButton(panel.MoveUpButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Palettes.Single(x => x.Id == characterPalette.Id).LibraryEntryIds.TakeLast(3).SequenceEqual(new[] { ids[1], ids[0], ids[2] }) &&
            vm.SelectedPaletteEntry?.LibraryEntryId == ids[1],
            "WUX10 actual Up button persists order and keeps the moved entry selected");
        await InvokeSelectionButton(panel.MoveDownButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Palettes.Single(x => x.Id == characterPalette.Id).LibraryEntryIds.TakeLast(3).SequenceEqual(ids),
            "WUX10 actual Down button returns the entry through the same persisted move engine");

        // The drag/drop UI sends this same insertion request; move Third before First.
        var firstIndex = saved.Palettes.Single(x => x.Id == characterPalette.Id).LibraryEntryIds.IndexOf(ids[0]);
        var dragRequest = new PaletteMoveRequest(ids[2], firstIndex);
        Assert(vm.MovePaletteEntryCommand.CanExecute(dragRequest), "WUX10 drag move request is accepted by the shared Palette move command");
        vm.MovePaletteEntryCommand.Execute(dragRequest); await Idle();
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var reordered = new[] { ids[2], ids[0], ids[1] };
        Assert(saved.Palettes.Single(x => x.Id == characterPalette.Id).LibraryEntryIds.TakeLast(3).SequenceEqual(reordered),
            "WUX10 insertion-index move persists Third/First/Second as the user-defined order");
        Assert(saved.Palettes.Single(x => x.Id == mirror.Id).LibraryEntryIds.SequenceEqual(ids),
            "WUX10 reordering one Palette never changes another Palette using the same Library entries");

        // The Palette row has a dedicated drag handle, so normal row double-click remains Quick Drop territory.
        panel.PaletteList.ScrollIntoView(vm.PaletteEntries.Single(x => x.LibraryEntryId == ids[2])); panel.PaletteList.UpdateLayout(); await Idle();
        var container = panel.PaletteList.ItemContainerGenerator.ContainerFromItem(vm.PaletteEntries.Single(x => x.LibraryEntryId == ids[2])) as ListBoxItem
            ?? throw new InvalidOperationException("WUX10 Palette row was not realized.");
        var handle = FindDragHandle(container);
        Assert(panel.PaletteList.AllowDrop && handle != null && handle.Cursor == Cursors.SizeAll,
            "WUX10 actual Palette row exposes a dedicated drag handle and drop surface instead of hijacking the whole row");

        // Existing Character-expression preference follows the user order immediately.
        var candidateOrder = row.Choices.Skip(1).Where(x => x.Template != null && sources.Any(source => ReferenceEquals(source, x.Template.Template)))
            .Select(x => x.Template!.Template).ToArray();
        Assert(candidateOrder.SequenceEqual(new[] { sources[2], sources[0], sources[1] }),
            "WUX10 Character expression candidates immediately follow the reordered Palette priority");
        panel.PaletteEditor.IsExpanded = false;
        var oldWidth = view.Width; var oldHeight = view.Height;
        try
        {
            view.Width = 360; view.Height = 360; await Idle();
            Assert(InTaskViewport(panel.PaletteList, view) && handle.IsVisible, "WUX10 360px Palette keeps the drag handle and working list visible");
            SaveNamedView(view, "ux-palette-ordering-narrow.png");
        }
        finally { view.Width = oldWidth; view.Height = oldHeight; await Idle(); }
        Assert(Signature(timeline) == beforeTimeline, "WUX10 all ordering operations leave Timeline geometry and content unchanged");

        // Remove only the appended entries; the original TestA order must become byte-for-byte equivalent again.
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == characterPalette.Id);
        foreach (var id in ids)
        {
            vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == id); vm.RemovePaletteEntry();
        }
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Palettes.Single(x => x.Id == characterPalette.Id).LibraryEntryIds.SequenceEqual(originalIds),
            "WUX10 removing proof entries restores the exact pre-proof Character Palette order");
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == mirror.Id); vm.DeleteCurrentPalette();
        vm.LibrarySearch = "";
        foreach (var entry in entries)
        {
            var libraryView = vm.LibraryEntries.FirstOrDefault(x => x.Id == entry.Id);
            if (libraryView != null) { vm.SelectedLibraryEntry = libraryView; vm.UnregisterLibrary(); }
        }
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualStylePaletteId); vm.ActivePaletteKind = beforeSettings.PaletteMode;
        foreach (var source in sources) ItemSettings.Default.Templates.Remove(source);
        vm.Refresh();
        Assert(Signature(timeline) == beforeTimeline, "WUX10 fixture cleanup preserves the complete pre-existing native Timeline");
        Log("WUX10=PASS");
    }
}
