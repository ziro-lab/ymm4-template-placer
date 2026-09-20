using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class IntentPalettePanel : UserControl
{
    private enum TilePointerPhase { Idle, Pressed, Dragging, SuppressRelease }
    private TilePointerPhase pointerPhase;
    private ContextMenu? activeMenu;
    private PlacerViewModel? observedRoot;
    private Guid? quickPopupSetId;
    private string quickPopupContext = "";
    private (Grid Cell, IntentTileChoice Tile, Point Point)? pendingDrag;
    public IntentPalettePanel()
    {
        InitializeComponent();
        Loaded += (_, _) => { ObserveRoot(DataContext as PlacerViewModel); SystemParameters.StaticPropertyChanged -= ThemeChanged; SystemParameters.StaticPropertyChanged += ThemeChanged; };
        Unloaded += (_, _) => { PanelQuickSettingsButton.IsChecked = false; ObserveRoot(null); SystemParameters.StaticPropertyChanged -= ThemeChanged; CancelLocalGesture(); CloseTileMenu(); };
        DataContextChanged += (_, _) => { PanelQuickSettingsButton.IsChecked = false; ObserveRoot(IsLoaded ? DataContext as PlacerViewModel : null); CancelLocalGesture(); CloseTileMenu(); };
        PanelQuickSettingsPopup.Closed += (_, _) => { PanelQuickSettingsButton.IsChecked = false; quickPopupSetId = null; quickPopupContext = ""; observedRoot?.EndPanelQuickSettings(); };
        PreviewMouseDown += PanelPointerDown;
        IsVisibleChanged += (_, _) => { if (!IsVisible) PanelQuickSettingsButton.IsChecked = false; };
    }
    private void PanelPointerDown(object sender, MouseButtonEventArgs e)
    {
        if (!PanelQuickSettingsPopup.IsOpen || e.OriginalSource is not DependencyObject source) return;
        for (var current = source; current != null; current = TimelinePointerIntentClassifier.Parent(current))
            if (ReferenceEquals(current, PanelQuickSettingsButton)) return;
        // Popup content is a separate visual tree. Any pointer routed through this
        // placement panel is therefore outside the open quick-settings popup.
        PanelQuickSettingsButton.IsChecked = false;
    }
    private void ObserveRoot(PlacerViewModel? next)
    {
        if (ReferenceEquals(observedRoot, next)) return;
        if (observedRoot != null) observedRoot.PropertyChanged -= RootChanged;
        observedRoot = next;
        if (observedRoot != null) observedRoot.PropertyChanged += RootChanged;
    }
    private void RootChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!PanelQuickSettingsPopup.IsOpen || observedRoot == null ||
            e.PropertyName is not (nameof(PlacerViewModel.SelectedIntentSet) or nameof(PlacerViewModel.IntentContextTitle))) return;
        if (observedRoot.SelectedIntentSet?.Id != quickPopupSetId ||
            !string.Equals(observedRoot.IntentContextTitle, quickPopupContext, StringComparison.Ordinal))
            PanelQuickSettingsButton.IsChecked = false;
    }
    private void PanelQuickSettingsOpened(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PlacerViewModel vm) return;
        quickPopupSetId = vm.SelectedIntentSet?.Id;
        quickPopupContext = vm.IntentContextTitle;
        vm.BeginPanelQuickSettings();
    }
    private void ThemeChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Local visual refresh only; never rebuild the root's tiles/Rows or change settings.
        if (IsLoaded) Dispatcher.InvokeAsync(() => IntentTileItems.Items.Refresh());
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
        e.Handled = true == suppress ? true : e.Handled;
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
