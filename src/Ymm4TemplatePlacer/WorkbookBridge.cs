using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Ymm4TemplatePlacer;

// Deliberately a one-way snapshot bridge, not a synchronization protocol.
public static class WorkbookBridge
{
    private const int MaxRows = 50000;
    private const string Schema = "YMM4 Template Placer/1";
    private static readonly string[] Headers = ["No", "Character", "Frame", "Length", "Serif", "Template"];
    private static readonly string[] SnapshotHeaders = ["No", "Character", "Frame", "Length", "Serif", "Layer"];
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static void Export(string path, string sceneName, IReadOnlyList<AssignmentRow> rows, IReadOnlyList<FaceTemplate> catalog)
    {
        if (rows.Count > MaxRows || catalog.Count > MaxRows) throw Bad("一度に扱えるVoice / Templateは50,000件までです。");
        var templates = catalog.OrderBy(x => x.Character, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
        if (templates.GroupBy(x => (x.Character, x.Name)).Any(x => x.Count() != 1))
            throw Bad("同じCharacterに同名Templateが複数あります。名前を区別して登録し直してください。");
        foreach (var row in rows)
        {
            if (row.SelectedChoice.Template is FaceTemplate chosen && !templates.Any(x => ReferenceEquals(x.Template, chosen.Template) && x.Name == chosen.Name && x.Character == row.Character))
                throw Bad("選択Templateが変更されています。［更新］して選び直してください。");
            foreach (var text in new[] { row.Character, row.Serif, row.SelectedChoice.Template?.Name ?? "" }) ValidateText(text);
        }
        var full = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(full)!, "." + Path.GetFileName(full) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var document = SpreadsheetDocument.Create(temporary, SpreadsheetDocumentType.Workbook))
            {
                var book = document.AddWorkbookPart();
                book.Workbook = new Workbook(new BookViews(new WorkbookView { ActiveTab = 0U }), new Sheets());
                book.AddNewPart<WorkbookStylesPart>().Stylesheet = CreateStyles();
                var data = AddSheet(book, "Assignments", false, [6, 16, 11, 11, 60, 34]);
                WriteRows(data, [Headers], true);
                for (var i = 0; i < rows.Count; i++)
                {
                    var x = rows[i];
                    var r = new Row { RowIndex = (uint)i + 2, Height = 36, CustomHeight = true };
                    var values = new[] { (i + 1).ToString(Invariant), x.Character, x.Frame.ToString(Invariant), x.Length.ToString(Invariant), x.Serif, x.SelectedChoice.Template?.Name ?? "" };
                    for (var c = 0; c < values.Length; c++) r.Append(CellAt(c, i + 2, values[c], c == 5 ? 3U : 2U, c is 0 or 2 or 3));
                    data.GetFirstChild<SheetData>()!.Append(r);
                }
                data.Append(new AutoFilter { Reference = $"A1:F{rows.Count + 1}" });
                var characterNames = rows.Select(x => x.Character).Concat(templates.Select(x => x.Character)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                var definitions = new DefinedNames();
                book.Workbook.Append(definitions);
                var ranges = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var group in templates.Select((x, i) => (Template: x, Index: i + 2)).GroupBy(x => x.Template.Character))
                {
                    var name = "tpl_group_" + (ranges.Count + 1).ToString("D4", Invariant);
                    ranges[group.Key] = name;
                    definitions.Append(new DefinedName { Name = name, Text = $"'_Catalog'!$C${group.First().Index}:$C${group.Last().Index}" });
                }
                definitions.Append(new DefinedName { Name = "tpl_empty", Text = "'_Catalog'!$F$2:$F$2" });
                definitions.Append(new DefinedName { Name = "tpl_chars", Text = $"'_Catalog'!$D$2:$D${Math.Max(2, characterNames.Length + 1)}" });
                definitions.Append(new DefinedName { Name = "tpl_lists", Text = $"'_Catalog'!$E$2:$E${Math.Max(2, characterNames.Length + 1)}" });
                var hiddenCatalog = AddSheet(book, "_Catalog", true, [14, 20, 44, 20, 24, 10]);
                var catalogRows = new List<string[]> { ["TemplateId", "Character", "TemplateName", "Character lookup", "Named range", "Empty"] };
                for (var i = 0; i < Math.Max(1, Math.Max(templates.Length, characterNames.Length)); i++)
                    catalogRows.Add([
                        i < templates.Length ? "T" + (i + 1).ToString("D4", Invariant) : "",
                        i < templates.Length ? templates[i].Character : "", i < templates.Length ? templates[i].Name : "",
                        i < characterNames.Length ? characterNames[i] : "",
                        i < characterNames.Length ? ranges.GetValueOrDefault(characterNames[i], "tpl_empty") : "", ""]);
                WriteRows(hiddenCatalog, catalogRows);
                if (rows.Count > 0)
                {
                    var validations = new DataValidations { Count = (uint)rows.Count };
                    for (var i = 0; i < rows.Count; i++) validations.Append(new DataValidation(new Formula1($"INDIRECT(IFERROR(INDEX(tpl_lists,MATCH($B{i + 2},tpl_chars,0)),\"tpl_empty\"))"))
                    {
                        Type = DataValidationValues.List, AllowBlank = true, ShowDropDown = false,
                        ShowInputMessage = true, PromptTitle = "表情Template", Prompt = "同じCharacterの候補から選択。空欄は配置しません。",
                        ShowErrorMessage = true, ErrorStyle = DataValidationErrorStyleValues.Stop,
                        ErrorTitle = "候補から選択してください", Error = "同じCharacterのTemplateを選ぶか、空欄にしてください。",
                        SequenceOfReferences = new ListValue<StringValue> { InnerText = $"F{i + 2}" }
                    });
                    data.Append(validations);
                }
                var snapshot = AddSheet(book, "_Snapshot", true, [8, 18, 12, 12, 60, 12]);
                WriteRows(snapshot, new[] { SnapshotHeaders }.Concat(rows.Select((x, i) => new[] { (i + 1).ToString(Invariant), x.Character, x.Frame.ToString(Invariant), x.Length.ToString(Invariant), x.Serif, x.Target.Layer.ToString(Invariant) })));
                WriteRows(AddSheet(book, "_Meta", true, [24, 60]), new[] { new[] { "Schema", Schema }, new[] { "Scene", sceneName }, new[] { "Rows", rows.Count.ToString(Invariant) } });
                WriteRows(AddSheet(book, "使い方", false, [25, 85]), new[]
                {
                    new[] { "YMM4 Template Placer", "Assignment Snapshot" },
                    new[] { "1. 編集", "AssignmentsのTemplate列をプルダウンで選択します。空欄の行には配置しません。" },
                    new[] { "2. 読み込み", "保存してPluginの［Excelから読み込み］を実行。まだTimelineは変更しません。" },
                    new[] { "3. 配置", "一覧を確認して［配置］。このSceneのCWT_TPL:face表情だけを置き換えます。" },
                    new[] { "編集する列", "Template列だけです。他の列、行数、非表示シートは変更しないでください。" },
                    new[] { "YMM4側を変更したら", "VoiceやTemplateを変更したら再出力してください。自動同期・差分マージは行いません。" },
                    new[] { "配置Layer", "登録Template内の表情ItemのLayerを使います。重なりは配置前に停止します。" },
                    new[] { "Template名", "YMM4の登録名（フォルダを含む）を使います。同名の曖昧な候補は読み込めません。" }
                });
                book.Workbook.Save();
            }
            File.Move(temporary, full, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static IReadOnlyList<AssignmentRow> Import(string path, string sceneName, IReadOnlyList<VoiceSnapshot> voices, IReadOnlyList<FaceTemplate> catalog)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 16 * 1024 * 1024) throw Bad("Workbookが16 MiBを超えています。");
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            if (zip.Entries.Count > 256 || zip.Entries.Sum(x => x.Length) > 128L * 1024 * 1024 ||
                zip.Entries.Any(x => x.Length > 64L * 1024 * 1024) || zip.Entries.Select(x => x.FullName).Distinct(StringComparer.Ordinal).Count() != zip.Entries.Count)
                throw Bad("Workbookが大きすぎるか、ZIP構造が不正です。");
        }
        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, false, new OpenSettings { MaxCharactersInPart = 64L * 1024 * 1024 });
        if (document.DocumentType != SpreadsheetDocumentType.Workbook) throw Bad("通常の.xlsxを読み込んでください。マクロ付きWorkbookは対応していません。");
        var book = document.WorkbookPart ?? throw Bad("Workbookがありません。");
        var strings = book.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().Select(RichText).ToArray() ?? [];
        var meta = ReadRows(GetSheet(book, "_Meta"), strings, 2);
        if (meta.Count != 3 || meta[0][0] != "Schema" || meta[0][1] != Schema || meta[1][0] != "Scene" || meta[1][1] != sceneName || meta[2][0] != "Rows" || Integer(meta[2][1]) != voices.Count)
            throw Bad("このSceneのSnapshotと一致しません。対象Sceneを確認し、Excelを再出力してください。");
        var snapshot = ReadRows(GetSheet(book, "_Snapshot"), strings, 6);
        var main = ReadRows(GetSheet(book, "Assignments"), strings, 6);
        if (voices.Count > MaxRows || snapshot.Count != voices.Count + 1 || main.Count != voices.Count + 1 ||
            !snapshot[0].SequenceEqual(SnapshotHeaders) || !main[0].SequenceEqual(Headers))
            throw Bad("行数・見出しが変更されています。Excelを再出力し、Template列だけを編集してください。");
        var exportedCatalog = ReadRows(GetSheet(book, "_Catalog"), strings, 3);
        if (exportedCatalog.Count == 0 || !exportedCatalog[0].SequenceEqual(new[] { "TemplateId", "Character", "TemplateName" })) throw Bad("Template Catalogが不正です。");
        var validNames = new HashSet<(string Character, string Name)>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in exportedCatalog.Skip(1).Where(x => x.Any(y => y.Length > 0)))
            if (entry[0].Length == 0 || !ids.Add(entry[0]) || !validNames.Add((entry[1], entry[2]))) throw Bad("Template Catalogに重複または不正な行があります。");
        var byNo = new Dictionary<int, string[]>();
        foreach (var row in main.Skip(1))
        {
            var no = Integer(row[0]);
            if (no < 1 || no > voices.Count || !byNo.TryAdd(no, row)) throw Bad("Noが重複・変更されています。Excelを再出力してください。");
        }
        var result = new List<AssignmentRow>();
        for (var i = 0; i < voices.Count; i++)
        {
            var voice = voices[i]; var expected = snapshot[i + 1]; var row = byNo[i + 1];
            if (Integer(expected[0]) != i + 1 || expected[1] != voice.Character || Integer(expected[2]) != voice.Frame ||
                Integer(expected[3]) != voice.Length || expected[4] != voice.Serif || Integer(expected[5]) != voice.Layer || !row.Take(5).SequenceEqual(expected.Take(5)))
                throw Bad($"No {i + 1}: VoiceまたはSnapshotが変更されています。Excelを再出力してください。");
            var assignment = new AssignmentRow(i + 1, voice, catalog);
            if (row[5].Length > 0)
            {
                if (!validNames.Contains((voice.Character, row[5]))) throw Bad($"No {i + 1}: このCharacterの候補ではありません: {row[5]}");
                var matches = assignment.Choices.Where(x => x.Template?.Name == row[5]).ToArray();
                if (matches.Length != 1) throw Bad($"No {i + 1}: 参照Templateが削除・変更されているか、同名で曖昧です: {row[5]}。登録を確認して再出力してください。");
                assignment.SelectedChoice = matches[0];
            }
            result.Add(assignment);
        }
        return result;
    }

    private static Worksheet AddSheet(WorkbookPart book, string name, bool hidden, double[] widths)
    {
        var part = book.AddNewPart<WorksheetPart>();
        var data = new SheetData();
        part.Worksheet = new Worksheet(new SheetViews(new SheetView(new Pane { VerticalSplit = 1, TopLeftCell = "A2", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen }) { WorkbookViewId = 0U }),
            new SheetFormatProperties { DefaultRowHeight = 24 },
            new Columns(widths.Select((width, i) => new Column { Min = (uint)i + 1, Max = (uint)i + 1, Width = width, CustomWidth = true })), data);
        var sheets = book.Workbook.GetFirstChild<Sheets>()!;
        sheets.Append(new Sheet { Id = book.GetIdOfPart(part), SheetId = (uint)sheets.ChildElements.Count + 1, Name = name, State = hidden ? SheetStateValues.Hidden : SheetStateValues.Visible });
        return part.Worksheet;
    }
    private static void WriteRows(Worksheet sheet, IEnumerable<string[]> rows, bool headerOnly = false)
    {
        var data = sheet.GetFirstChild<SheetData>()!;
        var r = 1;
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
        var matches = book.Workbook.GetFirstChild<Sheets>()?.Elements<Sheet>().Where(x => x.Name?.Value == name).ToArray() ?? [];
        if (matches.Length != 1 || matches[0].Id?.Value is not string id || book.GetPartById(id) is not WorksheetPart part)
            throw Bad($"必要なシートがありません: {name}。Excelを再出力してください。");
        return part.Worksheet;
    }
    private static List<string[]> ReadRows(Worksheet sheet, string[] shared, int columns)
    {
        var result = new List<string[]>();
        var data = sheet.GetFirstChild<SheetData>() ?? throw Bad("シートのデータがありません。");
        uint last = 0;
        foreach (var row in data.Elements<Row>())
        {
            if (result.Count > MaxRows + 1) throw Bad("行数が上限を超えています。");
            var index = row.RowIndex?.Value ?? last + 1;
            if (index <= last || index > MaxRows + 2) throw Bad("行番号が不正です。");
            var values = Enumerable.Repeat("", columns).ToArray();
            var used = new HashSet<int>();
            foreach (var cell in row.Elements<Cell>())
            {
                var reference = cell.CellReference?.Value ?? throw Bad("セル番地がありません。");
                var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
                if (letters.Length != 1 || letters[0] < 'A' || letters[0] >= 'A' + columns) continue;
                var c = letters[0] - 'A';
                if (!used.Add(c) || reference[letters.Length..] != index.ToString(Invariant)) throw Bad("セル番地が重複または不正です。");
                if (cell.CellFormula != null) throw Bad($"{reference}: 数式は読み込めません。Templateを候補の文字列として保存してください。");
                string text;
                if (cell.DataType?.Value == CellValues.SharedString)
                {
                    var n = Integer(cell.CellValue?.Text ?? "");
                    if (n < 0 || n >= shared.Length) throw Bad("共有文字列の参照が不正です。");
                    text = shared[n];
                }
                else if (cell.DataType?.Value == CellValues.InlineString) text = cell.InlineString == null ? "" : RichText(cell.InlineString);
                else if (cell.DataType?.Value == CellValues.Error) throw Bad("Workbookにエラー値があります。");
                else text = cell.CellValue?.Text ?? "";
                ValidateText(text); values[c] = text;
            }
            result.Add(values); last = index;
        }
        return result;
    }
    private static string RichText(OpenXmlElement value) => string.Concat(value.Descendants<Text>().Where(x => !x.Ancestors<PhoneticRun>().Any()).Select(x => x.Text));
    private static int Integer(string text) => int.TryParse(text, NumberStyles.Integer, Invariant, out var value) ? value : throw Bad("No / Frame / Length / Layerに整数以外の値があります。");
    private static void ValidateText(string text)
    {
        if (text.Length > 32767) throw Bad("セルの文字数がExcelの上限を超えています。");
        XmlConvert.VerifyXmlChars(text);
    }
    private static InvalidDataException Bad(string message) => new(message);
    private static Stylesheet CreateStyles() => new(
        new Fonts(new Font(new FontSize { Val = 10 }, new FontName { Val = "Yu Gothic" }), new Font(new Bold(), new Color { Rgb = "FFFFFFFF" }, new FontSize { Val = 10 }, new FontName { Val = "Yu Gothic" }), new Font(new Color { Rgb = "FF165CA0" }, new FontSize { Val = 10 }, new FontName { Val = "Yu Gothic" })) { Count = 3U },
        new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 }), new Fill(new PatternFill(new ForegroundColor { Rgb = "FF334E68" }, new BackgroundColor { Indexed = 64U }) { PatternType = PatternValues.Solid }), new Fill(new PatternFill(new ForegroundColor { Rgb = "FFEAF3FA" }, new BackgroundColor { Indexed = 64U }) { PatternType = PatternValues.Solid })) { Count = 4U },
        new Borders(new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder())) { Count = 1U },
        new CellStyleFormats(new CellFormat { FontId = 0U, FillId = 0U, BorderId = 0U }) { Count = 1U },
        new CellFormats(new CellFormat { FontId = 0U, FillId = 0U, BorderId = 0U },
            new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center }) { FontId = 1U, FillId = 2U, BorderId = 0U, ApplyFont = true, ApplyFill = true, ApplyAlignment = true },
            new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }) { FontId = 0U, FillId = 0U, BorderId = 0U, ApplyAlignment = true },
            new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }) { FontId = 2U, FillId = 3U, BorderId = 0U, ApplyFont = true, ApplyFill = true, ApplyAlignment = true }) { Count = 4U },
        new CellStyles(new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U }) { Count = 1U });
}
