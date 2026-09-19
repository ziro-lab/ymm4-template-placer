using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class IntentPalettePanel : UserControl
{
    private enum TilePointerPhase { Idle, Pressed, Dragging, SuppressRelease }
    private TilePointerPhase pointerPhase;
    private ContextMenu? activeMenu;
    private (Grid Cell, IntentTileChoice Tile, Point Point)? pendingDrag;
    public IntentPalettePanel()
    {
        InitializeComponent();
        Unloaded += (_, _) => { CancelLocalGesture(); CloseTileMenu(); };
        DataContextChanged += (_, _) => { CancelLocalGesture(); CloseTileMenu(); };
    }
    private void CancelLocalGesture()
    {
        var cell = pendingDrag?.Cell; pendingDrag = null; pointerPhase = TilePointerPhase.Idle;
        if (cell?.IsMouseCaptured == true) cell.ReleaseMouseCapture();
    }
    private void CloseTileMenu()
    {
        if (activeMenu is { } menu) { activeMenu = null; menu.IsOpen = false; menu.Closed -= TileMenuClosed; menu.Items.Clear(); }
    }
    private void TileMenuClosed(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu) { menu.Closed -= TileMenuClosed; if (ReferenceEquals(activeMenu, menu)) activeMenu = null; }
    }
    private void TilePointerDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Grid { DataContext: IntentTileChoice tile } cell) return;
        pendingDrag = (cell, tile, e.GetPosition(this)); pointerPhase = TilePointerPhase.Pressed;
        // Keep the real Button's short-click/keyboard semantics. An unavailable tile
        // still allows appearance/reorder on the enabled surrounding cell.
        if (cell.Children.OfType<Button>().SingleOrDefault()?.IsEnabled != true) cell.CaptureMouse();
    }
    private void TilePointerUp(object sender, MouseButtonEventArgs e)
    {
        var suppress = pointerPhase is TilePointerPhase.Dragging or TilePointerPhase.SuppressRelease;
        var cell = pendingDrag?.Cell; pendingDrag = null; pointerPhase = TilePointerPhase.Idle;
        if (cell?.IsMouseCaptured == true) cell.ReleaseMouseCapture();
        if (suppress) e.Handled = true;
    }
    private void TilePointerLostCapture(object sender, MouseEventArgs e)
    {
        if (pointerPhase == TilePointerPhase.Pressed) { pendingDrag = null; pointerPhase = TilePointerPhase.Idle; }
    }
    private void TilePointerMove(object sender, MouseEventArgs e)
    {
        if (pointerPhase != TilePointerPhase.Pressed || pendingDrag is not { } drag) return;
        if (e.LeftButton != MouseButtonState.Pressed) { CancelLocalGesture(); return; }
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - drag.Point.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - drag.Point.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        pointerPhase = TilePointerPhase.Dragging; pendingDrag = null;
        // Release the Button before starting the modal drag loop: its IsPressed state
        // must not survive into a placement Click when the mouse button is released.
        if (Mouse.Captured is DependencyObject captured && (ReferenceEquals(captured, drag.Cell) || drag.Cell.IsAncestorOf(captured))) Mouse.Capture(null);
        e.Handled = true;
        try
        {
            if (DataContext is PlacerViewModel vm && vm.ReorderIntentTileCommand.CanExecute(new IntentTileReorderRequest(drag.Tile, drag.Tile)))
                DragDrop.DoDragDrop(drag.Cell, new DataObject(typeof(IntentTileChoice), drag.Tile), DragDropEffects.Move);
        }
        finally { pointerPhase = TilePointerPhase.SuppressRelease; }
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
    private void TileContextOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: IntentTileChoice tile, ContextMenu: { } menu } || DataContext is not PlacerViewModel vm) return;
        CancelLocalGesture(); menu.Items.Clear(); menu.DataContext = tile;
        activeMenu = menu; menu.Closed -= TileMenuClosed; menu.Closed += TileMenuClosed;
        MenuItem Action(string title, ICommand command, object parameter) => new()
        {
            Header = title, Command = command, CommandParameter = parameter,
            ToolTip = vm.IntentTileEditNotice
        };
        menu.Items.Add(Action("表示名を変更…", vm.RenameIntentTileCommand, tile));
        var colors = new MenuItem { Header = "色" };
        foreach (var option in vm.IntentTileColors)
        {
            var item = Action(option.Name, vm.ColorIntentTileCommand, new IntentTileColorRequest(tile, option.Value));
            item.IsCheckable = true; item.IsChecked = tile.Color == option.Value; colors.Items.Add(item);
        }
        var shapes = new MenuItem { Header = "形" };
        foreach (var option in vm.IntentTileShapes)
        {
            var item = Action(option.Name, vm.ShapeIntentTileCommand, new IntentTileShapeRequest(tile, option.Value));
            item.IsCheckable = true; item.IsChecked = tile.Shape == option.Value; shapes.Items.Add(item);
        }
        menu.Items.Add(colors); menu.Items.Add(shapes); menu.Items.Add(new Separator());
        menu.Items.Add(Action("Setの設定を開く", vm.OpenTileSettingsCommand, tile));
    }
}
