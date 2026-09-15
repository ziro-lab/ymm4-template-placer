using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyPalettes(Timeline timeline)
    {
        stage = "W4 Palettes";
        var vm = ViewModel!; var view = View!;
        timeline.SelectedItems = []; var before = Signature(timeline);
        var entryA = vm.LibraryEntries.Single(x => x.DisplayName == "どや").Entry;
        vm.SelectedSourceTemplate = vm.SourceTemplates.Single(x => x.Name == "TestB/Neutral"); vm.LibraryDisplayName = "笑顔";
        var entryB = vm.RegisterLibrary();
        ShowTask(view, "palette"); await Idle(); var panel = view.PaletteSurface;
        Assert(panel.IsLoaded && ReferenceEquals(panel.DataContext, vm), "W4 actual native Palette surface is hosted");
        vm.ActivePaletteKind = PaletteKind.Character; vm.NewPaletteName = "TestA棚";
        vm.NewPaletteCharacter = vm.LibraryCharacters.Single(x => x.Name == "TestA"); var pa = vm.CreatePalette();
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entryA.Id); vm.AddPaletteEntry();
        vm.NewPaletteName = "TestB棚"; vm.NewPaletteCharacter = vm.LibraryCharacters.Single(x => x.Name == "TestB"); var pb = vm.CreatePalette();
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entryB.Id); vm.AddPaletteEntry();
        Assert(vm.CurrentPalette?.Id == pb.Id && vm.PaletteEntries.Single().LibraryEntryId == entryB.Id, "W4 Character shelves reference their Library entries");
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entryA.Id);
        RejectWithoutMutation(timeline, vm.AddPaletteEntry, "W4 other Character entry is not silently reassigned into a Character shelf");
        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = "戦闘"; var battle = vm.CreatePalette();
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entryA.Id); vm.AddPaletteEntry();
        vm.NewPaletteName = "明るい"; var bright = vm.CreatePalette();
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entryA.Id);
        await InvokeSelectionButton(panel.AddTemplateButton);
        await SetAddSource(view.TemplateAdditionSurface, TemplateResolver.Resolve(entryA).Template!);
        await InvokeSelectionButton(view.TemplateAdditionSurface.AddButton);
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(!vm.HasError && saved.Palettes.Single(x => x.Id == battle.Id).LibraryEntryIds.Single() == entryA.Id && saved.Palettes.Single(x => x.Id == bright.Id).LibraryEntryIds.Single() == entryA.Id,
            "W4 one Library entry persists in multiple Style palettes through real UI command");
        vm.ActivePaletteKind = PaletteKind.Character; vm.ManualCharacterPalette = vm.CharacterPalettes.Single(x => x.Id == pb.Id);
        var a = timeline.Items.OfType<VoiceItem>().Single(x => x.CharacterName == "TestA");
        var b = timeline.Items.OfType<VoiceItem>().Single(x => x.CharacterName == "TestB");
        var fa = timeline.Items.OfType<TachieFaceItem>().First(x => x.CharacterName == "TestA");
        var configurationBeforeContext = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        timeline.SelectedItems = [a]; await Idle();
        Assert(vm.CurrentPalette?.Id == pa.Id && vm.ManualCharacterPalette?.Id == pb.Id && vm.HasCharacterContext, "W4 public single Voice selection temporarily overrides, never replaces, manual palette");
        timeline.SelectedItems = [fa]; await Idle();
        Assert(vm.CurrentPalette?.Id == pa.Id && vm.ManualCharacterPalette?.Id == pb.Id, "W4 public single Face selection supplies Character context");
        timeline.SelectedItems = [a, b]; await Idle();
        Assert(vm.CurrentPalette?.Id == pb.Id && !vm.HasCharacterContext, "W4 multi-selection restores manual palette without guessing one Character");
        timeline.SelectedItems = [a]; await Idle(); timeline.SelectedItems = []; await Idle();
        Assert(vm.CurrentPalette?.Id == pb.Id && !vm.HasCharacterContext, "W4 selection clear restores the prior manual shelf");
        Assert(File.ReadAllText(PlacerSettingsStore.DefaultPath) == configurationBeforeContext, "W4 temporary selection context does not rewrite persistent manual settings");
        vm.ActivePaletteKind = PaletteKind.Style; vm.ManualStylePalette = vm.StylePalettes.Single(x => x.Id == battle.Id);
        timeline.SelectedItems = [a]; await Idle();
        Assert(vm.CurrentPalette?.Id == battle.Id && vm.ManualStylePalette?.Id == battle.Id, "W4 Style selection stays manual during Character context changes");
        timeline.SelectedItems = []; vm.ActivePaletteKind = PaletteKind.Character;
        var duplicateCharacter = Template("AmbiguousCharacterProbe", [new TachieFaceItem(new Character { Name = "TestA" }) { Length = 10 }]);
        ItemSettings.Default.Templates.Add(duplicateCharacter);
        try
        {
            timeline.SelectedItems = [a]; await Idle();
            Assert(vm.CurrentPalette?.Id == pb.Id && !vm.HasCharacterContext, "W4 duplicate Character name is not fuzzy-resolved");
        }
        finally { ItemSettings.Default.Templates.Remove(duplicateCharacter); timeline.SelectedItems = []; }
        Assert(Signature(timeline) == before, "W4 palette creation membership and context switching never mutate Timeline Items");
        panel.PaletteEditor.IsExpanded = false; await Idle(); SaveView(view); ShowTask(view, "expression"); await Idle();
        Log("W4=PASS");
    }
}
