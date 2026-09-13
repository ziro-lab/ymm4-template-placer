using System.Globalization;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyWorkbookVariants(Timeline timeline)
    {
        stage = "P6 workbook variants";
        var vm = ViewModel ?? throw new InvalidOperationException("Native ViewModel missing");
        var baseline = Path.Combine(output, "GoldenPath.xlsx");
        var variant = Path.Combine(output, "SharedStringsSorted.xlsx");
        File.Copy(baseline, variant, true);
        using (var document = SpreadsheetDocument.Open(variant, true))
        {
            var book = document.WorkbookPart!;
            var strings = new SharedStringTable();
            book.AddNewPart<SharedStringTablePart>().SharedStringTable = strings;
            var index = 0;
            foreach (var part in book.WorksheetParts)
            {
                var worksheet = part.Worksheet ?? throw new InvalidDataException("Fixture worksheet missing");
                foreach (var cell in worksheet.Descendants<Cell>().Where(x => x.DataType?.Value == CellValues.InlineString))
                {
                    var text = string.Concat(cell.InlineString!.Descendants<Text>().Select(x => x.Text));
                    strings.Append(new SharedStringItem(new Text(text)));
                    cell.InlineString = null;
                    cell.DataType = CellValues.SharedString;
                    cell.CellValue = new CellValue((index++).ToString(CultureInfo.InvariantCulture));
                }
                worksheet.Save();
            }
            strings.Save();
            var main = Sheet(book, "Assignments");
            var data = main.GetFirstChild<SheetData>()!;
            var reversed = data.Elements<Row>().Skip(1).Reverse().ToArray();
            foreach (var row in reversed) row.Remove();
            uint rowNumber = 2;
            foreach (var row in reversed)
            {
                row.RowIndex = rowNumber;
                foreach (var cell in row.Elements<Cell>())
                    cell.CellReference = new string(cell.CellReference!.Value!.TakeWhile(char.IsLetter).ToArray()) + rowNumber.ToString(CultureInfo.InvariantCulture);
                data.Append(row); rowNumber++;
            }
            main.Save();
        }
        var before = Signature(timeline);
        vm.ImportFrom(variant);
        Assert(vm.Rows[0].SelectedChoice.Template?.Name == "TestA/Smile" && vm.Rows[1].SelectedChoice.Template?.Name == "TestB/Neutral" && vm.Rows[0].Frame == 10,
            "P6 shared-string cells and sorted rows resolve by snapshot No");
        Assert(Signature(timeline) == before, "P6 shared-string import leaves Timeline unchanged");
        EditCell(variant, "F4", "");
        vm.ImportFrom(variant);
        Assert(vm.Rows[0].SelectedChoice.Template == null && vm.Rows[0].HasCandidates && Signature(timeline) == before,
            "P6 empty Template imports as no placement, not an error");
        vm.ImportFrom(baseline);
    }
}
