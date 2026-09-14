using System.IO;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyExpressionRecovery(Timeline timeline)
    {
        stage = "WUX3 management and expression recovery";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var before = Signature(timeline);
        var initial = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        ShowTask(view, "palette"); await Idle();
        Assert(view.MainTabs.Items.Cast<TabItem>().Select(x => x.Header?.ToString()).SequenceEqual(new[] { "パレット", "表情一覧", "選択配置" }),
            "WUX3 primary navigation contains three tasks in Palette-first order, not Template Management");
        await InvokeSelectionButton(view.PaletteSurface.ManageTemplatesButton);
        Assert(vm.IsManagingTemplates && TaskIsVisible(view, "library") && view.BackFromManagementButton.IsVisible,
            "WUX3 Palette secondary action opens actual management with an explicit return path");
        await InvokeSelectionButton(view.BackFromManagementButton);
        Assert(TaskIsVisible(view, "palette"), "WUX3 management Back returns to the same Palette task");
        ShowTask(view, "expression"); await Idle();
        var row = vm.Rows.Single(x => x.Character == "TestC");
        var other = vm.Rows.Single(x => x.Character == "TestA"); var previousOther = other.SelectedChoice.Template;
        other.SelectedChoice = other.Choices.First(x => x.Template != null); var chosenOther = other.SelectedChoice.Template;
        Assert(!row.HasCandidates, "WUX3 missing-expression fixture starts with no matching native Template");
        view.VoiceGrid.ScrollIntoView(row); view.VoiceGrid.UpdateLayout(); await Idle();
        var rowView = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row);
        var recovery = Descendant<Button>(rowView)!;
        Assert(recovery.IsVisible && recovery.IsEnabled && ReferenceEquals(recovery.Command, vm.AddExpressionTemplateCommand) && ReferenceEquals(recovery.CommandParameter, row),
            "WUX3 an actionable per-row recovery is visible and carries the exact Character context");
        SaveNamedView(view, "ux-expression-missing-normal.png");
        await InvokeSelectionButton(recovery);
        Assert(vm.IsAddingTemplate && vm.AddTemplateTitle.Contains("TestC", StringComparison.Ordinal) && vm.AddTemplateSources.Count == 0 &&
            vm.AddTemplateNotice.Contains("YMM4", StringComparison.Ordinal),
            "WUX3 recovery derives the Character and explains the honest empty-source boundary without another tab search");
        var width = view.Width; var height = view.Height;
        try { view.Width = 360; view.Height = 360; await Idle(); SaveNamedView(view, "ux-expression-recovery-empty-narrow.png"); }
        finally { view.Width = width; view.Height = height; await Idle(); }
        // The user creates the required native YMM4 Template outside this add-only plugin, then refreshes this picker.
        var face = new TachieFaceItem(row.Target.Voice.Character) { Length = 20, Layer = 118 };
        var source = Template("UX3/FaceC", [face]); var neutral = Template("UX3/NotAFace", [new TextItem { Length = 20 }]);
        ItemSettings.Default.Templates.Add(source); ItemSettings.Default.Templates.Add(neutral);
        vm.RefreshAddTemplatesCommand.Execute(null); await Idle();
        Assert(vm.AddTemplateSources.Count == 1 && ReferenceEquals(vm.AddTemplateSources[0], source),
            "WUX3 expression recovery offers only singleton Face sources for the exact Character, excluding neutral and other Characters");
        var add = view.TemplateAdditionSurface;
        add.SourceList.SelectedItem = source; add.SourceList.ScrollIntoView(source); await Idle();
        add.NameBox.Text = "ふつう"; add.NameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        await InvokeSelectionButton(add.ManageButton);
        Assert(vm.IsManagingTemplates && vm.IsAddingTemplate && vm.AddTemplateDisplayName == "ふつう" && ReferenceEquals(vm.AddTemplateSource, source),
            "WUX3 a secondary management visit retains the exact source, name and frozen recovery destination");
        await InvokeSelectionButton(view.BackFromManagementButton);
        Assert(add.IsVisible && !view.MainTabs.IsVisible && vm.AddTemplateDisplayName == "ふつう", "WUX3 management Back restores the pending add task, not an unrelated tab");
        await InvokeSelectionButton(add.AddButton);
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var entry = saved.Library.Single(x => x.Source == TemplateLocator.Capture(source));
        var palette = saved.Palettes.Single(x => x.CharacterName == "TestC");
        Assert(!vm.HasError && !vm.IsAddingTemplate && TaskIsVisible(view, "expression") && row.HasCandidates &&
            row.Choices.Any(x => ReferenceEquals(x.Template?.Template, source)) && row.SelectedChoice.Template == null,
            "WUX3 successful recovery returns to the same row with usable candidates and does not invent an assignment");
        Assert(ReferenceEquals(other.SelectedChoice.Template, chosenOther) && palette.LibraryEntryIds.Contains(entry.Id) &&
            saved.ManualCharacterPaletteId == initial.ManualCharacterPaletteId && saved.ManualStylePaletteId == initial.ManualStylePaletteId && saved.PaletteMode == initial.PaletteMode,
            "WUX3 source addition refreshes candidates without losing other assignments or replacing the manual Palette vocabulary");
        Assert(Signature(timeline) == before && saved.NextAssociationId == initial.NextAssociationId,
            "WUX3 recovery organizes references only, without Timeline mutation or association allocation");
        var persisted = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        vm.AddExpressionTemplateCommand.Execute(row); add.SourceList.SelectedItem = source; await Idle();
        await InvokeSelectionButton(add.AddButton);
        Assert(!vm.HasError && File.ReadAllText(PlacerSettingsStore.DefaultPath) == persisted,
            "WUX3 an already-registered member restores candidates without an unnecessary second save");
        var previousName = chosenOther!.Template.Name;
        try
        {
            chosenOther.Template.Name = previousName + "-external-change";
            other.RefreshCandidates(TemplateCatalog.Read(), saved);
            Assert(ReferenceEquals(other.SelectedChoice.Template, chosenOther) && other.SelectedChoice.Template?.Name == previousName,
                "WUX3 candidate refresh never heals the snapshot of an externally renamed assigned source");
        }
        finally { chosenOther.Template.Name = previousName; }
        other.SelectedChoice = other.Choices.First(x => ReferenceEquals(x.Template, previousOther));
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == palette.Id); vm.DeleteCurrentPalette();
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        ItemSettings.Default.Templates.Remove(source); ItemSettings.Default.Templates.Remove(neutral);
        row.RefreshCandidates(TemplateCatalog.Read(), new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load());
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == initial.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == initial.ManualStylePaletteId); vm.ActivePaletteKind = initial.PaletteMode;
        Assert(!row.HasCandidates && Signature(timeline) == before, "WUX3 recovery proof restores its missing-source fixture and leaves the regression Timeline intact");
        ShowTask(view, "expression"); await Idle(); Log("WUX3=PASS");
    }
}
