using System.IO;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyPaletteTask(Timeline timeline)
    {
        stage = "WUX2 unified Palette task";
        var vm = ViewModel!; var view = View!; var panel = view.PaletteSurface;
        timeline.SelectedItems = []; ShowTask(view, "palette"); await Idle();
        var before = Signature(timeline);
        var originalKind = vm.ActivePaletteKind; var originalCharacter = vm.ManualCharacterPalette?.Id; var originalStyle = vm.ManualStylePalette?.Id;
        Assert(vm.PaletteChoices.Count == vm.CharacterPalettes.Count + vm.StylePalettes.Count &&
            ReferenceEquals(panel.PaletteSelector.ItemsSource, vm.PaletteChoices),
            "WUX2 one actual picker exposes every linked and manual Palette without a Kind decision");
        await InvokeSelectionButton(panel.NewPaletteButton);
        Assert(panel.CreatePaletteForm.IsVisible && vm.NewPaletteCharacter?.Name == null,
            "WUX2 creation defaults to no link, independently of the last internal Palette mode");
        panel.NewPaletteNameBox.Text = "字幕と効果";
        panel.NewPaletteNameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        await InvokeSelectionButton(panel.CreatePaletteButton);
        var manualId = vm.CurrentPalette!.Id;
        Assert(!vm.HasError && !panel.CreatePaletteForm.IsVisible && vm.CurrentPalette.Kind == PaletteKind.Style && vm.CurrentPalette.CharacterName == null,
            "WUX2 optional Character absence derives a persisted manual Palette through actual form controls");
        vm.PaletteNameDraft = "字幕・効果"; vm.RenamePalette();
        Assert(vm.CurrentPalette?.Id == manualId && vm.CurrentPalette.Name == "字幕・効果" && vm.CurrentPalette.LibraryEntryIds.Count == 0,
            "WUX2 renaming the automatic/default vocabulary preserves stable identity and membership");
        await InvokeSelectionButton(panel.NewPaletteButton);
        panel.NewPaletteNameBox.Text = "TestC・表情"; panel.NewPaletteNameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        panel.NewCharacterCombo.SelectedItem = vm.LibraryCharacters.Single(x => x.Name == "TestC"); await Idle();
        await InvokeSelectionButton(panel.CreatePaletteButton);
        var linkedId = vm.CurrentPalette!.Id;
        Assert(!vm.HasError && vm.CurrentPalette.Kind == PaletteKind.Character && vm.CurrentPalette.CharacterName == "TestC" &&
            new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Palettes.Single(x => x.Id == linkedId).Kind == PaletteKind.Character,
            "WUX2 optional Character presence derives the existing linked Palette representation without a mode control");
        var a = timeline.Items.OfType<VoiceItem>().Single(x => x.CharacterName == "TestA");
        var pa = vm.PaletteChoices.Single(x => x.CharacterName == "TestA").Id;
        var pb = vm.PaletteChoices.Single(x => x.CharacterName == "TestB").Id;
        panel.PaletteSelector.SelectedItem = vm.PaletteChoices.Single(x => x.Id == pb); await Idle();
        var persisted = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        timeline.SelectedItems = [a]; await Idle();
        Assert(vm.CurrentPalette?.Id == pa && (panel.PaletteSelector.SelectedItem as PaletteChoice)?.Id == pa && vm.PaletteContextStatus.Contains("連動中", StringComparison.Ordinal),
            "WUX2 automatic Character context updates the actual unified picker and identifies the reason");
        timeline.SelectedItems = []; await Idle();
        Assert(vm.CurrentPalette?.Id == pb && (panel.PaletteSelector.SelectedItem as PaletteChoice)?.Id == pb &&
            File.ReadAllText(PlacerSettingsStore.DefaultPath) == persisted,
            "WUX2 context clear visibly restores the manual choice without persisting transient context");
        timeline.SelectedItems = [a]; await Idle();
        panel.PaletteSelector.SelectedItem = vm.PaletteChoices.Single(x => x.Id == pb); await Idle();
        Assert(vm.CurrentPalette?.Id == pb && !vm.HasCharacterContext,
            "WUX2 an explicit Palette picker choice is not immediately undone by the old Timeline context");
        timeline.SelectedItems = []; await Idle(); timeline.SelectedItems = [a]; await Idle();
        Assert(vm.CurrentPalette?.Id == pa && vm.HasCharacterContext, "WUX2 subsequent Timeline selection resumes linked Character context");
        panel.PaletteSelector.SelectedItem = vm.PaletteChoices.Single(x => x.Id == manualId); await Idle();
        timeline.SelectedItems = []; await Idle(); timeline.SelectedItems = [a]; await Idle();
        Assert(vm.CurrentPalette?.Id == manualId && !vm.HasCharacterContext && string.IsNullOrEmpty(vm.PaletteContextStatus),
            "WUX2 an unlinked editing vocabulary stays manual without a normal-state explanatory paragraph");
        var entry = vm.LibraryEntries.Single(x => x.DisplayName == "どや").Entry;
        Assert(new PaletteEntryView(entry.Id, entry, "TestA").Status == "" && new LibraryEntryView(entry).Status == "",
            "WUX2 resolved entries render no available/ready status in either list");
        var source = TemplateResolver.Resolve(entry).Template!;
        ItemSettings.Default.Templates.Remove(source);
        Assert(new PaletteEntryView(entry.Id, entry).Status.Contains("見つかりません", StringComparison.Ordinal) && new LibraryEntryView(entry).Status.Length > 0,
            "WUX2 missing references retain actionable warnings while normal status is hidden");
        ItemSettings.Default.Templates.Add(source);
        var duplicate = Template(source.Name, [new TachieFaceItem(a.Character) { Length = 10 }]);
        ItemSettings.Default.Templates.Add(duplicate);
        Assert(new PaletteEntryView(entry.Id, entry).Status.Contains("一意", StringComparison.Ordinal), "WUX2 ambiguous source remains a visible warning");
        ItemSettings.Default.Templates.Remove(duplicate);
        Assert(new PaletteEntryView(entry.Id, entry, "TestB").Status.Contains("違います", StringComparison.Ordinal), "WUX2 a Palette Character mismatch is visible, not presented as ready");
        timeline.SelectedItems = []; panel.PaletteSelector.SelectedItem = vm.PaletteChoices.Single(x => x.Id == pb); await Idle();
        SaveNamedView(view, "ux-palette-unified-normal.png");
        var width = view.Width; var height = view.Height;
        try { view.Width = 360; view.Height = 360; await Idle(); SaveNamedView(view, "ux-palette-unified-narrow.png"); }
        finally { view.Width = width; view.Height = height; await Idle(); }
        panel.PaletteSelector.SelectedItem = vm.PaletteChoices.Single(x => x.Id == linkedId); await Idle(); vm.DeleteCurrentPalette();
        panel.PaletteSelector.SelectedItem = vm.PaletteChoices.Single(x => x.Id == manualId); await Idle(); vm.DeleteCurrentPalette();
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == originalCharacter);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == originalStyle); vm.ActivePaletteKind = originalKind;
        Assert(Signature(timeline) == before, "WUX2 creation, renaming, switching, warnings and context do not mutate Timeline Items");
        ShowTask(view, "expression"); await Idle(); Log("WUX2=PASS");
    }
}
