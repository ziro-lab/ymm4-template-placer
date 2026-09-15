using System.Collections.Immutable;
using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task SetAddSource(TemplateAdditionPanel panel, ItemTemplate source, bool selected = true)
    {
        var vm = (PlacerViewModel)panel.DataContext;
        var choice = vm.AddTemplateChoices.Single(x => ReferenceEquals(x.Source, source));
        panel.SourceList.ScrollIntoView(choice); panel.SourceList.UpdateLayout(); await Idle();
        var container = panel.SourceList.ItemContainerGenerator.ContainerFromItem(choice) as ListBoxItem
            ?? throw new InvalidOperationException("Bulk-add source row was not realized.");
        var box = Descendant<CheckBox>(container) ?? throw new InvalidOperationException("Bulk-add source checkbox was not realized.");
        if (box.IsChecked != selected)
            ((IToggleProvider)new CheckBoxAutomationPeer(box).GetPattern(PatternInterface.Toggle)).Toggle();
        await Idle();
        Assert(choice.IsSelected == selected, selected ? "bulk-add source checkbox selects the source" : "bulk-add source checkbox clears the source");
    }

    private static async Task VerifyBulkPaletteAdd(Timeline timeline)
    {
        stage = "WUX9 bulk Palette add";
        var vm = ViewModel!; var view = View!; var panel = view.TemplateAdditionSurface;
        timeline.SelectedItems = []; vm.Refresh(); ShowTask(view, "palette"); await Idle();
        var beforeTimeline = Signature(timeline);
        var beforeSettings = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var sources = Enumerable.Range(0, 10).Select(i => Template($"WUX9/Bulk{i:00}", [new TextItem { Length = 10 + i, Layer = 170 + i }])).ToArray();
        foreach (var source in sources) ItemSettings.Default.Templates.Add(source);
        vm.Refresh();

        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = "WUX9 bulk";
        var firstPalette = vm.CreatePalette();
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        foreach (var source in sources) await SetAddSource(panel, source);
        Assert(vm.AddTemplateSelectedCount == 10 && vm.AddTemplateActionLabel == "10件をパレットへ追加" && !vm.AddTemplateIsSingleSelection,
            "WUX9 one add task represents ten selected native Templates without ten registration dialogs");
        vm.AddTemplateSearch = "Bulk00"; await Idle();
        Assert(vm.AddTemplateChoices.Count == 1 && vm.AddTemplateSelectedCount == 10 && panel.AddButton.IsEnabled,
            "WUX9 search can hide selected rows without discarding the batch selection");
        vm.AddTemplateSearch = ""; await Idle();
        var width = view.Width; var height = view.Height;
        try
        {
            view.Width = 360; view.Height = 360; await Idle();
            Assert(InTaskViewport(panel.AddButton, view) && vm.AddTemplateSelectionSummary == "10件選択中",
                "WUX9 360px bulk task keeps selected count and completion action reachable");
            SaveNamedView(view, "ux-bulk-add-narrow.png");
        }
        finally { view.Width = width; view.Height = height; await Idle(); }
        await InvokeSelectionButton(panel.AddButton);
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var first = saved.Palettes.Single(x => x.Id == firstPalette.Id);
        var bulkEntries = saved.Library.Where(x => sources.Any(source => x.Source == TemplateLocator.Capture(source))).ToArray();
        Assert(!vm.HasError && bulkEntries.Length == 10 && first.LibraryEntryIds.Count == 10 &&
            sources.All(source => bulkEntries.Count(x => x.Source == TemplateLocator.Capture(source)) == 1),
            "WUX9 one completion atomically creates ten exact references and ten Palette memberships");
        Assert(Signature(timeline) == beforeTimeline, "WUX9 bulk organization never mutates the Timeline");

        // Reuse the exact same Library entries in a second Palette. No duplicate registration is created.
        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = "WUX9 reuse"; var secondPalette = vm.CreatePalette();
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        foreach (var source in sources) await SetAddSource(panel, source);
        var libraryCount = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Library.Count;
        await InvokeSelectionButton(panel.AddButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Library.Count == libraryCount && saved.Palettes.Single(x => x.Id == secondPalette.Id).LibraryEntryIds.SequenceEqual(first.LibraryEntryIds),
            "WUX9 bulk add reuses the same stable Library entries in another Palette without duplicates");
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        var already = vm.AddTemplateChoices.Single(x => ReferenceEquals(x.Source, sources[0]));
        Assert(already.IsAlreadyAdded && !already.CanSelect, "WUX9 a Template already in the current Palette is shown as added and cannot be selected again");
        await InvokeSelectionButton(panel.CancelButton);

        // One ambiguous registration invalidates the whole batch before any membership write.
        vm.ActivePaletteKind = PaletteKind.Style; vm.NewPaletteCharacter = null; vm.NewPaletteName = "WUX9 atomic"; var atomicPalette = vm.CreatePalette();
        vm.SelectedSourceTemplate = sources[0]; vm.LibraryDisplayName = "WUX9 duplicate ref"; var duplicate = vm.RegisterLibrary();
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        await SetAddSource(panel, sources[0]); await SetAddSource(panel, sources[1]);
        var settingsText = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(panel.AddButton);
        Assert(vm.HasError && vm.IsAddingTemplate && vm.AddTemplateSelectedCount == 2 && File.ReadAllText(PlacerSettingsStore.DefaultPath) == settingsText &&
            new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Palettes.Single(x => x.Id == atomicPalette.Id).LibraryEntryIds.Count == 0,
            "WUX9 one ambiguous source rejects the complete batch with zero partial membership writes and preserves the selection");
        await InvokeSelectionButton(panel.CancelButton);
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == duplicate.Id); vm.UnregisterLibrary();

        // A source removed after selection also rejects the whole batch and retains the user's choices.
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        await SetAddSource(panel, sources[2]); await SetAddSource(panel, sources[3]);
        ItemSettings.Default.Templates.Remove(sources[3]); settingsText = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(panel.AddButton);
        Assert(vm.HasError && vm.IsAddingTemplate && vm.AddTemplateSelectedCount == 2 && File.ReadAllText(PlacerSettingsStore.DefaultPath) == settingsText,
            "WUX9 a source removed after selection rejects the entire batch and retains the pending selection");
        ItemSettings.Default.Templates.Add(sources[3]); await InvokeSelectionButton(panel.CancelButton);

        // Linked Character Palette filters another Character before the user has to diagnose a later error.
        var ca = new Character { Name = "WUX9-A" }; var cb = new Character { Name = "WUX9-B" };
        var aFace = Template("WUX9/A-face", [new TachieFaceItem(ca) { Length = 20 }]);
        var bFace = Template("WUX9/B-face", [new TachieFaceItem(cb) { Length = 20 }]);
        ItemSettings.Default.Templates.Add(aFace); ItemSettings.Default.Templates.Add(bFace); vm.Refresh();
        vm.ActivePaletteKind = PaletteKind.Character; vm.NewPaletteName = "WUX9 A";
        vm.NewPaletteCharacter = vm.LibraryCharacters.Single(x => x.Name == ca.Name); var linked = vm.CreatePalette();
        await InvokeSelectionButton(view.PaletteSurface.AddTemplateButton);
        Assert(vm.AddTemplateSources.Contains(aFace) && !vm.AddTemplateSources.Contains(bFace) && vm.AddTemplateSources.All(x =>
            ItemCharacters.Get(x.Items[0]) is not Character c || c.Name == ca.Name),
            "WUX9 linked Character Palette filters incompatible Character Templates before selection");
        await InvokeSelectionButton(panel.CancelButton);

        // Candidate-zero recovery can collect several exact Face Templates for the same Character in one operation.
        ShowTask(view, "expression"); await Idle();
        var row = vm.Rows.Single(x => x.Character == "TestC");
        Assert(!row.HasCandidates, "WUX9 expression bulk fixture starts with no TestC candidate");
        var faceSources = Enumerable.Range(0, 3).Select(i => Template($"WUX9/TestC/{i}", [new TachieFaceItem(row.Target.Voice.Character) { Length = 20 + i, Layer = 190 + i }])).ToArray();
        foreach (var source in faceSources) ItemSettings.Default.Templates.Add(source);
        vm.AddExpressionTemplateCommand.Execute(row); vm.RefreshAddTemplatesCommand.Execute(null); await Idle();
        Assert(faceSources.All(vm.AddTemplateSources.Contains) && vm.AddTemplateSources.All(x => x.Items[0] is TachieFaceItem face && ReferenceEquals(face.Character, row.Target.Voice.Character)),
            "WUX9 expression recovery exposes only exact-Character Face sources and can offer several at once");
        foreach (var source in faceSources) await SetAddSource(panel, source);
        var expressionSettingsBefore = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        await InvokeSelectionButton(panel.AddButton);
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var expressionPalette = saved.Palettes.Single(x => x.CharacterName == "TestC");
        Assert(!vm.HasError && !vm.IsAddingTemplate && row.SelectedChoice.Template == null &&
            faceSources.All(source => row.Choices.Any(x => ReferenceEquals(x.Template?.Template, source))) && expressionPalette.LibraryEntryIds.Count == 3,
            "WUX9 expression recovery batch returns to the row with three usable candidates and never invents an assignment");
        Assert(Signature(timeline) == beforeTimeline && File.ReadAllText(PlacerSettingsStore.DefaultPath) != expressionSettingsBefore,
            "WUX9 expression bulk recovery changes organization only, never Timeline content");

        // Cleanup every WUX9 persistent fixture and restore the prior manual selectors.
        foreach (var paletteId in new[] { firstPalette.Id, secondPalette.Id, atomicPalette.Id, linked.Id, expressionPalette.Id })
        {
            if (!vm.PaletteChoices.Any(x => x.Id == paletteId)) continue;
            vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == paletteId); vm.DeleteCurrentPalette();
        }
        foreach (var entry in vm.LibraryEntries.Where(x => sources.Concat(faceSources).Any(source => x.Entry.Source == TemplateLocator.Capture(source))).ToArray())
        {
            vm.SelectedLibraryEntry = entry; vm.UnregisterLibrary();
        }
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == beforeSettings.ManualStylePaletteId); vm.ActivePaletteKind = beforeSettings.PaletteMode;
        foreach (var source in sources.Concat(faceSources).Concat([aFace, bFace]).ToArray()) ItemSettings.Default.Templates.Remove(source);
        vm.Refresh();
        Assert(Signature(timeline) == beforeTimeline, "WUX9 cleanup preserves the complete pre-existing native Timeline");
        Log("WUX9=PASS");
    }
}
