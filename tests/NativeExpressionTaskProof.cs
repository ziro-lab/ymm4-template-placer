using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static bool WithinView(FrameworkElement control, PlacerView view)
    {
        var at = control.TranslatePoint(new Point(), view);
        return control.IsVisible && at.X >= 0 && at.Y >= 0 && at.X + control.ActualWidth <= view.ActualWidth + 1 && at.Y + control.ActualHeight <= view.ActualHeight + 1;
    }
    private static async Task VerifyExpressionTask(Timeline timeline)
    {
        stage = "WUX4 expression task";
        var vm = ViewModel!; var view = View!; var preset = view.PresetSurface;
        timeline.SelectedItems = []; ShowTask(view, "expression"); await Idle();
        var original = Signature(timeline); var selectedPreset = vm.SelectedExpressionPreset!.Id;
        Assert(view.VoiceGrid.Columns.Count == 4 && view.VoiceGrid.Columns.Select(x => x.Header?.ToString()).SequenceEqual(new[] { "No", "キャラ", "セリフ", "テンプレート" }),
            "WUX4 expression task exposes No, Character, Serif and Template rather than internal timing columns");
        Assert(vm.ExpressionPresets.Count == 1 && !preset.PresetSelector.IsVisible && !preset.PresetEditor.IsExpanded &&
            preset.PresetEditor.Header.ToString()!.Contains("音声と同じ", StringComparison.Ordinal),
            "WUX4 first use shows the actual default placement range without requiring a Preset selection");
        Assert(preset.PresetSelector.GetBindingExpression(ComboBox.SelectedItemProperty)?.Status == BindingStatus.Active,
            "WUX4 the hidden single-preset selector retains its saved current ID binding");
        vm.CopyExpressionPreset(); await Idle();
        Assert(preset.PresetSelector.IsVisible && preset.PresetSelector.SelectedItem is ExpressionPreset copied && copied.Id == vm.SelectedExpressionPreset!.Id,
            "WUX4 multiple saved conditions reveal a usable selector without changing current-preset semantics");
        vm.ExpressionDraft.StartOffset = "-1";
        Assert(!vm.PlaceCommand.CanExecute(null) && !vm.ResyncCommand.CanExecute(null) && vm.ExpressionPresetDirty,
            "WUX4 progressive disclosure never bypasses the saved-draft guard for placement or resync");
        vm.RevertExpressionPresetCommand.Execute(null); vm.DeleteExpressionPreset();
        vm.SelectedExpressionPreset = vm.ExpressionPresets.Single(x => x.Id == selectedPreset); await Idle();
        Assert(!preset.PresetSelector.IsVisible, "WUX4 returning to one saved range removes an unnecessary selection decision");
        Assert(view.ResyncButton.IsVisible && !view.ResyncButton.IsEnabled && vm.ResyncHint.Contains("タイムライン", StringComparison.Ordinal) && ToolTipService.GetShowOnDisabled(view.ResyncButton),
            "WUX4 expression-scoped Resync explains disabled selection context without occupying every task header");
        var linked = timeline.Items.OfType<VoiceItem>().First(x => AssociationTag.Voice(x.Remark, out _) == AssociationTagState.Valid);
        timeline.SelectedItems = [linked]; await Idle();
        Assert(view.ResyncButton.IsEnabled, "WUX4 a real selected associated Voice enables the same existing Resync command");
        timeline.SelectedItems = [timeline.Items.OfType<TachieItem>().First()]; await Idle();
        Assert(!view.ResyncButton.IsEnabled, "WUX4 an unrelated native item does not advertise Expression Resync");
        ShowTask(view, "palette"); await Idle(); Assert(!view.ResyncButton.IsVisible, "WUX4 Palette has no global Expression recovery action");
        ShowTask(view, "selection"); await Idle(); Assert(!view.ResyncButton.IsVisible, "WUX4 Selection Placement has no global Expression recovery action");
        ShowTask(view, "expression"); timeline.SelectedItems = []; await Idle();
        var row = vm.Rows.Single(x => x.Character == "TestA"); var oldChoice = row.SelectedChoice;
        view.VoiceGrid.SelectedItem = row; view.VoiceGrid.ScrollIntoView(row); await Idle();
        var alias = row.Choices.First(x => x.Label.Contains("（パレット）", StringComparison.Ordinal));
        row.SelectedChoice = alias; await Idle();
        Assert(alias.DisplayName == "どや" && ReferenceEquals(alias.Template, row.SelectedChoice.Template),
            "WUX4 the compact alias hides the Palette badge without changing the strict selected source");
        SaveNamedView(view, "ux-expression-normal.png");
        var width = view.Width; var height = view.Height;
        try
        {
            view.Width = 360; view.Height = 360; await Idle(); view.VoiceGrid.UpdateLayout();
            var container = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row);
            var combo = Descendant<ComboBox>(container)!;
            Assert(view.VoiceGrid.Columns.Sum(x => x.ActualWidth) <= view.VoiceGrid.ActualWidth + 1 &&
                combo.ActualWidth >= 104 && WithinView(combo, view),
                "WUX4 360px keeps the entire Template selector inside the view instead of requiring horizontal exploration");
            Assert(view.ExpressionRowDetail.IsVisible && view.ExpressionSelectedSerif.Text == row.Serif && WithinView(view.ExpressionRowDetail, view),
                "WUX4 narrow task provides the selected native Serif at full available width");
            Assert(WithinView(view.PlaceButton, view) && WithinView(view.ResyncButton, view),
                "WUX4 primary placement and scoped recovery remain reachable without scrolling the whole task");
            SaveNamedView(view, "ux-expression-narrow.png");
            var missing = vm.Rows.Single(x => x.Character == "TestC");
            view.VoiceGrid.SelectedItem = missing; view.VoiceGrid.ScrollIntoView(missing); await Idle();
            container = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(missing);
            Assert(WithinView(Descendant<Button>(container)!, view) && view.ExpressionSelectedSerif.Text == missing.Serif,
                "WUX4 missing-candidate recovery remains reachable next to a readable full-width Serif at 360px");
            SaveNamedView(view, "ux-expression-missing-narrow.png");
        }
        finally { view.Width = width; view.Height = height; row.SelectedChoice = oldChoice; view.VoiceGrid.SelectedItem = null; await Idle(); }
        Assert(Signature(timeline) == original, "WUX4 all disclosure, selector and layout checks preserve native Timeline content");
        Log("WUX4=PASS");
    }
}
