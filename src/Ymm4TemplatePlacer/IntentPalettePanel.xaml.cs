using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class IntentPalettePanel : UserControl
{
    private (FrameworkElement Handle, IntentTileChoice Tile, Point Point)? pendingDrag;
    public IntentPalettePanel() => InitializeComponent();
    private void TileHandleDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement handle && handle.DataContext is IntentTileChoice tile)
        {
            pendingDrag = (handle, tile, e.GetPosition(this)); handle.CaptureMouse(); e.Handled = true;
        }
    }
    private void TileHandleUp(object sender, MouseButtonEventArgs e)
    {
        pendingDrag = null;
        if (sender is FrameworkElement handle && handle.IsMouseCaptured) handle.ReleaseMouseCapture();
        e.Handled = true;
    }
    private void TileHandleLostCapture(object sender, MouseEventArgs e) => pendingDrag = null;
    private void TileHandleMove(object sender, MouseEventArgs e)
    {
        if (pendingDrag is not { } drag || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - drag.Point.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - drag.Point.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        pendingDrag = null; drag.Handle.ReleaseMouseCapture(); e.Handled = true;
        // The handle is a sibling of the action Button; it can never press / click it.
        DragDrop.DoDragDrop(drag.Handle, new DataObject(typeof(IntentTileChoice), drag.Tile), DragDropEffects.Move);
    }
    private IntentTileReorderRequest? Request(object sender, DragEventArgs e) =>
        sender is FrameworkElement { DataContext: IntentTileChoice target } &&
        e.Data.GetDataPresent(typeof(IntentTileChoice)) && e.Data.GetData(typeof(IntentTileChoice)) is IntentTileChoice source
            ? new(source, target) : null;
    private void TileDragOver(object sender, DragEventArgs e)
    {
        var request = Request(sender, e);
        e.Effects = request != null && DataContext is PlacerViewModel vm && vm.ReorderIntentTileCommand.CanExecute(request)
            ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }
    private void TileDrop(object sender, DragEventArgs e)
    {
        var request = Request(sender, e);
        if (request != null && DataContext is PlacerViewModel vm && vm.ReorderIntentTileCommand.CanExecute(request))
            vm.ReorderIntentTileCommand.Execute(request);
        e.Handled = true;
    }
}
