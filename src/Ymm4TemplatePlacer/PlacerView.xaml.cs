using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;

public partial class PlacerView : UserControl
{
    private PlacerViewModel? observedViewModel;
    internal TimelinePointerInputRouter PointerRouter { get; }
    internal PaletteShortcutInputRouter ShortcutRouter { get; }
    private TransientWorkSnapshot? suspendedWork;
    private DataGridRow? expressionRowHeightResizeRow;
    private double expressionRowHeightDragStart, expressionRowHeightPreview, expressionRowHeightPointerStart;
    public IntentPalettePanel RelativePaletteSurface { get; } = new();
    public IntentSettingsPanel RelativeSettingsSurface { get; } = new();
    public PlacerView()
    {
        PointerRouter = new(origin => observedViewModel?.ObserveTimelinePointer(origin), () => observedViewModel?.EndTimelinePointer());
        ShortcutRouter = new((key, modifiers) => observedViewModel?.TryExecutePositionShortcut(key, modifiers) == true);
        InitializeComponent();
        var choiceStyle = new Style(typeof(ComboBoxItem));
        choiceStyle.Setters.Add(new Setter(IsEnabledProperty, new Binding(nameof(TemplateChoice.IsAvailable))));
        VoiceGrid.Resources[typeof(ComboBoxItem)] = choiceStyle;
        SizeChanged += (_, _) => RefreshExpressionDetail();
        PresetSurface.PresetEditor.Expanded += (_, _) => ExcelEditor.IsExpanded = false;
        ExcelEditor.Expanded += (_, _) => PresetSurface.PresetEditor.IsExpanded = false;
        VoiceGrid.SelectionChanged += (_, e) =>
        {
            // Selector.SelectionChanged bubbles: a nested expression ComboBox
            // changing its choice is NOT a different Voice-row selection.
            if (!ReferenceEquals(e.OriginalSource, VoiceGrid)) return;
            RefreshExpressionDetail();
            observedViewModel?.SetExpressionRowContext(VoiceGrid.SelectedItem as AssignmentRow);
        };
        MainTabs.SelectionChanged += (_, e) => { if (ReferenceEquals(e.OriginalSource, MainTabs)) SynchronizeTask(); };
        PreviewKeyDown += (_, e) =>
        {
            if ((e.Key == Key.Z || e.Key == Key.Y) && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                observedViewModel?.CloseExpressionTrialSession();
        };
        IsKeyboardFocusWithinChanged += (_, e) => { if (e.NewValue is false) observedViewModel?.CloseExpressionTrialSession(); };
        DataContextChanged += ChangeViewModel;
        Loaded += (_, _) => { ObserveViewModel(DataContext as PlacerViewModel); SynchronizeTask(); };
        Unloaded += (_, _) => ObserveViewModel(null);
        IsVisibleChanged += (_, _) => SynchronizeTask();
#if YMM4_PROOF
        NativeProof.View = this;
#endif
    }
    private const double ExpressionRowResizeBand = 4d;

    private static bool IsExpressionRowResizeHit(DataGridRow row, MouseEventArgs e)
    {
        var point = e.GetPosition(row);
        return row.ActualHeight > 0 && point.Y >= Math.Max(0, row.ActualHeight - ExpressionRowResizeBand) &&
            point.Y <= row.ActualHeight + 1;
    }

    private void VoiceGridRow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DataGridRow row) return;
        if (expressionRowHeightResizeRow is { } active)
        {
            if (!ReferenceEquals(active, row)) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                CancelExpressionRowHeightResize();
                return;
            }
            expressionRowHeightPreview = Math.Clamp(expressionRowHeightDragStart +
                (e.GetPosition(VoiceGrid).Y - expressionRowHeightPointerStart), 32, 96);
            PreviewExpressionRowHeight((int)Math.Round(expressionRowHeightPreview));
            e.Handled = true;
            return;
        }
        row.Cursor = IsExpressionRowResizeHit(row, e) ? Cursors.SizeNS : null;
    }

    private void VoiceGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || observedViewModel is not { CanEditExpressionRowHeight: true } vm ||
            !IsExpressionRowResizeHit(row, e)) return;
        if (!row.CaptureMouse()) return;
        expressionRowHeightResizeRow = row;
        expressionRowHeightDragStart = vm.ExpressionRowHeight;
        expressionRowHeightPreview = expressionRowHeightDragStart;
        expressionRowHeightPointerStart = e.GetPosition(VoiceGrid).Y;
        row.Cursor = Cursors.SizeNS;
        e.Handled = true;
    }

    private void VoiceGridRow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || !ReferenceEquals(expressionRowHeightResizeRow, row)) return;
        var value = (int)Math.Round(expressionRowHeightPreview);
        expressionRowHeightResizeRow = null;
        if (row.IsMouseCaptured) row.ReleaseMouseCapture();
        if (observedViewModel is { CanEditExpressionRowHeight: true } vm) vm.ExpressionRowHeight = value;
        RestoreExpressionRowHeightVisual();
        row.Cursor = null;
        e.Handled = true;
    }

    private void VoiceGridRow_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is DataGridRow row && ReferenceEquals(expressionRowHeightResizeRow, row))
            CancelExpressionRowHeightResize();
    }

    private void VoiceGridRow_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is DataGridRow row && !ReferenceEquals(expressionRowHeightResizeRow, row)) row.Cursor = null;
    }

    private void PreviewExpressionRowHeight(int value)
    {
        value = Math.Clamp(value, 32, 96);
        VoiceGrid.SetCurrentValue(DataGrid.RowHeightProperty, (double)value);
        ExpressionRowHeightBox.SetCurrentValue(TextBox.TextProperty,
            value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private void RestoreExpressionRowHeightVisual()
    {
        if (observedViewModel is not { } vm) return;
        PreviewExpressionRowHeight(vm.ExpressionRowHeight);
        ExpressionRowHeightBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
    }

    private void CancelExpressionRowHeightResize()
    {
        var row = expressionRowHeightResizeRow;
        expressionRowHeightResizeRow = null;
        if (row?.IsMouseCaptured == true) row.ReleaseMouseCapture();
        if (row != null) row.Cursor = null;
        RestoreExpressionRowHeightVisual();
    }

    private void ExpressionRowHeightBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RestoreExpressionRowHeightVisual();
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter) return;
        CommitExpressionRowHeightBox();
        e.Handled = true;
    }

    private void ExpressionRowHeightBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (expressionRowHeightResizeRow == null) CommitExpressionRowHeightBox();
    }

    private void CommitExpressionRowHeightBox()
    {
        if (observedViewModel is not { } vm) return;
        if (!vm.TrySetExpressionRowHeight(ExpressionRowHeightBox.Text))
        {
            RestoreExpressionRowHeightVisual();
            return;
        }
        RestoreExpressionRowHeightVisual();
    }

    private void VoiceGridRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow { DataContext: AssignmentRow row } || observedViewModel == null || IsInteractiveExpressionSource(e.OriginalSource as DependencyObject, sender as DataGridRow)) return;
        if (!observedViewModel.NavigateExpressionRowCommand.CanExecute(row)) return;
        observedViewModel.NavigateExpressionRowCommand.Execute(row);
        e.Handled = true;
    }
    private static bool IsInteractiveExpressionSource(DependencyObject? source, DataGridRow? row)
    {
        for (var current = source; current != null && !ReferenceEquals(current, row); current = VisualTreeHelper.GetParent(current))
            if (current is ComboBox or ButtonBase) return true;
        return false;
    }
    private void ChangeViewModel(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PlacerViewModel previous) suspendedWork = previous.TakeTransientWork();
        var next = e.NewValue as PlacerViewModel;
        ObserveViewModel(IsLoaded ? next : null);
        if (next != null && suspendedWork != null) { next.AcceptTransientWork(suspendedWork); suspendedWork = null; }
        SynchronizeTask();
    }
    private void ObserveViewModel(PlacerViewModel? next)
    {
        if (ReferenceEquals(observedViewModel, next)) return;
        PointerRouter.Detach(); ShortcutRouter.Detach();
        if (observedViewModel != null)
        {
            observedViewModel.IntentWorkspaceDeactivated -= WorkspaceDeactivated;
            observedViewModel.PropertyChanged -= ViewModelChanged; observedViewModel.IntentSettingsRequested -= OpenIntentSettings;
            observedViewModel.DeactivateIntentWorkspace(); observedViewModel.SetActiveTask("");
        }
        observedViewModel = next;
        if (next != null)
        {
            next.IntentWorkspaceDeactivated += WorkspaceDeactivated;
            next.PropertyChanged += ViewModelChanged; next.IntentSettingsRequested += OpenIntentSettings;
            next.ActivateIntentWorkspace(); next.AttachRelativeExpressionBindings(); RefreshWorkspaceSurface();
        }
    }
    private void WorkspaceDeactivated(object? sender, EventArgs e) { PointerRouter.Detach(); ShortcutRouter.Detach(); }
    private void OpenIntentSettings(object? sender, EventArgs e) { observedViewModel?.BeginIntentSettings(); SelectionTab.IsSelected = true; }
    private void RefreshWorkspaceSurface()
    {
        var legacy = observedViewModel?.UseLegacyWorkspace == true;
        PaletteTab.Content = legacy ? PaletteSurface : RelativePaletteSurface;
        SelectionTab.Content = legacy ? SelectionSurface : RelativeSettingsSurface;
        PaletteTab.Header = legacy ? "パレット" : "配置";
        ExpressionTab.Header = legacy ? "表情一覧" : "表情をまとめて";
        SelectionTab.Header = legacy ? "選択配置" : "設定";
        PresetSurface.Visibility = legacy ? Visibility.Visible : Visibility.Collapsed;
        SynchronizeTask();
    }
    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlacerViewModel.SceneName)) { SynchronizeTask(); observedViewModel?.RefreshExpressionVocabulary(); }
        if (e.PropertyName == nameof(PlacerViewModel.UseLegacyWorkspace)) RefreshWorkspaceSurface();
        if (e.PropertyName is nameof(PlacerViewModel.IsAddingTemplate) or nameof(PlacerViewModel.IsManagingTemplates)) SynchronizeTask();
    }
    private void SynchronizeTask()
    {
        var vm = observedViewModel;
        if (vm == null) { PointerRouter.Detach(); ShortcutRouter.Detach(); return; }
        if (IsLoaded && IsVisible) vm.ActivateIntentWorkspace(); else vm.DeactivateIntentWorkspace();
        if (IsLoaded && IsVisible && vm.HasIntentTimeline && !vm.UseLegacyWorkspace) PointerRouter.Attach(); else PointerRouter.Detach();
        if (IsLoaded && IsVisible && vm.HasIntentTimeline && !vm.UseLegacyWorkspace && PaletteTab.IsSelected && !vm.IsAddingTemplate && !vm.IsManagingTemplates) ShortcutRouter.Attach(); else ShortcutRouter.Detach();
        vm.SetActiveTask(!IsLoaded || !IsVisible ? "" : vm.IsManagingTemplates ? "library" : vm.IsAddingTemplate ? "adding" :
            SelectionTab.IsSelected ? vm.UseLegacyWorkspace ? "selection" : "intent-settings" : ExpressionTab.IsSelected ? "expression" : "palette");
    }
    private void RefreshExpressionDetail()
    {
        ExpressionRowDetail.Visibility = ActualWidth < 520 && VoiceGrid.SelectedItem is AssignmentRow ? Visibility.Visible : Visibility.Collapsed;
        var editorHeight = Math.Clamp(ActualHeight - 360, 90, 280);
        foreach (var content in new[] { PresetSurface.PresetEditor.Content, PaletteSurface.PaletteEditor.Content, PaletteSurface.DropSurface.LayerEditor.Content })
            if (content is ScrollViewer scroll) scroll.MaxHeight = editorHeight;
    }
}
