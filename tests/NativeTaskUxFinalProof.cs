using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static bool InTaskViewport(FrameworkElement control, PlacerView view)
    {
        if (!WithinView(control, view) || control.ActualWidth < 1 || control.ActualHeight < 1) return false;
        var bounds = new Rect(control.TranslatePoint(new Point(), view), control.RenderSize);
        for (DependencyObject? parent = VisualTreeHelper.GetParent(control); parent != null && !ReferenceEquals(parent, view); parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is not ScrollContentPresenter clip) continue;
            var visible = new Rect(clip.TranslatePoint(new Point(), view), clip.RenderSize);
            visible.Inflate(2, 2);
            if (!visible.Contains(bounds)) return false;
        }
        return true;
    }
    private static async Task VerifyTaskUxFinal(Timeline timeline, UndoRedoManager undo)
    {
        stage = "WUX7 final task review";
        var vm = ViewModel!; var view = View!; var selection = view.SelectionSurface; var palette = view.PaletteSurface;
        timeline.SelectedItems = []; vm.Refresh();
        var before = Signature(timeline); var settingsBefore = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var width = view.Width; var height = view.Height;
        var template = Template("UX7/字幕の枠", [new TextItem { Length = 20, Layer = 145 }]);
        var otherTemplate = Template("UX7/強調の枠", [new TextItem { Length = 15, Layer = 146 }]);
        ItemSettings.Default.Templates.Add(template); ItemSettings.Default.Templates.Add(otherTemplate); vm.Refresh();
        ShowTask(view, "palette"); await Idle();
        await InvokeSelectionButton(palette.NewPaletteButton);
        palette.NewPaletteNameBox.Text = "よく使う演出"; palette.NewPaletteNameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        view.Width = 360; view.Height = 320; await Idle();
        Assert(InTaskViewport(palette.CreatePaletteButton, view) && InTaskViewport(palette.NewCharacterCombo, view),
            "WUX7 360px creation asks only name/optional Character and keeps its completion action in the viewport");
        SaveNamedView(view, "ux-final-create-narrow.png");
        await InvokeSelectionButton(palette.CreatePaletteButton); var paletteId = vm.CurrentPalette!.Id;
        async Task<LibraryEntry> AddSource(ItemTemplate source)
        {
            await InvokeSelectionButton(palette.AddTemplateButton);
            await SetAddSource(view.TemplateAdditionSurface, source);
            view.TemplateAdditionSurface.NameBox.Text = "枠";
            view.TemplateAdditionSurface.NameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Assert(InTaskViewport(view.TemplateAdditionSurface.AddButton, view), "WUX7 360px direct-add completion does not require scrolling the whole task");
            await InvokeSelectionButton(view.TemplateAdditionSurface.AddButton);
            return vm.SelectedPaletteEntry!.Entry!;
        }
        var entry = await AddSource(template); var otherEntry = await AddSource(otherTemplate);
        ShowTask(view, "expression"); ShowTask(view, "palette"); await Idle();
        Assert(InTaskViewport(palette.AddTemplateButton, view) && InTaskViewport(palette.DropSurface.DropButton, view),
            "WUX7 narrow Palette retains direct addition and Quick Drop in the visible task");
        Assert(vm.PaletteEntries.Select(x => x.SourceDetail).ToHashSet(StringComparer.Ordinal)
            .SetEquals(new[] { template.Name, otherTemplate.Name }),
            "WUX7 duplicate Palette aliases expose distinct source labels without adding normal ready status");
        SaveNamedView(view, "ux-final-palette-narrow.png");
        view.Width = width; view.Height = height; await Idle(); SaveNamedView(view, "ux-final-palette-normal.png");
        view.Width = 360; view.Height = 320;
        palette.PaletteEditor.IsExpanded = true; palette.DropSurface.LayerEditor.IsExpanded = true; await Idle();
        Assert(!palette.PaletteEditor.IsExpanded && InTaskViewport(palette.DropSurface.DropButton, view),
            "WUX7 secondary Palette editors do not stack above the primary placement task");
        var oldMinimum = vm.PaletteMinimumText; vm.PaletteMinimumText = "bad"; await Idle();
        Assert(!palette.DropSurface.DropButton.IsEnabled && ToolTipService.GetShowOnDisabled(palette.DropSurface.DropButton) && vm.QuickDropHint.Contains("保存", StringComparison.Ordinal),
            "WUX7 an unsaved Palette setting has a reachable disabled-action explanation");
        palette.PaletteEditor.IsExpanded = true; await Idle();
        Assert(!palette.DropSurface.LayerEditor.IsExpanded && vm.PaletteMinimumText == "bad" && vm.PaletteLayerDirty,
            "WUX7 changing disclosure does not discard an unfinished Layer draft");
        vm.PaletteMinimumText = oldMinimum; palette.PaletteEditor.IsExpanded = false; await Idle();
        vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        ItemSettings.Default.Templates.Remove(template); vm.RefreshLibraryCommand.Execute(null); await Idle();
        Assert(vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id).Status.Contains("見つかりません", StringComparison.Ordinal),
            "WUX7 the narrow Palette shows an actionable missing-reference warning, not a ready label");
        await InvokeSelectionButton(palette.DropSurface.DropButton);
        Assert(vm.HasError && Signature(timeline) == before && vm.CurrentPalette!.Id == paletteId,
            "WUX7 attempted broken-source Quick Drop leaves Timeline and selected Palette intact");
        SaveNamedView(view, "ux-final-broken-narrow.png");
        ItemSettings.Default.Templates.Add(template); vm.Refresh();
        ShowTask(view, "expression"); await Idle();
        var row = vm.Rows.First(x => x.HasCandidates); view.VoiceGrid.SelectedItem = row; view.VoiceGrid.ScrollIntoView(row); await Idle();
        var rowView = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row);
        Assert(InTaskViewport(Descendant<ComboBox>(rowView)!, view) && InTaskViewport(view.PlaceButton, view) && view.VoiceGrid.Columns.Count == 4,
            "WUX7 360px expression Template input and primary action are inside actual scroll viewports");
        SaveNamedView(view, "ux-final-expression-narrow.png");
        view.Width = width; view.Height = height; await Idle(); SaveNamedView(view, "ux-final-expression-normal.png");
        view.Width = 360; view.Height = 320;
        view.ExcelEditor.IsExpanded = true; view.PresetSurface.PresetEditor.IsExpanded = true; await Idle();
        Assert(!view.ExcelEditor.IsExpanded && InTaskViewport(view.PlaceButton, view), "WUX7 expression condition editor does not stack with Excel or push Place offscreen");
        view.Width = 640; view.Height = 640; await Idle();
        Assert(((ScrollViewer)view.PresetSurface.PresetEditor.Content).MaxHeight > 90 && InTaskViewport(view.PlaceButton, view),
            "WUX7 a taller Tool offers more editor space without pushing the primary action outside its viewport");
        SaveNamedView(view, "ux-final-expression-editor-tall.png");
        view.Width = 360; view.Height = 320; await Idle();
        view.PresetSurface.PresetEditor.IsExpanded = false;
        var targets = timeline.Items.OfType<VoiceItem>().Take(3).Cast<IItem>().ToArray();
        for (var n = 1; n <= 3; n++)
        {
            timeline.SelectedItems = targets.Take(n).ToImmutableList(); ShowTask(view, "selection");
            vm.SelectedSelectionProfile = vm.SelectionProfiles.First(x => x.Value == (n == 1 ? SelectionProfile.TargetCompanion : SelectionProfile.SelectionRange));
            selection.TemplateSelector.SelectedItem = vm.SelectionTemplates.Single(x => x.Id == entry.Id); await Idle();
            Descendant<ScrollViewer>(selection)?.ScrollToTop(); await Idle();
            Assert(selection.ProfileSelector.Items.Count == (n == 3 ? 1 : 2) && InTaskViewport(selection.TemplateSelector, view) &&
                InTaskViewport(selection.ProfileSelector, view) && InTaskViewport(selection.PlaceSelectionButton, view),
                $"WUX7 {n}-item narrow task displays only valid purposes with Template and Place visible");
            Assert(vm.SelectionPreview.StartsWith("配置予定:", StringComparison.Ordinal), $"WUX7 {n}-item task has an automatic current preview");
            SaveNamedView(view, $"ux-final-selection-{n}-narrow.png");
        }
        selection.TemplateSelector.IsDropDownOpen = true; await Idle();
        var actualChoice = (ComboBoxItem)selection.TemplateSelector.ItemContainerGenerator.ContainerFromItem(vm.SelectionTemplates.Single(x => x.Id == otherEntry.Id));
        Assert(actualChoice != null && Descendant<TextBlock>(actualChoice) != null &&
            vm.SelectionTemplates.Count(x => x.DisplayName == "枠") == 2,
            "WUX7 both identically named Templates remain distinct actual choices; source/Character detail is shown instead of guessing");
        var labelPanel = (StackPanel)selection.TemplateSelector.ItemTemplate.LoadContent();
        labelPanel.DataContext = vm.SelectionTemplates.Single(x => x.Id == otherEntry.Id);
        var detail = labelPanel.Children.OfType<TextBlock>().Single(x => x.Inlines.Count == 3);
        var runs = detail.Inlines.OfType<System.Windows.Documents.Run>().Where(x => x.GetBindingExpression(System.Windows.Documents.Run.TextProperty) != null).ToArray();
        Assert(runs.Length == 2 && runs.All(x => x.GetBindingExpression(System.Windows.Documents.Run.TextProperty)!.ParentBinding.Mode == System.Windows.Data.BindingMode.OneWay),
            "WUX7 source labels bind read-only values one-way instead of requesting an invalid write-back");
        selection.TemplateSelector.IsDropDownOpen = false;
        view.Width = width; view.Height = height; await Idle(); SaveNamedView(view, "ux-final-selection-normal.png");
        // The normal Tool lifecycle also gates the automatic preview, not just the first-use expression path.
        var root = Application.Current.Windows.Cast<Window>().Select(x => x.DataContext)
            .First(x => x?.GetType().FullName == "YukkuriMovieMaker.ViewModels.MainViewModel")!;
        var previousViewModel = vm; var count = vm.AutomaticPreviewCount;
        await SetToolVisible(root, false); timeline.SelectedItems = []; timeline.SelectedItems = targets.Take(2).ToImmutableList(); await Idle();
        Assert(vm.AutomaticPreviewCount == count && Signature(timeline) == before, "WUX7 native Tool close cancels invisible preview work without modifying Timeline");
        await SetToolVisible(root, true); await Idle();
        Log($"WUX7 reopen diagnostic: sameView={ReferenceEquals(View, view)}; sameVM={ReferenceEquals(ViewModel, vm)}; oldLoaded={view.IsLoaded}; newLoaded={View?.IsLoaded}; oldVisible={view.IsVisible}; newVisible={View?.IsVisible}; oldSelection={view.SelectionTab.IsSelected}; newSelection={View?.SelectionTab.IsSelected}; countBefore={count}; countAfter={vm.AutomaticPreviewCount}; newCount={ViewModel?.AutomaticPreviewCount}; oldTemplate={vm.SelectionTemplate?.Id}; newTemplate={ViewModel?.SelectionTemplate?.Id}; preview={vm.SelectionPreview}; unchanged={Signature(timeline) == before}");
        if (View?.IsLoaded == true) SaveNamedView(View, "ux-final-reopen-diagnostic.png");
        vm = ViewModel ?? throw new InvalidOperationException("Reopened native ViewModel missing.");
        Assert(view.IsLoaded && view.IsVisible && ReferenceEquals(view.DataContext, vm) && vm.SelectionTemplate?.Id == entry.Id &&
            vm.AutomaticPreviewCount > (ReferenceEquals(vm, previousViewModel) ? count : 0) &&
            vm.SelectionPreview.StartsWith("配置予定:", StringComparison.Ordinal) && Signature(timeline) == before,
            "WUX7 actual Tool reopen resumes live preview with the preserved Template and no Timeline mutation");
        Assert(ReferenceEquals(vm, previousViewModel) || previousViewModel.AutomaticPreviewCount == count,
            "WUX7 a replaced native ViewModel does not resume background preview work");
        ShowTask(view, "library"); await Idle();
        view.LibrarySurface.SourceCombo.SelectedItem = vm.SourceTemplates.First(x => x.Items.Count == 1 && x.Items[0] is TachieFaceItem);
        view.LibrarySurface.CharacterSummary.BringIntoView(); await Idle();
        Assert(InTaskViewport(view.LibrarySurface.CharacterSummary, view), "WUX7 automatic Character summary is actually visible in management evidence");
        SaveNamedView(view, "ux-final-character-auto.png");
        timeline.SelectedItems = []; ShowTask(view, "palette");
        vm.SelectedPalette = vm.PaletteChoices.Single(x => x.Id == paletteId); vm.DeleteCurrentPalette();
        foreach (var id in new[] { entry.Id, otherEntry.Id }) { vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == id); vm.UnregisterLibrary(); }
        vm.ManualCharacterPalette = vm.CharacterPalettes.FirstOrDefault(x => x.Id == settingsBefore.ManualCharacterPaletteId);
        vm.ManualStylePalette = vm.StylePalettes.FirstOrDefault(x => x.Id == settingsBefore.ManualStylePaletteId); vm.ActivePaletteKind = settingsBefore.PaletteMode;
        timeline.SelectedItems = [targets[0]]; vm.SelectedSelectionProfile = vm.SelectionProfiles.First(x => x.Value == SelectionProfile.TargetCompanion);
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == settingsBefore.CurrentSelectionPresetId);
        timeline.SelectedItems = []; ItemSettings.Default.Templates.Remove(template); ItemSettings.Default.Templates.Remove(otherTemplate);
        view.Width = width; view.Height = height; view.VoiceGrid.SelectedItem = null; vm.Refresh(); ShowTask(view, "expression"); await Idle();
        Assert(Signature(timeline) == before && new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId == settingsBefore.NextAssociationId,
            "WUX7 final UI review leaves original Items and association allocation unchanged");
        Log("WUX7=PASS");
    }
    private static void VerifyTaskUxAcceptance()
    {
        var lines = File.ReadAllLines(Path.Combine(output, "proof-log.txt"));
        foreach (var required in Enumerable.Range(1, 7).Select(x => $"WUX{x}=PASS").Append("V04=PASS"))
            Assert(lines.Contains(required, StringComparer.Ordinal), "UX acceptance requires real native stage " + required);
        Assert(!nativeFaultOccurred, "UX acceptance rejects any captured unhandled native fault");
        var requirements = new[]
        {
            "Direct Palette add, exact reference reuse and zero-write ambiguity rejection: WUX1",
            "Management is a secondary task with draft-preserving Back: WUX3",
            "Unified Palette picker; optional Character derives stored kind: WUX2",
            "Missing expression candidate opens exact-Character recovery: WUX3/WUX4",
            "Normal status hidden; broken/ambiguous/mismatch actionable: WUX2/WUX7",
            "Resync scoped to Expression and actual associated Timeline selection: WUX4",
            "Four-column expression task, readable Serif/Template at 360px: WUX4/WUX7",
            "Default range without mandatory Preset concept; custom selector retained: WUX4",
            "What/where selection, only valid cardinality purposes: WUX5/WUX7",
            "Event-coalesced preview, native measured cost and fresh-plan Place: WUX6",
            "Success clears on navigation; errors/partial results and drafts retained: WUX6",
            "Automatic Character with advanced override; native close/reopen and final viewport checks: WUX6/WUX7"
        };
        File.WriteAllText(Path.Combine(output, "ux-acceptance.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Task-UX/1", result = "PASS", version = "0.4.0",
            checks = requirements.Select((requirement, i) => new { id = i + 1, requirement, result = "PASS" }),
            optional_bulk_assignment = "Not implemented; Excel Bridge retained",
            boundary = "Real YMM4 4.55.1.1 synthetic fixtures and actual WPF bindings/commands; no physical-pointer, arbitrary user-assets or all-DPI claim. Screenshot files require a separate visual review."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("UX_ACCEPTANCE=PASS");
    }
}
