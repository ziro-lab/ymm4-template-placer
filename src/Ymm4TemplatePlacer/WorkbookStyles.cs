using DocumentFormat.OpenXml.Spreadsheet;

namespace Ymm4TemplatePlacer;

public static partial class WorkbookBridge
{
    private static Font SheetFont(bool bold = false, string? color = null)
    {
        // Typed setters maintain the schema-defined font child order.
        var font = new Font { FontSize = new FontSize { Val = 10 }, FontName = new FontName { Val = "Yu Gothic" } };
        if (bold) font.Bold = new Bold();
        if (color != null) font.Color = new Color { Rgb = color };
        return font;
    }
    private static Stylesheet CreateStyles() => new(
        new Fonts(SheetFont(), SheetFont(true, "FFFFFFFF"), SheetFont(false, "FF165CA0")) { Count = 3U },
        new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 }), new Fill(new PatternFill(new ForegroundColor { Rgb = "FF334E68" }, new BackgroundColor { Indexed = 64U }) { PatternType = PatternValues.Solid }), new Fill(new PatternFill(new ForegroundColor { Rgb = "FFEAF3FA" }, new BackgroundColor { Indexed = 64U }) { PatternType = PatternValues.Solid })) { Count = 4U },
        new Borders(new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder())) { Count = 1U },
        new CellStyleFormats(new CellFormat { FontId = 0U, FillId = 0U, BorderId = 0U }) { Count = 1U },
        new CellFormats(new CellFormat { FontId = 0U, FillId = 0U, BorderId = 0U },
            new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center }) { FontId = 1U, FillId = 2U, BorderId = 0U, ApplyFont = true, ApplyFill = true, ApplyAlignment = true },
            new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }) { FontId = 0U, FillId = 0U, BorderId = 0U, ApplyAlignment = true },
            new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }) { FontId = 2U, FillId = 3U, BorderId = 0U, ApplyFont = true, ApplyFill = true, ApplyAlignment = true }) { Count = 4U },
        new CellStyles(new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U }) { Count = 1U });
}
