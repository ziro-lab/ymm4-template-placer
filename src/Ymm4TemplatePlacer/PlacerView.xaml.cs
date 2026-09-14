using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

public partial class PlacerView : UserControl
{
    private PlacerViewModel? observedViewModel;
    private LibraryEntry? suspendedSelectionEntry;
    public PlacerView()
    {
        InitializeComponent();
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
        // YMM4 can keep this View while replacing its suspended ViewModel.
        // Reuse only the exact unchanged registration the user had selected, never infer another source.
        if (e.OldValue is PlacerViewModel previous) suspendedSelectionEntry = previous.SelectionTemplate?.Entry;
        var next = e.NewValue as PlacerViewModel;
        ObserveViewModel(IsLoaded ? next : null);
        if (next != null)
        {
            if (suspendedSelectionEntry != null && next.SelectionTemplate == null)
            {
                var choice = next.SelectionTemplates.FirstOrDefault(x => x.Entry == suspendedSelectionEntry);
                if (choice != null) next.SelectionTemplate = choice;
            }
            suspendedSelectionEntry = null;
        }
        SynchronizeTask();
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
    private void RefreshExpressionDetail()
    {
        ExpressionRowDetail.Visibility = ActualWidth < 520 && VoiceGrid.SelectedItem is AssignmentRow ? Visibility.Visible : Visibility.Collapsed;
        // More room in a taller Tool means less scrolling, without adding a user setting.
        var editorHeight = Math.Clamp(ActualHeight - 360, 90, 280);
        foreach (var content in new[] { PresetSurface.PresetEditor.Content, PaletteSurface.PaletteEditor.Content, PaletteSurface.DropSurface.LayerEditor.Content })
            if (content is ScrollViewer scroll) scroll.MaxHeight = editorHeight;
    }
}
