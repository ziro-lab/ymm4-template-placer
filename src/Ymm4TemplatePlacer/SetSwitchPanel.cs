using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

// The small Set selector is navigation, not a tile grid. Equal cells keep
// long names from changing neighboring widths; narrow hosts reflow vertically.
public sealed class SetSwitchPanel : Panel
{
    private const double PreferredCellWidth = 140;
    private const double CellHeight = 46;
    private int Columns(double width) => Math.Min(Math.Max(1, InternalChildren.Count),
        double.IsFinite(width) ? Math.Max(1, (int)Math.Floor(width / PreferredCellWidth)) : 2);

    protected override Size MeasureOverride(Size availableSize)
    {
        var columns = Columns(availableSize.Width);
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : columns * PreferredCellWidth;
        foreach (UIElement child in InternalChildren) child.Measure(new Size(width / columns, CellHeight));
        return new Size(width, Math.Ceiling((double)InternalChildren.Count / columns) * CellHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Columns(finalSize.Width);
        var width = finalSize.Width / columns;
        for (var i = 0; i < InternalChildren.Count; i++)
            InternalChildren[i].Arrange(new Rect(i % columns * width, i / columns * CellHeight, width, CellHeight));
        return finalSize;
    }
}
