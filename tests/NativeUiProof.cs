using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml.Packaging;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task SelectInDropdown(PlacerView view, AssignmentRow row, string name)
    {
        view.VoiceGrid.ScrollIntoView(row); view.VoiceGrid.UpdateLayout(); await Idle();
        var container = view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow ?? throw new InvalidOperationException("DataGrid row was not realized.");
        var combo = Descendant<ComboBox>(container) ?? throw new InvalidOperationException("Template ComboBox was not realized.");
        Assert(combo.Items.Count == row.Choices.Count && row.Choices.Skip(1).All(x => x.Template!.Character == row.Character), "dropdown candidates bound to " + row.Character);
        combo.IsDropDownOpen = true; await Idle();
        combo.SelectedItem = row.Choices.Single(x => x.Template?.Name == name); combo.IsDropDownOpen = false; await Idle();
        Assert(row.SelectedChoice.Template?.Name == name, "dropdown two-way selection " + name);
    }
    private static async Task ClickPlace(PlacerView view)
    {
        Assert(view.PlaceButton.IsEnabled, "placement button enabled");
        var peer = new ButtonAutomationPeer(view.PlaceButton);
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
    }
    private static T? Descendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) return item;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) if (Descendant<T>(VisualTreeHelper.GetChild(root, i)) is T child) return child;
        return null;
    }
    private static object? FindToolArea(object main)
    {
        var items = main.GetType().GetProperty("ToolMenuItems")?.GetValue(main) as IEnumerable;
        if (items == null) return null;
        object? Visit(object item, int depth)
        {
            if (depth > 5) return null;
            var type = item.GetType();
            if (type.FullName == "YukkuriMovieMaker.ViewModels.ToolAreaViewModel" &&
                type.GetProperty("ViewModelType")?.GetValue(item) is Type vmType && vmType == typeof(PlacerViewModel)) return item;
            var children = (type.GetProperty("Children")?.GetValue(item) ?? type.GetProperty("Items")?.GetValue(item)) as IEnumerable;
            if (children != null) foreach (var child in children) if (child != null && Visit(child, depth + 1) is { } found) return found;
            return null;
        }
        foreach (var item in items) if (item != null && Visit(item, 0) is { } found) return found;
        return null;
    }
    private static async Task SetToolVisible(object main, bool visible)
    {
        var area = FindToolArea(main) ?? throw new InvalidOperationException("YMM4 Template Placer ToolArea was not found.");
        var type = area.GetType();
        var isVisible = type.GetProperty("IsVisible") ?? throw new InvalidOperationException("ToolArea IsVisible was not found.");
        isVisible.SetValue(area, visible);
        if (visible)
        {
            type.GetProperty("IsSelected")?.SetValue(area, true);
            type.GetProperty("IsActive")?.SetValue(area, true);
        }
        await Idle(); await Task.Delay(100); await Idle();
        Assert(Equals(isVisible.GetValue(area), visible), visible ? "native ToolArea can reopen" : "native ToolArea can hide");
    }
    private static bool OpenTool(object main)
    {
        var items = main.GetType().GetProperty("ToolMenuItems")?.GetValue(main) as IEnumerable; if (items == null) return false;
        bool Visit(object item, int depth)
        {
            if (depth > 5) return false; var type = item.GetType();
            var header = type.GetProperty("Header")?.GetValue(item)?.ToString() ?? type.GetProperty("Title")?.GetValue(item)?.ToString() ?? type.GetProperty("Name")?.GetValue(item)?.ToString() ?? "";
            Log("tool menu: " + type.FullName + " | " + header); DumpType(type);
            if (header.Contains("YMM4 Template Placer", StringComparison.Ordinal))
            {
                var command = type.GetProperty("Command")?.GetValue(item) as ICommand; var parameter = type.GetProperty("CommandParameter")?.GetValue(item);
                if (command?.CanExecute(parameter) == true) { command.Execute(parameter); return true; }
                // YMM4 4.55 exposes tool entries as ToolAreaViewModel, not WPF MenuItem.
                // Toggle the native entry's visibility, then verify the host-created View.
                if (type.FullName == "YukkuriMovieMaker.ViewModels.ToolAreaViewModel" && type.GetProperty("ViewModelType")?.GetValue(item) is Type vmType && vmType == typeof(PlacerViewModel))
                {
                    type.GetProperty("IsVisible")!.SetValue(item, true);
                    type.GetProperty("IsSelected")!.SetValue(item, true);
                    type.GetProperty("IsActive")!.SetValue(item, true);
                    Log("P4 native ToolArea visibility activated"); return true;
                }
            }
            var children = (type.GetProperty("Children")?.GetValue(item) ?? type.GetProperty("Items")?.GetValue(item)) as IEnumerable;
            if (children != null) foreach (var child in children) if (child != null && Visit(child, depth + 1)) return true; return false;
        }
        foreach (var item in items) if (item != null && Visit(item, 0)) return true; return false;
    }
    private static void SaveView(PlacerView view)
    {
        view.UpdateLayout(); Assert(view.ActualWidth > 0 && view.ActualHeight > 0, "native UI has measured visible dimensions");
        var bounds = new Rect(0, 0, view.ActualWidth, view.ActualHeight);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Pbgra32);
        // VisualBrush retains the root visual's layout offset. Sample that exact
        // source rectangle into a local-origin image without rearranging the host UI.
        var visual = new DrawingVisual();
        var offset = VisualTreeHelper.GetOffset(view);
        var sourceBounds = new Rect(offset.X, offset.Y, bounds.Width, bounds.Height);
        using (var drawing = visual.RenderOpen())
            drawing.DrawRectangle(new VisualBrush(view) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = sourceBounds, Stretch = Stretch.Fill }, null, bounds);
        bitmap.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(output, "native-plugin-ui.png")); encoder.Save(file);
    }
    private static S.Worksheet Sheet(WorkbookPart book, string name)
    {
        var workbook = book.Workbook ?? throw new InvalidDataException("Fixture Workbook missing");
        var sheet = workbook.GetFirstChild<S.Sheets>()!.Elements<S.Sheet>().Single(x => x.Name == name);
        var part = (WorksheetPart)book.GetPartById(sheet.Id!.Value!);
        return part.Worksheet ?? throw new InvalidDataException("Fixture Worksheet missing");
    }
    private static void EditCell(string path, string reference, string text, bool formula = false)
    {
        using var document = SpreadsheetDocument.Open(path, true); var sheet = Sheet(document.WorkbookPart!, "Assignments");
        var cell = sheet.Descendants<S.Cell>().Single(x => x.CellReference == reference);
        cell.CellValue = null; cell.CellFormula = null; cell.InlineString = new S.InlineString(new S.Text(text)); cell.DataType = S.CellValues.InlineString;
        if (formula) cell.CellFormula = new S.CellFormula("\"TestA/Smile\""); sheet.Save();
    }
}
