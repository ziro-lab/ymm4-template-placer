using System.Globalization;
using System.IO;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Ymm4TemplatePlacer;

// A one-way snapshot bridge, deliberately not a synchronization protocol.
public static partial class WorkbookBridge
{
    private const int MaxRows = 50000;
    private const string Schema = "YMM4 Template Placer/1";
    private static readonly string[] Headers = ["No", "Character", "Frame", "Length", "Serif", "Template"];
    private static readonly string[] SnapshotHeaders = ["No", "Character", "Frame", "Length", "Serif", "Layer"];
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static Worksheet AddSheet(WorkbookPart book, string name, bool hidden, double[] widths)
    {
        var part = book.AddNewPart<WorksheetPart>();
        var worksheet = new Worksheet(new SheetViews(new SheetView(new Pane { VerticalSplit = 1, TopLeftCell = "A2", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen }) { WorkbookViewId = 0U }),
            new SheetFormatProperties { DefaultRowHeight = 24 },
            new Columns(widths.Select((width, i) => new Column { Min = (uint)i + 1, Max = (uint)i + 1, Width = width, CustomWidth = true })), new SheetData());
        part.Worksheet = worksheet;
        var workbook = book.Workbook ?? throw Bad("Excelファイルのブック情報がありません。");
        var sheets = workbook.GetFirstChild<Sheets>() ?? throw Bad("Excelファイルにシート一覧がありません。");
        sheets.Append(new Sheet { Id = book.GetIdOfPart(part), SheetId = (uint)sheets.ChildElements.Count + 1, Name = name, State = hidden ? SheetStateValues.Hidden : SheetStateValues.Visible });
        return worksheet;
    }
    private static void WriteRows(Worksheet sheet, IEnumerable<string[]> rows)
    {
        var data = sheet.GetFirstChild<SheetData>()!; var r = 1;
        foreach (var values in rows)
        {
            var row = new Row { RowIndex = (uint)r, Height = r == 1 ? 26 : 36, CustomHeight = true };
            for (var c = 0; c < values.Length; c++) row.Append(CellAt(c, r, values[c], r == 1 ? 1U : 2U, false));
            data.Append(row); r++;
        }
    }
    private static Cell CellAt(int column, int row, string value, uint style, bool number)
    {
        ValidateText(value);
        var cell = new Cell { CellReference = $"{(char)('A' + column)}{row}", StyleIndex = style, DataType = number ? CellValues.Number : CellValues.InlineString };
        if (number) cell.CellValue = new CellValue(value);
        else cell.InlineString = new InlineString(new Text(value) { Space = SpaceProcessingModeValues.Preserve });
        return cell;
    }
    private static Worksheet GetSheet(WorkbookPart book, string name)
    {
        var workbook = book.Workbook ?? throw Bad("Excelファイルのブック情報がありません。");
        var matches = workbook.GetFirstChild<Sheets>()?.Elements<Sheet>().Where(x => x.Name?.Value == name).ToArray() ?? [];
        if (matches.Length != 1 || matches[0].Id?.Value is not string id || book.GetPartById(id) is not WorksheetPart part)
            throw Bad($"必要なシートがありません: {name}。Excelを再出力してください。");
        return part.Worksheet ?? throw Bad($"シートのデータがありません: {name}");
    }
    private static List<string[]> ReadRows(Worksheet sheet, string[] shared, int columns)
    {
        var result = new List<string[]>(); var data = sheet.GetFirstChild<SheetData>() ?? throw Bad("シートのデータがありません。"); uint last = 0;
        foreach (var row in data.Elements<Row>())
        {
            if (result.Count > MaxRows + 1) throw Bad("行数が上限を超えています。");
            var index = row.RowIndex?.Value ?? last + 1;
            if (index <= last || index > MaxRows + 2) throw Bad("行番号が不正です。");
            var values = Enumerable.Repeat("", columns).ToArray(); var used = new HashSet<int>();
            foreach (var cell in row.Elements<Cell>())
            {
                var reference = cell.CellReference?.Value ?? throw Bad("セル番地がありません。");
                var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
                if (letters.Length != 1 || letters[0] < 'A' || letters[0] >= 'A' + columns) continue;
                var c = letters[0] - 'A';
                if (!used.Add(c) || reference[letters.Length..] != index.ToString(Invariant)) throw Bad("セル番地が重複または不正です。");
                if (cell.CellFormula != null) throw Bad($"{reference}: 数式は読み込めません。テンプレートを候補の文字列として保存してください。");
                string text;
                if (cell.DataType?.Value == CellValues.SharedString)
                {
                    var n = Integer(cell.CellValue?.Text ?? "");
                    if (n < 0 || n >= shared.Length) throw Bad("共有文字列の参照が不正です。"); text = shared[n];
                }
                else if (cell.DataType?.Value == CellValues.InlineString) text = cell.InlineString == null ? "" : RichText(cell.InlineString);
                else if (cell.DataType?.Value == CellValues.Error) throw Bad("Excelファイルにエラー値があります。");
                else text = cell.CellValue?.Text ?? "";
                ValidateText(text); values[c] = text;
            }
            result.Add(values); last = index;
        }
        while (result.Count > 0 && result[^1].All(string.IsNullOrEmpty)) result.RemoveAt(result.Count - 1);
        return result;
    }
    private static string RichText(OpenXmlElement value) => string.Concat(value.Descendants<Text>().Where(x => !x.Ancestors<PhoneticRun>().Any()).Select(x => x.Text));
    private static int Integer(string text) => int.TryParse(text, NumberStyles.Integer, Invariant, out var value) ? value : throw Bad("No / 開始 / 長さ / レイヤーに整数以外の値があります。");
    private static void ValidateText(string text)
    {
        if (text.Length > 32767) throw Bad("セルの文字数がExcelの上限を超えています。"); XmlConvert.VerifyXmlChars(text);
    }
    private static InvalidDataException Bad(string message) => new(message);
}
