using System.IO;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifySafetyIntent(Timeline timeline)
    {
        stage = "WUX11 safety and intent";
        var vm = ViewModel!; var view = View!; var palettePanel = view.PaletteSurface;
        timeline.SelectedItems = []; vm.Refresh(); ShowTask(view, "palette"); await Idle();
        var beforeTimeline = Signature(timeline);
        var beforeSettings = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var sourceA = Template("WUX11/A", [new TextItem { Length = 17, Layer = 220 }]);
        var sourceB = Template("WUX11/B", [new TextItem { Length = 19, Layer = 221 }]);
        ItemSettings.Default.Templates.Add(sourceA); ItemSettings.Default.Templates.Add(sourceB); vm.Refresh();

        vm.LibrarySearch = "";
        vm.SelectedSourceTemplate = sourceA; vm.LibraryDisplayName = "WUX11 A";
        vm.SelectedLibraryCharacter = vm.LibraryCharacters.First(x => x.Name == null); var entryA = vm.RegisterLibrary();
        vm.SelectedSourceTemplate = sourceB; vm.LibraryDisplayName = "WUX11 B";
        vm.SelectedLibraryCharacter = vm.LibraryCharacters.First(x => x.Name == null); var entryB = vm.RegisterLibrary();

        PaletteDefinition CreateStyle(string name, params LibraryEntry[] entries)
        {
            vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = name;
            var palette = vm.CreatePalette();
            foreach (var entry in entries)
            {
                vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
            }
            return palette;
        }
        var deletePalette = CreateStyle("WUX11 削除確認", entryA, entryB);
        var keepOne = CreateStyle("WUX11 保持1", entryA);
        var keepTwo = CreateStyle("WUX11 保持2", entryA);

        // Palette delete: cancel must be byte-for-byte zero-write and explain the exact consequence.
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == deletePalette.Id); palettePanel.PaletteEditor.IsExpanded = true; await Idle();
        string deleteMessage = "";
        vm.ConfirmationOverride = message => { deleteMessage = message; return false; };
        var settingsBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(palettePanel.DeletePaletteButton);
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Palettes.Any(x => x.Id == deletePalette.Id) && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(settingsBytes) && Signature(timeline) == beforeTimeline,
            "WUX11 cancelling Palette deletion is a byte-for-byte zero-write operation");
        Assert(deleteMessage.Contains(deletePalette.Name, StringComparison.Ordinal) && deleteMessage.Contains("2件", StringComparison.Ordinal) &&
            deleteMessage.Contains("元テンプレート", StringComparison.Ordinal) && deleteMessage.Contains("テンプレート管理", StringComparison.Ordinal) &&
            deleteMessage.Contains("タイムライン", StringComparison.Ordinal),
            "WUX11 Palette delete confirmation states the item count and what will not be deleted");

        vm.ConfirmationOverride = _ => true;
        await InvokeSelectionButton(palettePanel.DeletePaletteButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(!saved.Palettes.Any(x => x.Id == deletePalette.Id) && saved.Library.Any(x => x.Id == entryA.Id) && saved.Library.Any(x => x.Id == entryB.Id) &&
            ItemSettings.Default.Templates.Contains(sourceA) && ItemSettings.Default.Templates.Contains(sourceB) && Signature(timeline) == beforeTimeline,
            "WUX11 confirmed Palette deletion removes only that Palette, not registrations, native Templates or Timeline items");

        // Library unregister: two remaining Palette memberships must be called out before removal.
        vm.OpenTemplateManagementCommand.Execute(null); await Idle();
        vm.LibrarySearch = ""; vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entryA.Id); await Idle();
        string unregisterMessage = "";
        vm.ConfirmationOverride = message => { unregisterMessage = message; return false; };
        settingsBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(view.LibrarySurface.UnregisterButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Library.Any(x => x.Id == entryA.Id) && saved.Palettes.Single(x => x.Id == keepOne.Id).LibraryEntryIds.Contains(entryA.Id) &&
            saved.Palettes.Single(x => x.Id == keepTwo.Id).LibraryEntryIds.Contains(entryA.Id) && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(settingsBytes) &&
            Signature(timeline) == beforeTimeline,
            "WUX11 cancelling Library unregister preserves registration and every Palette membership without writes");
        Assert(unregisterMessage.Contains(entryA.DisplayName, StringComparison.Ordinal) && unregisterMessage.Contains("2個のパレット", StringComparison.Ordinal) &&
            unregisterMessage.Contains("元のYMM4テンプレート", StringComparison.Ordinal) && unregisterMessage.Contains("タイムライン", StringComparison.Ordinal),
            "WUX11 unregister confirmation states the real Palette impact and preserved native content");

        vm.ConfirmationOverride = _ => true;
        await InvokeSelectionButton(view.LibrarySurface.UnregisterButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(!saved.Library.Any(x => x.Id == entryA.Id) && !saved.Palettes.Single(x => x.Id == keepOne.Id).LibraryEntryIds.Contains(entryA.Id) &&
            !saved.Palettes.Single(x => x.Id == keepTwo.Id).LibraryEntryIds.Contains(entryA.Id) && ItemSettings.Default.Templates.Contains(sourceA) &&
            Signature(timeline) == beforeTimeline,
            "WUX11 confirmed unregister removes the reference and all Palette memberships while preserving source and Timeline");
        Assert(vm.Status.Contains("すべてのパレットからも外しました", StringComparison.Ordinal),
            "WUX11 unregister result truthfully reports the Palette side effect");

        // Destructive discard, hierarchy navigation, and scene reload must use different verbs.
        Assert(view.TemplateAdditionSurface.CancelButton.Content?.ToString() == "キャンセル",
            "WUX11 Template Add discard is labelled Cancel rather than Back");
        Assert(view.BackFromManagementButton.Content?.ToString() == "戻る",
            "WUX11 nested Template Management navigation remains Back because it preserves the parent task");
        Assert(view.RefreshButton.Content?.ToString() == "シーン更新" && view.ExpressionEmptyNotice.Text.Contains("［シーン更新］", StringComparison.Ordinal),
            "WUX11 global refresh is named Scene Refresh in both action and recovery guidance");

        vm.ConfirmationOverride = null;
        vm.CloseTemplateManagementCommand.Execute(null); ShowTask(view, "palette"); await Idle();
        foreach (var paletteId in new[] { keepOne.Id, keepTwo.Id })
        {
            if (!vm.PaletteChoices.Any(x => x.Id == paletteId)) continue;
            vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == paletteId); vm.DeleteCurrentPalette();
        }
        vm.LibrarySearch = "";
        var remainingB = vm.LibraryEntries.FirstOrDefault(x => x.Id == entryB.Id);
        if (remainingB != null) { vm.SelectedLibraryEntry = remainingB; vm.UnregisterLibrary(); }
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualStylePaletteId); vm.ActivePaletteKind = beforeSettings.PaletteMode;
        ItemSettings.Default.Templates.Remove(sourceA); ItemSettings.Default.Templates.Remove(sourceB); vm.Refresh();
        Assert(Signature(timeline) == beforeTimeline, "WUX11 fixture cleanup preserves the complete pre-existing native Timeline");
        Log("WUX11=PASS");
    }
}
