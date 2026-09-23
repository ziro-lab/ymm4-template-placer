using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;

/// <summary>Local, direction-aware wheel ownership within one Settings scroller.</summary>
public static class NestedWheelRouting
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out CursorPoint point);

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(NestedWheelRouting), new PropertyMetadata(false, EnabledChanged));
    public static bool GetEnabled(DependencyObject value) => (bool)value.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject value, bool enabled) => value.SetValue(EnabledProperty, enabled);

    private static void EnabledChanged(DependencyObject value, DependencyPropertyChangedEventArgs e)
    {
        if (value is not ScrollViewer viewer) return;
        viewer.PreviewMouseWheel -= Wheel;
        if (e.NewValue is true) viewer.PreviewMouseWheel += Wheel;
    }
    private static void Wheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not ScrollViewer root) return;

        // Wheel input can outpace WPF mouse-over/event-source refresh while the user
        // crosses an inner-control boundary. Read the physical cursor at handling time
        // and hit-test the live visual tree there. Never revive a stale OriginalSource.
        var source = ResolveCurrentSource(root);
        if (source != null && TryScroll(root, source, e.Delta, Keyboard.Modifiers)) e.Handled = true;
    }

    internal static DependencyObject? ResolveCurrentSource(ScrollViewer root)
    {
        if (!GetCursorPos(out var cursor) || !root.IsVisible || root.ActualWidth <= 0 || root.ActualHeight <= 0) return null;
        try { return ResolveCurrentSource(root, root.PointFromScreen(new Point(cursor.X, cursor.Y))); }
        catch (InvalidOperationException) { return null; }
    }

    internal static DependencyObject? ResolveCurrentSource(ScrollViewer root, Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X > root.ActualWidth || point.Y > root.ActualHeight) return null;
        return root.InputHitTest(point) as DependencyObject ?? root;
    }

    internal static bool TryScrollFromHost(FrameworkElement host, ScrollViewer root, int delta, ModifierKeys modifiers)
    {
        if (!GetCursorPos(out var cursor) || !host.IsVisible || !root.IsVisible ||
            host.ActualWidth <= 0 || host.ActualHeight <= 0 || root.ActualWidth <= 0 || root.ActualHeight <= 0) return false;

        try
        {
            var screen = new Point(cursor.X, cursor.Y);
            var rootPoint = root.PointFromScreen(screen);
            if (rootPoint.X >= 0 && rootPoint.Y >= 0 && rootPoint.X <= root.ActualWidth && rootPoint.Y <= root.ActualHeight)
                return false; // The existing root PreviewMouseWheel route remains authoritative inside SettingsScroll.

            var hostPoint = host.PointFromScreen(screen);
            if (hostPoint.X < 0 || hostPoint.Y < 0 || hostPoint.X > host.ActualWidth || hostPoint.Y > host.ActualHeight)
                return false;

            var source = host.InputHitTest(hostPoint) as DependencyObject;
            var depth = 0;
            for (var current = source; current != null && depth++ < 64; current = TimelinePointerIntentClassifier.Parent(current))
            {
                // Keep the same explicit wheel owners as the in-scroll route.
                if (current is ComboBox or RangeBase) return false;
                if (current is ScrollViewer other && !ReferenceEquals(other, root) && CanScroll(other, delta)) return false;
                if (ReferenceEquals(current, host)) break;
            }

            return TryScrollTarget(root, delta, modifiers);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
    internal static bool TryScroll(ScrollViewer root, DependencyObject? source, int delta, ModifierKeys modifiers)
    {
        if (delta == 0 || modifiers != ModifierKeys.None || SystemParameters.WheelScrollLines == 0) return false;
        var viewers = new List<ScrollViewer>();
        var inside = false; var depth = 0;
        for (var current = source; current != null && depth++ < 64; current = TimelinePointerIntentClassifier.Parent(current))
        {
            // A combo/numeric slider owns its own wheel behavior; never convert
            // that operation into Settings scrolling. Popup trees are out of scope.
            if (current is ComboBox or RangeBase) return false;
            if (current is ScrollViewer viewer) viewers.Add(viewer);
            if (ReferenceEquals(current, root)) { inside = true; break; }
        }
        if (!inside) return false;
        var target = viewers.FirstOrDefault(viewer => CanScroll(viewer, delta));
        return target != null && TryScrollTarget(target, delta, modifiers);
    }

    private static bool CanScroll(ScrollViewer viewer, int delta) =>
        viewer.IsEnabled && viewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
        (delta > 0 ? viewer.VerticalOffset > 0.01 : viewer.VerticalOffset + 0.01 < viewer.ScrollableHeight);

    private static bool TryScrollTarget(ScrollViewer target, int delta, ModifierKeys modifiers)
    {
        if (delta == 0 || modifiers != ModifierKeys.None || SystemParameters.WheelScrollLines == 0 || !CanScroll(target, delta))
            return false;

        // Use WPF line/page operations, not guessed pixel sizes: these preserve
        // logical item scrolling as well as physical ScrollViewer scrolling.
        var pages = SystemParameters.WheelScrollLines < 0;
        var count = Math.Clamp((int)Math.Ceiling(Math.Abs((double)delta) / 120 *
            (pages ? 1 : SystemParameters.WheelScrollLines)), 1, pages ? 8 : 48);
        for (var i = 0; i < count; i++)
        {
            if (pages) { if (delta > 0) target.PageUp(); else target.PageDown(); }
            else { if (delta > 0) target.LineUp(); else target.LineDown(); }
        }
        return true;
    }
}
