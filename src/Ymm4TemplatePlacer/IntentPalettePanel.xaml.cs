using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
    private double setTabWidth = 52;
    private TilePointerPhase setPointerPhase;
    private (TabItem Header, IntentSetChoice Set, Point Point, PlacerViewModel Root)? pendingSetDrag;
    private IntentSetChoice? draggingSet;
    private PlacerViewModel? draggingSetRoot;
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
            CancelSetGesture();
            CloseTileMenu();
        };
        DataContextChanged += (_, _) => { CancelSetGesture(); RequestSelectedSetExposure(); PanelQuickSettingsButton.IsChecked = false; ObserveRoot(IsLoaded ? DataContext as PlacerViewModel : null); CancelLocalGesture(); CloseTileMenu(); };
        PanelQuickSettingsPopup.Closed += (_, _) =>
        {
            observedRoot?.EndPanelQuickSettings();
            DetachOwnerQuickDismiss();
            PanelQuickSettingsButton.IsChecked = false;
            quickPopupSetId = null;
        };
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible) { exposeSelectedSet = false; CancelSetGesture(); PanelQuickSettingsButton.IsChecked = false; }
            else RequestSelectedSetExposure();
        };
    }
    private void RequestSelectedSetExposure()
    {
        // One local layout flag always reads the CURRENT selection. There are no
        // queued selection snapshots that could scroll back after rapid input/re-entry.
        exposeSelectedSet = setPointerPhase != TilePointerPhase.Dragging;
        SizeSetTabs();
    }
    private void SizeSetTabs()
    {
        var viewport = IntentSetHeaderScroll?.ViewportWidth ?? 0;
        if (viewport <= 0 || !double.IsFinite(viewport)) return;
        var available = Math.Max(1, viewport - 4); // room for the native selected-tab overlap
        var columns = Math.Min(Math.Max(1, IntentSetSegments.Items.Count), Math.Max(1, (int)Math.Floor(available / 52)));
        var width = Math.Clamp(available / columns, 50, 96);
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
    private void ScrollSetHeaders(double distance)
    {
        if (IntentSetHeaderScroll is not { } scroll) return;
        // Browsing exposes adjacent headers only. Selection, placement and saved order
        // remain unchanged until a real native TabItem is selected.
        exposeSelectedSet = false;
        scroll.ScrollToHorizontalOffset(Math.Clamp(scroll.HorizontalOffset + distance, 0, scroll.ScrollableWidth));
    }
    private void BrowseSets(int direction) => ScrollSetHeaders(direction * setTabWidth);
    private void IntentSetWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || e.Delta == 0 || Keyboard.Modifiers != ModifierKeys.None ||
            setPointerPhase == TilePointerPhase.Dragging || IntentSetHeaderScroll == null ||
            !new Rect(IntentSetRow.RenderSize).Contains(e.GetPosition(IntentSetRow))) return;
        // Only this bounded header strip (including arrows) owns plain wheel
        // input. Preserve fractional deltas and consume both saturated ends so
        // browsing cannot spill into the tile area's vertical scroll or host.
        ScrollSetHeaders(-e.Delta / (double)Mouse.MouseWheelDeltaForOneLine * setTabWidth);
        e.Handled = true;
    }
    private void IntentSetScrollLeft(object sender, RoutedEventArgs e) => BrowseSets(-1);
    private void IntentSetScrollRight(object sender, RoutedEventArgs e) => BrowseSets(1);

    private void CancelSetGesture()
    {
        var header = pendingSetDrag?.Header;
        pendingSetDrag = null; draggingSet = null; draggingSetRoot = null;
        setPointerPhase = TilePointerPhase.Idle;
        ClearSetInsertion();
        if (header?.IsMouseCaptured == true) header.ReleaseMouseCapture();
    }
    private void ClearSetInsertion() => IntentSetInsertionMarker.Visibility = Visibility.Collapsed;
    private void SetPointerDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TabItem { DataContext: IntentSetChoice set } header || DataContext is not PlacerViewModel vm) return;
        CancelSetGesture();
        pendingSetDrag = (header, set, e.GetPosition(this), vm);
        setPointerPhase = TilePointerPhase.Pressed;
        // Native TabItem selects on mouse-down. Defer this pointer selection until
        // a short release so dragging another header preserves the current Set.
        // Keyboard selection and the native TabItem template remain unchanged.
        e.Handled = true;
        if (!header.CaptureMouse()) CancelSetGesture();
    }
    private void SetPointerUp(object sender, MouseButtonEventArgs e)
    {
        if (setPointerPhase == TilePointerPhase.Idle) return;
        var drag = pendingSetDrag;
        var select = setPointerPhase == TilePointerPhase.Pressed && drag != null &&
            ReferenceEquals(DataContext, drag.Value.Root) && drag.Value.Root.IntentSets.Any(x => ReferenceEquals(x, drag.Value.Set)) &&
            new Rect(drag.Value.Header.RenderSize).Contains(e.GetPosition(drag.Value.Header));
        CancelSetGesture();
        e.Handled = true;
        if (select && drag is { } click)
        {
            // SetCurrentValue preserves the normal Selector binding route; Focus
            // supplies native tab focus/keyboard behavior for this short click.
            click.Header.SetCurrentValue(TabItem.IsSelectedProperty, true);
            click.Header.Focus();
        }
    }
    private void SetPointerLostCapture(object sender, MouseEventArgs e)
    {
        if (setPointerPhase == TilePointerPhase.Pressed) CancelSetGesture();
    }
    private void SetPointerMove(object sender, MouseEventArgs e)
    {
        if (setPointerPhase != TilePointerPhase.Pressed || pendingSetDrag is not { } drag) return;
        if (e.LeftButton != MouseButtonState.Pressed || !ReferenceEquals(DataContext, drag.Root)) { CancelSetGesture(); return; }
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - drag.Point.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - drag.Point.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        e.Handled = true;
        if (drag.Root.ReorderIntentSetCommand?.CanExecute(new IntentSetReorderRequest(drag.Set, drag.Set)) != true)
        {
            setPointerPhase = TilePointerPhase.SuppressRelease;
            return;
        }
        setPointerPhase = TilePointerPhase.Dragging;
        draggingSet = drag.Set; draggingSetRoot = drag.Root;
        pendingSetDrag = null;
        if (drag.Header.IsMouseCaptured) drag.Header.ReleaseMouseCapture();
        exposeSelectedSet = false;
        try
        {
            DragDrop.DoDragDrop(drag.Header, new DataObject(typeof(IntentSetChoice), drag.Set), DragDropEffects.Move);
        }
        finally
        {
            draggingSet = null; draggingSetRoot = null;
            setPointerPhase = TilePointerPhase.Idle;
            ClearSetInsertion();
            RequestSelectedSetExposure();
        }
    }
    private void SetQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
    {
        if (!IsVisible || draggingSet == null || draggingSetRoot is not { } root || !ReferenceEquals(DataContext, root) ||
            root.ReorderIntentSetCommand?.CanExecute(new IntentSetReorderRequest(draggingSet, draggingSet)) != true)
        {
            e.Action = DragAction.Cancel;
            e.Handled = true;
            ClearSetInsertion();
        }
    }
    private IntentSetReorderRequest? SetRequest(object sender, DragEventArgs e)
    {
        if (sender is not TabItem { DataContext: IntentSetChoice target } header ||
            !ReferenceEquals(DataContext, draggingSetRoot) || draggingSet == null ||
            !e.Data.GetDataPresent(typeof(IntentSetChoice)) || e.Data.GetData(typeof(IntentSetChoice)) is not IntentSetChoice source ||
            !ReferenceEquals(source, draggingSet)) return null;
        return new(source, target, e.GetPosition(header).X >= header.ActualWidth / 2);
    }
    private bool BrowseSetDragEdge(DragEventArgs e)
    {
        if (!ReferenceEquals(DataContext, draggingSetRoot) || draggingSet == null ||
            !e.Data.GetDataPresent(typeof(IntentSetChoice)) || !ReferenceEquals(e.Data.GetData(typeof(IntentSetChoice)), draggingSet) ||
            IntentSetHeaderScroll is not { ViewportWidth: > 0 } scroll) return false;
        var point = e.GetPosition(scroll);
        if (point.Y < 0 || point.Y > scroll.ActualHeight) return false;
        var direction = point.X <= 12 ? -1 : point.X >= scroll.ViewportWidth - 12 ? 1 : 0;
        if (direction == 0 || direction < 0 && scroll.HorizontalOffset <= 0 ||
            direction > 0 && scroll.HorizontalOffset >= scroll.ScrollableWidth) return false;
        // Native DragOver events advance one header at an exposed edge. No timer,
        // persistent reorder state or independent root mutation is introduced.
        BrowseSets(direction);
        return true;
    }
    private void SetHeaderDragOver(object sender, DragEventArgs e)
    {
        var request = SetRequest(sender, e);
        var admitted = request != null && DataContext is PlacerViewModel vm && vm.ReorderIntentSetCommand.CanExecute(request);
        e.Effects = admitted ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
        if (!admitted || request == null || sender is not TabItem header) { ClearSetInsertion(); return; }
        BrowseSetDragEdge(e);
        var edge = header.TranslatePoint(new Point(request.After ? header.ActualWidth : 0, 0), IntentSetDropFeedback).X;
        ((TranslateTransform)IntentSetInsertionMarker.RenderTransform).X = Math.Clamp(edge, 0, Math.Max(0, IntentSetDropFeedback.ActualWidth - 2));
        IntentSetInsertionMarker.Visibility = Visibility.Visible;
    }
    private void SetHeaderDrop(object sender, DragEventArgs e)
    {
        var request = SetRequest(sender, e);
        e.Effects = DragDropEffects.None;
        if (request != null && DataContext is PlacerViewModel vm && vm.ReorderIntentSetCommand.CanExecute(request))
        {
            vm.ReorderIntentSetCommand.Execute(request);
            e.Effects = DragDropEffects.Move;
        }
        ClearSetInsertion();
        e.Handled = true;
    }
    private void SetStripDragOver(object sender, DragEventArgs e)
    {
        if (!BrowseSetDragEdge(e)) ClearSetInsertion();
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }
    private void SetStripDrop(object sender, DragEventArgs e)
    {
        // Blank strip/arrow drops have no target; only a real header can reorder.
        ClearSetInsertion(); e.Effects = DragDropEffects.None; e.Handled = true;
    }
    private void SetStripDragLeave(object sender, DragEventArgs e)
    {
        var point = e.GetPosition(IntentSetRow);
        if (!new Rect(IntentSetRow.RenderSize).Contains(point)) ClearSetInsertion();
    }

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
