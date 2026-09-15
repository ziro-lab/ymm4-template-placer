using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyIdentityClarity(Timeline timeline)
    {
        stage = "WUX12 identity clarity";
        var vm = ViewModel!; var view = View!; var panel = view.PaletteSurface;
        timeline.SelectedItems = []; vm.Refresh(); ShowTask(view, "palette"); await Idle();
        var beforeTimeline = Signature(timeline);
        var beforeSettings = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var testA = vm.PaletteChoices.Single(x => x.Kind == PaletteKind.Character && x.CharacterName == "TestA");
        var testB = vm.PaletteChoices.Single(x => x.Kind == PaletteKind.Character && x.CharacterName == "TestB");
        var originalA = testA.Name; var originalB = testB.Name;

        // Same user-facing Palette name remains allowed, but the picker must become distinguishable only for the collision.
        vm.SelectedPalette = testA; vm.PaletteNameDraft = "WUX12 表情"; vm.RenamePalette();
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == testB.Id); vm.PaletteNameDraft = "WUX12 表情"; vm.RenamePalette();
        var collided = vm.PaletteChoices.Where(x => x.Name == "WUX12 表情").ToArray();
        Assert(collided.Length == 2 && collided.Any(x => x.DisplayLabel == "WUX12 表情 — TestA") && collided.Any(x => x.DisplayLabel == "WUX12 表情 — TestB"),
            "WUX12 duplicate Character Palette names remain legal but gain Character disambiguation in the picker");
        Assert(panel.PaletteSelector.DisplayMemberPath == nameof(PaletteChoice.DisplayLabel) && panel.PaletteSelector.Items.Cast<PaletteChoice>()
            .Where(x => x.Name == "WUX12 表情").Select(x => x.DisplayLabel).Distinct(StringComparer.Ordinal).Count() == 2,
            "WUX12 actual Palette ComboBox renders the collision-aware labels rather than two identical names");
        Assert(vm.PaletteChoices.Where(x => x.Name != "WUX12 表情").All(x => x.DisplayLabel == x.Name),
            "WUX12 unique Palette names stay short and do not gain unnecessary implementation detail");

        // Two strict TestA sources may intentionally share the same short alias; only then show their source names.
        var voice = vm.Rows.First(x => x.Character == "TestA").Target.Voice;
        var character = voice.Character ?? throw new InvalidOperationException("WUX12 TestA Character missing.");
        var sourceA = Template("WUX12/Smile-A", [new TachieFaceItem(character) { Length = 24, Layer = 230 }]);
        var sourceB = Template("WUX12/Smile-B", [new TachieFaceItem(character) { Length = 25, Layer = 231 }]);
        ItemSettings.Default.Templates.Add(sourceA); ItemSettings.Default.Templates.Add(sourceB); vm.Refresh();
        vm.LibrarySearch = "";
        vm.SelectedSourceTemplate = sourceA; vm.LibraryDisplayName = "WUX12 笑顔";
        vm.SelectedLibraryCharacter = vm.LibraryCharacters.Single(x => x.Name == "TestA"); var entryA = vm.RegisterLibrary();
        vm.SelectedSourceTemplate = sourceB; vm.LibraryDisplayName = "WUX12 笑顔";
        vm.SelectedLibraryCharacter = vm.LibraryCharacters.Single(x => x.Name == "TestA"); var entryB = vm.RegisterLibrary();
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == testA.Id);
        foreach (var entry in new[] { entryA, entryB })
        {
            vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
        }
        var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
        var exact = row.Choices.Where(x => x.Template != null && (ReferenceEquals(x.Template.Template, sourceA) || ReferenceEquals(x.Template.Template, sourceB))).ToArray();
        Assert(exact.Length == 2 && exact.Any(x => x.DisplayName == "WUX12 笑顔 — WUX12/Smile-A") &&
            exact.Any(x => x.DisplayName == "WUX12 笑顔 — WUX12/Smile-B") && !ReferenceEquals(exact[0].Template!.Template, exact[1].Template!.Template),
            "WUX12 duplicate Expression aliases show source context while retaining two strict distinct native sources");
        Assert(row.Choices.Where(x => x.Template != null && !exact.Contains(x)).All(x =>
            !x.DisplayName.StartsWith("WUX12 笑顔 — ", StringComparison.Ordinal)),
            "WUX12 source detail is collision-local rather than added to unrelated Expression candidates");

        ShowTask(view, "expression"); await Idle(); view.VoiceGrid.ScrollIntoView(row); view.VoiceGrid.UpdateLayout(); await Idle();
        var rowView = view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow
            ?? throw new InvalidOperationException("WUX12 Expression row was not realized.");
        var combo = Descendant<ComboBox>(rowView) ?? throw new InvalidOperationException("WUX12 Expression selector was not realized.");
        var uiExact = combo.Items.Cast<TemplateChoice>().Where(x => x.Template != null &&
            (ReferenceEquals(x.Template.Template, sourceA) || ReferenceEquals(x.Template.Template, sourceB))).ToArray();
        Assert(combo.DisplayMemberPath == nameof(TemplateChoice.DisplayName) && uiExact.Select(x => x.DisplayName).Distinct(StringComparer.Ordinal).Count() == 2,
            "WUX12 actual Expression ComboBox exposes two distinguishable choices without changing strict source identity");
        row.SelectedChoice = uiExact[0]; await Idle();
        Assert(ReferenceEquals(combo.SelectedItem, uiExact[0]) && combo.Text == uiExact[0].DisplayName,
            "WUX12 collapsed Expression selector visibly presents the source-qualified alias after selection");
        var oldWidth = view.Width; var oldHeight = view.Height;
        try
        {
            view.Width = 360; view.Height = 360; await Idle();
            Assert(InTaskViewport(combo, view) && InTaskViewport(view.PlaceButton, view),
                "WUX12 360px keeps the collision-aware Expression selector and primary action reachable");
            SaveNamedView(view, "ux-identity-collisions-narrow.png");
        }
        finally { view.Width = oldWidth; view.Height = oldHeight; await Idle(); }
        row.SelectedChoice = row.Choices[0];
        Assert(Signature(timeline) == beforeTimeline, "WUX12 all identity-only UI changes leave Timeline content unchanged");

        // Cleanup aliases and restore the original Palette names and manual selections.
        ShowTask(view, "palette"); vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == testA.Id);
        foreach (var id in new[] { entryA.Id, entryB.Id })
        {
            var paletteEntry = vm.PaletteEntries.FirstOrDefault(x => x.LibraryEntryId == id);
            if (paletteEntry != null) { vm.SelectedPaletteEntry = paletteEntry; vm.RemovePaletteEntry(); }
        }
        vm.LibrarySearch = "";
        foreach (var id in new[] { entryA.Id, entryB.Id })
        {
            var libraryEntry = vm.LibraryEntries.FirstOrDefault(x => x.Id == id);
            if (libraryEntry != null) { vm.SelectedLibraryEntry = libraryEntry; vm.UnregisterLibrary(); }
        }
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == testA.Id); vm.PaletteNameDraft = originalA; vm.RenamePalette();
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == testB.Id); vm.PaletteNameDraft = originalB; vm.RenamePalette();
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualStylePaletteId); vm.ActivePaletteKind = beforeSettings.PaletteMode;
        ItemSettings.Default.Templates.Remove(sourceA); ItemSettings.Default.Templates.Remove(sourceB); vm.Refresh();
        Assert(Signature(timeline) == beforeTimeline, "WUX12 fixture cleanup restores the pre-existing Timeline and vocabulary");
        Log("WUX12=PASS");
    }
}
