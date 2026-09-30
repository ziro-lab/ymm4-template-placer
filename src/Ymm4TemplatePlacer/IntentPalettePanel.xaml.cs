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
    private Window? observedOwnerWindow;
    private bool ownerQuickDismissAttached;
    private Guid? quickPopupSetId;
    private (Grid Cell, IntentTileChoice Tile, Point Point)? pendingDrag;
    private bool exposeSelectedSet;
    private double setTabWidth = 140;
    public ScrollViewer? IntentSetHeaderScroll => IntentSetSegments?.Template?.FindName("IntentSetHeaderScroll", IntentSetSegments) as ScrollViewer;
    public IntentPalettePanel()
    {
        InitializeComponent();
        IntentSetSegments.ItemContainerGenerator.StatusChanged += (_, _) => RequestSelectedSetExposure();
        IntentSetSegments.LayoutUpdated += IntentSetHeaderLayoutUpdated;
        Loaded += (_, _) =>
        {
            RequestSelectedSetExposure();
            ObserveRoot(DataContext as PlacerViewModel);
            ObserveOwnerWindow(Window.GetWindow(this));
            SystemParameters.StaticPropertyChanged -= ThemeChanged;
            SystemParameters.StaticPropertyChanged += ThemeChanged;
        };
        Unloaded += (_, _) =>
        {
            exposeSelectedSet = false;
            PanelQuickSettingsButton.IsChecked = false;
            ObserveOwnerWindow(null);
            ObserveRoot(null);
            SystemParameters.StaticPropertyChanged -= ThemeChanged;
            CancelLocalGesture();
            CloseTileMenu();
        };
        DataContextChanged += (_, _) => { RequestSelectedSetExposure(); PanelQuickSettingsButton.IsChecked = false; ObserveRoot(IsLoaded ? DataContext as PlacerViewModel : null); CancelLocalGesture(); CloseTileMenu(); };
        PanelQuickSettingsPopup.Closed += (_, _) =>
        {
            observedRoot?.EndPanelQuickSettings();
            DetachOwnerQuickDismiss();
            PanelQuickSettingsButton.IsChecked = false;
            quickPopupSetId = null;
        };
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible) { exposeSelectedSet = false; PanelQuickSettingsButton.IsChecked = false; }
            else RequestSelectedSetExposure();
        };
    }
    private void RequestSelectedSetExposure()
    {
        // One local layout flag always reads the CURRENT selection. There are no
        // queued selection snapshots that could scroll back after rapid input/re-entry.
        exposeSelectedSet = true;
        SizeSetTabs();
    }
    private void SizeSetTabs()
    {
        var viewport = IntentSetHeaderScroll?.ViewportWidth ?? 0;
        if (viewport <= 0 || !double.IsFinite(viewport)) return;
        var available = Math.Max(1, viewport - 4); // room for the native selected-tab overlap
        var columns = Math.Min(Math.Max(1, IntentSetSegments.Items.Count), Math.Max(1, (int)Math.Floor(available / 120)));
        var width = Math.Clamp(available / columns, 64, 160);
        setTabWidth = width;
        for (var i = 0; i < IntentSetSegments.Items.Count; i++)
            if (IntentSetSegments.ItemContainerGenerator.ContainerFromIndex(i) is TabItem tab && Math.Abs(tab.Width - width) > 0.1)
                tab.Width = width;
    }
    private void IntentSetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, IntentSetSegments)) RequestSelectedSetExposure();
        // Keep the normal TwoWay binding as the sole route into the root coordinator.
        // The owner's MainTabs handler already ignores nested SelectionChanged events.
    }
    private void IntentSetHeaderScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ViewportWidthChange != 0) RequestSelectedSetExposure();
        UpdateSetBrowseButtons();
    }
    private void IntentSetHeaderLayoutUpdated(object? sender, EventArgs e)
    {
        if (!exposeSelectedSet || !IsLoaded || !IsVisible) return;
        if (IntentSetHeaderScroll is not { ViewportWidth: > 0 } scroll || !scroll.IsArrangeValid) return;
        if (IntentSetSegments.SelectedItem == null) { exposeSelectedSet = false; UpdateSetBrowseButtons(); return; }
        if (IntentSetSegments.ItemContainerGenerator.ContainerFromItem(IntentSetSegments.SelectedItem) is not TabItem tab ||
            !tab.IsArrangeValid || tab.ActualWidth <= 0) return;
        exposeSelectedSet = false;
        var bounds = tab.TransformToAncestor(scroll).TransformBounds(new Rect(tab.RenderSize));
        var left = bounds.Left - 2;
        var right = bounds.Right + 2;
        var offset = scroll.HorizontalOffset;
        if (left < 0 || right - left > scroll.ViewportWidth) offset += left;
        else if (right > scroll.ViewportWidth) offset += right - scroll.ViewportWidth;
        scroll.ScrollToHorizontalOffset(Math.Clamp(offset, 0, scroll.ScrollableWidth));
        UpdateSetBrowseButtons();
    }
    private void UpdateSetBrowseButtons()
    {
        if (IntentSetScrollLeftButton == null || IntentSetScrollRightButton == null) return;
        var scroll = IntentSetHeaderScroll;
        IntentSetScrollLeftButton.IsEnabled = scroll != null && scroll.HorizontalOffset > 0.5;
        IntentSetScrollRightButton.IsEnabled = scroll != null && scroll.HorizontalOffset < scroll.ScrollableWidth - 0.5;
    }
    private void BrowseSets(int direction)
    {
        if (IntentSetHeaderScroll is not { } scroll) return;
        // Browsing exposes adjacent headers only. Selection, placement and saved order
        // remain unchanged until a real native TabItem is selected.
        exposeSelectedSet = false;
        scroll.ScrollToHorizontalOffset(Math.Clamp(scroll.HorizontalOffset + direction * setTabWidth, 0, scroll.ScrollableWidth));
    }
    private void IntentSetScrollLeft(object sender, RoutedEventArgs e) => BrowseSets(-1);
    private void IntentSetScrollRight(object sender, RoutedEventArgs e) => BrowseSets(1);

    private void ObserveOwnerWindow(Window? next)
    {
        if (ReferenceEquals(observedOwnerWindow, next)) return;
        DetachOwnerQuickDismiss();
        if (observedOwnerWindow != null) observedOwnerWindow.Deactivated -= OwnerWindowDeactivated;
        observedOwnerWindow = next;
        if (observedOwnerWindow != null) observedOwnerWindow.Deactivated += OwnerWindowDeactivated;
        if (PanelQuickSettingsPopup.IsOpen) AttachOwnerQuickDismiss();
    }
    private void AttachOwnerQuickDismiss()
    {
        if (ownerQuickDismissAttached || observedOwnerWindow == null) return;
        observedOwnerWindow.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OwnerWindowPreviewMouseDown), true);
        ownerQuickDismissAttached = true;
    }
    private void DetachOwnerQuickDismiss()
    {
        if (!ownerQuickDismissAttached || observedOwnerWindow == null) { ownerQuickDismissAttached = false; return; }
        observedOwnerWindow.RemoveHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OwnerWindowPreviewMouseDown));
        ownerQuickDismissAttached = false;
    }
    private void OwnerWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!PanelQuickSettingsPopup.IsOpen) return;
        if (e.OriginalSource is DependencyObject source)
        {
            if (ReferenceEquals(source, PanelQuickSettingsButton) || PanelQuickSettingsButton.IsAncestorOf(source)) return;
            if (PanelQuickSettingsPopup.Child is FrameworkElement popupRoot &&
                (ReferenceEquals(source, popupRoot) || popupRoot.IsAncestorOf(source))) return;
        }
        // Settle a valid placement quick edit before the owner-window click is
        // allowed to continue into a placement command using saved settings.
        observedRoot?.EndPanelQuickSettings();
        // Keep interaction inside the Popup open. Any remaining owner-window mouse
        // input is outside quick settings, so close without consuming that YMM4 click.
        PanelQuickSettingsButton.IsChecked = false;
    }

    private void OwnerWindowDeactivated(object? sender, EventArgs e)
    {
        // WPF Popup owns a separate native surface. Tie its visibility back to the
        // actual YMM4 owner window so it cannot remain stranded over another app.
        observedRoot?.EndPanelQuickSettings();
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
            e.PropertyName != nameof(PlacerViewModel.SelectedIntentSet)) return;
        // RefreshIntentWorkspace briefly clears/rebuilds descriptive Context text.
        // Popup lifetime follows the logical Set identity instead of those transient labels.
        if (observedRoot.SelectedIntentSet?.Id != quickPopupSetId)
            PanelQuickSettingsButton.IsChecked = false;
    }
    private void PanelQuickSettingsOpened(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PlacerViewModel vm) return;
        ObserveOwnerWindow(Window.GetWindow(this));
        quickPopupSetId = vm.SelectedIntentSet?.Id;
        AttachOwnerQuickDismiss();
        vm.BeginPanelQuickSettings();
        PanelQuickSettingsSurface.SelectDefaultPage();
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
