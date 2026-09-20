using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;
// Auto keeps the established 104-DIP geometry. Fixed preserves exact column/slot
// mapping but may grow square cells to consume a wider viewport; cells never shrink
// below 104 DIP, so narrow panes retain horizontal overflow instead of tiny tiles.
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
    internal double CellSize
    {
        get
        {
            if (LayoutMode != PaletteLayoutMode.Fixed || !double.IsFinite(ViewportWidth) || ViewportWidth <= 0) return 104;
            return Math.Max(104, ViewportWidth / ColumnCount);
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var cell = CellSize; var columns = ColumnCount;
        foreach (UIElement child in InternalChildren) child.Measure(new Size(cell, cell));
        return new Size(InternalChildren.Count == 0 ? 0 : columns * cell,
            Math.Ceiling((double)InternalChildren.Count / columns) * cell);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = ColumnCount; var cell = CellSize;
        for (var i = 0; i < InternalChildren.Count; i++)
            InternalChildren[i].Arrange(new Rect(i % columns * cell, i / columns * cell, cell, cell));
        return finalSize;
    }
}
