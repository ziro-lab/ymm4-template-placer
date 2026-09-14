using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

public partial class PlacerView : UserControl
{
    private PlacerViewModel? observedViewModel;
    public PlacerView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => RefreshExpressionDetail();
        PresetSurface.PresetEditor.Expanded += (_, _) => ExcelEditor.IsExpanded = false;
        ExcelEditor.Expanded += (_, _) => PresetSurface.PresetEditor.IsExpanded = false;
        VoiceGrid.SelectionChanged += (_, _) => RefreshExpressionDetail();
        MainTabs.SelectionChanged += (_, e) => { if (ReferenceEquals(e.OriginalSource, MainTabs)) SynchronizeTask(); };
        DataContextChanged += (_, _) => { ObserveViewModel(IsLoaded ? DataContext as PlacerViewModel : null); SynchronizeTask(); };
        Loaded += (_, _) => { ObserveViewModel(DataContext as PlacerViewModel); SynchronizeTask(); };
        Unloaded += (_, _) => ObserveViewModel(null);
        IsVisibleChanged += (_, _) => SynchronizeTask();
#if YMM4_PROOF
        NativeProof.View = this;
#endif
    }
    private void ObserveViewModel(PlacerViewModel? next)
    {
        if (ReferenceEquals(observedViewModel, next)) return;
        if (observedViewModel != null)
        {
            observedViewModel.PropertyChanged -= ViewModelChanged;
            observedViewModel.SetActiveTask("");
        }
        observedViewModel = next;
        if (next != null) next.PropertyChanged += ViewModelChanged;
    }
    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlacerViewModel.IsAddingTemplate) or nameof(PlacerViewModel.IsManagingTemplates)) SynchronizeTask();
    }
    private void SynchronizeTask()
    {
        var vm = observedViewModel;
        if (vm == null) return;
        vm.SetActiveTask(!IsLoaded || !IsVisible ? "" : vm.IsManagingTemplates ? "library" : vm.IsAddingTemplate ? "adding" :
            SelectionTab.IsSelected ? "selection" : ExpressionTab.IsSelected ? "expression" : "palette");
    }
    private void RefreshExpressionDetail() => ExpressionRowDetail.Visibility =
        ActualWidth < 520 && VoiceGrid.SelectedItem is AssignmentRow ? Visibility.Visible : Visibility.Collapsed;
}
