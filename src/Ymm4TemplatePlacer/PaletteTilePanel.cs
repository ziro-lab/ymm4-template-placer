using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;
// Equal 104-DIP cells preserve existing Auto geometry. Fixed width is allowed to
// overflow horizontally; resizing can never change slot-to-row/column mapping.
public sealed class PaletteTilePanel : Panel
{
    public static readonly DependencyProperty LayoutModeProperty = DependencyProperty.Register(nameof(LayoutMode), typeof(PaletteLayoutMode), typeof(PaletteTilePanel),
        new FrameworkPropertyMetadata(PaletteLayoutMode.Auto, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty FixedColumnsProperty = DependencyProperty.Register(nameof(FixedColumns), typeof(int), typeof(PaletteTilePanel),
        new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty ViewportWidthProperty = DependencyProperty.Register(nameof(ViewportWidth), typeof(double), typeof(PaletteTilePanel),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public PaletteLayoutMode LayoutMode { get => (PaletteLayoutMode)GetValue(LayoutModeProperty); set => SetValue(LayoutModeProperty, value); }
    public int FixedColumns { get => (int)GetValue(FixedColumnsProperty); set => SetValue(FixedColumnsProperty, value); }
    public double ViewportWidth { get => (double)GetValue(ViewportWidthProperty); set => SetValue(ViewportWidthProperty, value); }
    internal int ColumnCount => LayoutMode == PaletteLayoutMode.Fixed ? Math.Clamp(FixedColumns, 1, 16) :
        Math.Max(1, (int)(double.IsFinite(ViewportWidth) ? Math.Floor(Math.Max(0, ViewportWidth) / 104) : 1));
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (UIElement child in InternalChildren) child.Measure(new Size(104, 104));
        return new Size(InternalChildren.Count == 0 ? 0 : ColumnCount * 104, Math.Ceiling((double)InternalChildren.Count / ColumnCount) * 104);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = ColumnCount;
        for (var i = 0; i < InternalChildren.Count; i++) InternalChildren[i].Arrange(new Rect(i % columns * 104, i / columns * 104, 104, 104));
        return finalSize;
    }
}
