using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task ChoosePurpose(SelectionPanel panel, SelectionProfile profile)
    {
        var choice = panel.ProfileSelector.Items.Cast<SelectionProfileChoice>().Single(x => x.Value == profile);
        panel.ProfileSelector.ScrollIntoView(choice); panel.ProfileSelector.UpdateLayout(); await Idle();
        var container = (ListBoxItem)panel.ProfileSelector.ItemContainerGenerator.ContainerFromItem(choice);
        var radio = Descendant<RadioButton>(container)!;
        Assert(radio.IsVisible && radio.IsEnabled, "WUX5 valid placement purpose is directly visible and actionable");
        ((ISelectionItemProvider)new RadioButtonAutomationPeer(radio).GetPattern(PatternInterface.SelectionItem)).Select();
        await Idle();
        Assert(radio.IsChecked == true && (panel.ProfileSelector.SelectedItem as SelectionProfileChoice)?.Value == profile,
            "WUX5 selecting the actual purpose control updates the single selected placement method");
    }
    private static async Task VerifySelectionTask(Timeline timeline, UndoRedoManager undo)
    {
        stage = "WUX5 Selection task";
        var vm = ViewModel!; var view = View!; var panel = view.SelectionSurface;
        timeline.SelectedItems = []; var original = Signature(timeline);
        var initial = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var a = new TextItem { Frame = 8000, Length = 20, Layer = 1, Remark = "UX5 first" };
        var b = new TextItem { Frame = 8020, Length = 20, Layer = 2, Remark = "UX5 second" };
        var c = new TextItem { Frame = 8100, Length = 10, Layer = 3, Remark = "UX5 third" };
        var source = new TextItem { Length = 10, Layer = 124, Remark = "UX5 source" };
        var template = Template("UX5/Frame", [source]); ItemSettings.Default.Templates.Add(template);
        undo.Record();
        foreach (var item in new IItem[] { a, b, c }) Assert(timeline.TryAddItems([item], item.Frame, item.Layer), "WUX5 native task fixture insertion");
        undo.Record(); vm.Refresh(); vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "ふちどり";
        var entry = vm.RegisterLibrary(); timeline.SelectedItems = [a]; ShowTask(view, "selection"); await Idle();
        panel.TemplateSelector.SelectedItem = vm.SelectionTemplates.Single(x => x.Id == entry.Id); await Idle();
        Assert(vm.SelectionTemplate?.Id == entry.Id && panel.ProfileSelector.Items.Cast<SelectionProfileChoice>().Select(x => x.Value)
            .SequenceEqual(new[] { SelectionProfile.TargetCompanion, SelectionProfile.PointEmphasis }),
            "WUX5 actual what-to-place selector precedes exactly the two valid one-item purposes");
        await ChoosePurpose(panel, SelectionProfile.TargetCompanion);
        var stable = Signature(timeline); var before = timeline.Items;
        await InvokeSelectionButton(panel.PlaceSelectionButton);
        var placedItem = timeline.Items.Except(before).Single();
        Assert(!vm.HasError && placedItem.Frame == 8000 && placedItem.Length == 20 && placedItem.Layer == 124 && source.Length == 10,
            "WUX5 actual Template -> purpose -> Place works without a mandatory manual Preview action");
        var placed = Signature(timeline); await undo.UndoAsync(); await Idle();
        Assert(Signature(timeline) == stable, "WUX5 direct task placement retains one-step native Undo");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "WUX5 direct task placement retains one-step native Redo");
        await undo.UndoAsync(); await Idle(); timeline.SelectedItems = [a];
        await ChoosePurpose(panel, SelectionProfile.PointEmphasis);
        Assert(vm.SelectedSelectionPreset?.Profile == SelectionProfile.PointEmphasis, "WUX5 the alternate one-item purpose uses its existing saved preset");
        var bytes = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        timeline.SelectedItems = [b, a]; await Idle();
        Assert(panel.ProfileSelector.Items.Cast<SelectionProfileChoice>().Select(x => x.Value).SequenceEqual(new[] { SelectionProfile.SelectionRange, SelectionProfile.Boundary }) &&
            File.ReadAllText(PlacerSettingsStore.DefaultPath) == bytes && !panel.PlaceSelectionButton.IsEnabled,
            "WUX5 two-item context removes single-item methods without silently rewriting the current saved preset");
        await ChoosePurpose(panel, SelectionProfile.SelectionRange); await ChoosePurpose(panel, SelectionProfile.Boundary);
        Assert(vm.SelectedSelectionPreset?.Profile == SelectionProfile.Boundary, "WUX5 the explicit two-item boundary action selects the existing Boundary family");
        timeline.SelectedItems = [c, b, a]; await Idle();
        Assert(panel.ProfileSelector.Items.Count == 1 && ((SelectionProfileChoice)panel.ProfileSelector.Items[0]).Value == SelectionProfile.SelectionRange,
            "WUX5 three items expose only the valid range purpose, never an unusable boundary decision");
        await ChoosePurpose(panel, SelectionProfile.SelectionRange);
        Assert(panel.PlaceSelectionButton.IsEnabled && vm.SelectedSelectionPreset?.Profile == SelectionProfile.SelectionRange,
            "WUX5 one explicit range choice makes three-item placement ready using saved conditions");
        vm.SelectionDraft.HeadPadding = "1"; await Idle();
        Assert(vm.SelectionPresetDirty && !panel.ProfileSelector.IsEnabled && !panel.SelectionPresetSelector.IsEnabled &&
            !panel.PlaceSelectionButton.IsEnabled && vm.SelectionPresetNotice.Length > 0,
            "WUX5 dirty conditions cannot be replaced through a misleading purpose or saved-condition selector");
        vm.RevertSelectionPresetCommand.Execute(null); await Idle();
        Assert(panel.ProfileSelector.IsEnabled && panel.PlaceSelectionButton.IsEnabled, "WUX5 reverting the draft restores the task without losing its Template");
        SaveNamedView(view, "ux-selection-task-normal.png");
        var width = view.Width; var height = view.Height;
        try
        {
            view.Width = 360; view.Height = 360; await Idle();
            Descendant<ScrollViewer>(panel)?.ScrollToTop(); await Idle();
            Assert(WithinView(panel.TemplateSelector, view) && WithinView(panel.ProfileSelector, view) && WithinView(panel.PlaceSelectionButton, view),
                "WUX5 360px exposes what, valid where, and Place without whole-task navigation");
            SaveNamedView(view, "ux-selection-task-narrow.png");
        }
        finally { view.Width = width; view.Height = height; await Idle(); }
        timeline.SelectedItems = []; await Idle();
        Assert(panel.ProfileSelector.Items.Count == 0 && !panel.PlaceSelectionButton.IsEnabled && vm.SelectionContext.Contains("タイムライン", StringComparison.Ordinal),
            "WUX5 no selection provides a concrete Timeline action instead of meaningless placement choices");
        timeline.SelectedItems = [a];
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.TargetCompanion);
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == initial.CurrentSelectionPresetId);
        timeline.SelectedItems = []; vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        undo.Record(); timeline.Items = timeline.Items.RemoveAll(x => ReferenceEquals(x, a) || ReferenceEquals(x, b) || ReferenceEquals(x, c));
        timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); ItemSettings.Default.Templates.Remove(template); vm.Refresh(); await Idle();
        Assert(Signature(timeline) == original, "WUX5 fixture cleanup preserves the entire pre-existing native Timeline");
        Log("WUX5=PASS");
    }
}
