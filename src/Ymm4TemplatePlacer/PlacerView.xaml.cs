using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ymm4TemplatePlacer;

public partial class PlacerView : UserControl
{
    private PlacerViewModel? observedViewModel;
    private TransientWorkSnapshot? suspendedWork;
    public IntentPalettePanel RelativePaletteSurface { get; } = new();
    public IntentSettingsPanel RelativeSettingsSurface { get; } = new();
    private readonly Button returnToRelative = new() { Content = "相対パレットへ戻る", Padding = new Thickness(10, 4, 10, 4), Visibility = Visibility.Collapsed };
    public PlacerView()
    {
        InitializeComponent();
        returnToRelative.SetBinding(Button.CommandProperty, new Binding(nameof(PlacerViewModel.CloseLegacyWorkspaceCommand)));
        if (RefreshButton.Parent is DockPanel header) { DockPanel.SetDock(returnToRelative, Dock.Right); header.Children.Insert(0, returnToRelative); }
        var choiceStyle = new Style(typeof(ComboBoxItem));
        choiceStyle.Setters.Add(new Setter(IsEnabledProperty, new Binding(nameof(TemplateChoice.IsAvailable))));
        VoiceGrid.Resources[typeof(ComboBoxItem)] = choiceStyle;
        SizeChanged += (_, _) => RefreshExpressionDetail();
        PresetSurface.PresetEditor.Expanded += (_, _) => ExcelEditor.IsExpanded = false;
        ExcelEditor.Expanded += (_, _) => PresetSurface.PresetEditor.IsExpanded = false;
        VoiceGrid.SelectionChanged += (_, _) => RefreshExpressionDetail();
        MainTabs.SelectionChanged += (_, e) => { if (ReferenceEquals(e.OriginalSource, MainTabs)) SynchronizeTask(); };
        DataContextChanged += ChangeViewModel;
        Loaded += (_, _) => { ObserveViewModel(DataContext as PlacerViewModel); SynchronizeTask(); };
        Unloaded += (_, _) => ObserveViewModel(null);
        IsVisibleChanged += (_, _) => SynchronizeTask();
#if YMM4_PROOF
        NativeProof.View = this;
#endif
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
        if (observedViewModel != null)
        {
            observedViewModel.PropertyChanged -= ViewModelChanged; observedViewModel.IntentSettingsRequested -= OpenIntentSettings;
            observedViewModel.DeactivateIntentWorkspace(); observedViewModel.SetActiveTask("");
        }
        observedViewModel = next;
        if (next != null)
        {
            next.PropertyChanged += ViewModelChanged; next.IntentSettingsRequested += OpenIntentSettings;
            next.ActivateIntentWorkspace(); next.AttachRelativeExpressionBindings(); RefreshWorkspaceSurface();
        }
    }
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
        returnToRelative.Visibility = legacy ? Visibility.Visible : Visibility.Collapsed;
        SynchronizeTask();
    }
    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlacerViewModel.SceneName)) { observedViewModel?.ActivateIntentWorkspace(); observedViewModel?.RefreshExpressionVocabulary(); }
        if (e.PropertyName == nameof(PlacerViewModel.UseLegacyWorkspace)) RefreshWorkspaceSurface();
        if (e.PropertyName is nameof(PlacerViewModel.IsAddingTemplate) or nameof(PlacerViewModel.IsManagingTemplates)) SynchronizeTask();
    }
    private void SynchronizeTask()
    {
        var vm = observedViewModel; if (vm == null) return;
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
