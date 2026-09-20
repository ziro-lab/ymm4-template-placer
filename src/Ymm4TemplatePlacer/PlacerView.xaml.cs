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
