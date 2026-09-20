using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;

/// <summary>Local, direction-aware wheel ownership within one Settings scroller.</summary>
public static class NestedWheelRouting
{
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
        if (!e.Handled && sender is ScrollViewer root &&
            TryScroll(root, e.OriginalSource as DependencyObject, e.Delta, Keyboard.Modifiers))
            e.Handled = true;
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
        var target = viewers.FirstOrDefault(viewer => viewer.IsEnabled && viewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
            (delta > 0 ? viewer.VerticalOffset > 0.01 : viewer.VerticalOffset + 0.01 < viewer.ScrollableHeight));
        if (target == null) return false;

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
