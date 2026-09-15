using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace Ymm4TemplatePlacer;
public partial class PalettePanel : UserControl
{
    public QuickDropControls DropSurface { get; } = new();
    private Point dragStart;
    private PaletteEntryView? dragEntry;
    private ListBoxItem? dropCue;
    public PalettePanel()
    {
        InitializeComponent(); DropControls.Content = DropSurface;
        PaletteEditor.Expanded += (_, _) => DropSurface.LayerEditor.IsExpanded = false;
        DropSurface.LayerEditor.Expanded += (_, _) => PaletteEditor.IsExpanded = false;
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new EventSetter(Control.MouseDoubleClickEvent, new MouseButtonEventHandler(OnPaletteEntryDoubleClick)));
        PaletteList.ItemContainerStyle = style;
    }
    private void OnPaletteEntryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || sender is not ListBoxItem container || container.DataContext is not PaletteEntryView entry || DataContext is not PlacerViewModel vm) return;
        vm.SelectedPaletteEntry = entry;
        if (vm.QuickDropCommand.CanExecute(null)) vm.QuickDropCommand.Execute(null);
        e.Handled = true;
    }
    private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || sender is not Border handle || handle.DataContext is not PaletteEntryView entry) return;
        dragStart = e.GetPosition(PaletteList); dragEntry = entry; PaletteList.SelectedItem = entry;
        handle.CaptureMouse(); e.Handled = true;
    }
    private void OnDragHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (dragEntry == null || e.LeftButton != MouseButtonState.Pressed || sender is not Border handle) return;
        var point = e.GetPosition(PaletteList);
        if (Math.Abs(point.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var entry = dragEntry; dragEntry = null; handle.ReleaseMouseCapture();
        var data = new DataObject(typeof(PaletteEntryView), entry);
        DragDrop.DoDragDrop(handle, data, DragDropEffects.Move);
        ClearDropCue(); e.Handled = true;
    }
    private void OnDragHandleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement handle && handle.IsMouseCaptured) handle.ReleaseMouseCapture();
        dragEntry = null; e.Handled = true;
    }
    private void OnDragHandleDoubleClick(object sender, MouseButtonEventArgs e) => e.Handled = true;
    private void OnPaletteDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(PaletteEntryView))) { e.Effects = DragDropEffects.None; ClearDropCue(); return; }
        var target = FindContainer(e.OriginalSource as DependencyObject);
        if (target != null)
        {
            var before = e.GetPosition(target).Y <= target.ActualHeight / 2;
            ShowDropCue(target, before);
        }
        else ClearDropCue();
        e.Effects = DragDropEffects.Move; e.Handled = true;
    }
    private void OnPaletteDragLeave(object sender, DragEventArgs e) => ClearDropCue();
    private void OnPaletteDrop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetData(typeof(PaletteEntryView)) is not PaletteEntryView source || DataContext is not PlacerViewModel vm) return;
            var target = FindContainer(e.OriginalSource as DependencyObject);
            var insertionIndex = PaletteList.Items.Count;
            if (target != null)
            {
                insertionIndex = PaletteList.ItemContainerGenerator.IndexFromContainer(target);
                if (e.GetPosition(target).Y > target.ActualHeight / 2) insertionIndex++;
            }
            var request = new PaletteMoveRequest(source.LibraryEntryId, insertionIndex);
            if (vm.MovePaletteEntryCommand.CanExecute(request)) vm.MovePaletteEntryCommand.Execute(request);
            e.Handled = true;
        }
        finally { ClearDropCue(); }
    }
    private ListBoxItem? FindContainer(DependencyObject? current)
    {
        while (current != null && !ReferenceEquals(current, PaletteList))
        {
            if (current is ListBoxItem item) return item;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    private void ShowDropCue(ListBoxItem target, bool before)
    {
        if (!ReferenceEquals(dropCue, target)) ClearDropCue();
        dropCue = target; target.BorderBrush = SystemColors.HighlightBrush;
        target.BorderThickness = before ? new Thickness(0, 2, 0, 0) : new Thickness(0, 0, 0, 2);
    }
    private void ClearDropCue()
    {
        if (dropCue == null) return;
        dropCue.ClearValue(Control.BorderBrushProperty); dropCue.ClearValue(Control.BorderThicknessProperty); dropCue = null;
    }
}
