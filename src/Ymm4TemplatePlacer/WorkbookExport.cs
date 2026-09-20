using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Ymm4TemplatePlacer;

public static partial class WorkbookBridge
{
    public static void Export(string path, string sceneName, IReadOnlyList<AssignmentRow> rows, IReadOnlyList<FaceTemplate> catalog)
    {
        if (rows.Count > MaxRows || catalog.Count > MaxRows) throw Bad("一度に扱える音声 / テンプレートは50,000件までです。");
        var templates = catalog.OrderBy(x => x.Character, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
        if (templates.GroupBy(x => (x.Character, x.Name)).Any(x => x.Count() != 1)) throw Bad("同じキャラクターに同名テンプレートが複数あります。名前を区別して登録し直してください。");
        foreach (var row in rows)
        {
            if (row.SelectedChoice.Template is FaceTemplate chosen && !templates.Any(x => ReferenceEquals(x.Template, chosen.Template) && x.Name == chosen.Name && x.Character == row.Character))
                throw Bad("選択したテンプレートが変更されています。メンテナンスで一覧を読み直し、選び直してください。");
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
                var data = AddSheet(book, "Assignments", false, [6, 16, 11, 11, 60, 34]); WriteRows(data, [Headers]);
                for (var i = 0; i < rows.Count; i++)
                {
                    var x = rows[i]; var r = new Row { RowIndex = (uint)i + 2, Height = 36, CustomHeight = true };
                    var values = new[] { (i + 1).ToString(Invariant), x.Character, x.Frame.ToString(Invariant), x.Length.ToString(Invariant), x.Serif, x.SelectedChoice.Template?.Name ?? "" };
                    for (var c = 0; c < values.Length; c++) r.Append(CellAt(c, i + 2, values[c], c == 5 ? 3U : 2U, c is 0 or 2 or 3));
                    data.GetFirstChild<SheetData>()!.Append(r);
                }
                data.Append(new AutoFilter { Reference = $"A1:F{rows.Count + 1}" });
                var characterNames = rows.Select(x => x.Character).Concat(templates.Select(x => x.Character)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                var definitions = new DefinedNames(); book.Workbook.Append(definitions);
                var ranges = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var group in templates.Select((x, i) => (Template: x, Index: i + 2)).GroupBy(x => x.Template.Character))
                {
                    var name = "tpl_group_" + (ranges.Count + 1).ToString("D4", Invariant); ranges[group.Key] = name;
                    definitions.Append(new DefinedName { Name = name, Text = $"'_Catalog'!$C${group.First().Index}:$C${group.Last().Index}" });
                }
                definitions.Append(new DefinedName { Name = "tpl_empty", Text = "'_Catalog'!$F$2:$F$2" });
                definitions.Append(new DefinedName { Name = "tpl_chars", Text = $"'_Catalog'!$D$2:$D${Math.Max(2, characterNames.Length + 1)}" });
                definitions.Append(new DefinedName { Name = "tpl_lists", Text = $"'_Catalog'!$E$2:$E${Math.Max(2, characterNames.Length + 1)}" });
                var hiddenCatalog = AddSheet(book, "_Catalog", true, [14, 20, 44, 20, 24, 10]);
                var catalogRows = new List<string[]> { new[] { "TemplateId", "Character", "TemplateName", "Character lookup", "Named range", "Empty" } };
                for (var i = 0; i < Math.Max(1, Math.Max(templates.Length, characterNames.Length)); i++) catalogRows.Add([
                    i < templates.Length ? "T" + (i + 1).ToString("D4", Invariant) : "", i < templates.Length ? templates[i].Character : "", i < templates.Length ? templates[i].Name : "",
                    i < characterNames.Length ? characterNames[i] : "", i < characterNames.Length ? ranges.GetValueOrDefault(characterNames[i], "tpl_empty") : "", ""]);
                WriteRows(hiddenCatalog, catalogRows);
                if (rows.Count > 0)
                {
                    var validations = new DataValidations { Count = (uint)rows.Count };
                    for (var i = 0; i < rows.Count; i++) validations.Append(new DataValidation(new Formula1($"INDIRECT(IFERROR(INDEX(tpl_lists,MATCH($B{i + 2},tpl_chars,0)),\"tpl_empty\"))"))
                    {
                        Type = DataValidationValues.List, AllowBlank = true, ShowDropDown = false, ShowInputMessage = true,
                        PromptTitle = "表情テンプレート", Prompt = "同じキャラクターの候補から選択。空欄は配置しません。",
                        ShowErrorMessage = true, ErrorStyle = DataValidationErrorStyleValues.Stop, ErrorTitle = "候補から選択してください", Error = "同じキャラクターのテンプレートを選ぶか、空欄にしてください。",
                        SequenceOfReferences = new ListValue<StringValue> { InnerText = $"F{i + 2}" }
                    });
                    data.Append(validations);
                }
                WriteRows(AddSheet(book, "_Snapshot", true, [8, 18, 12, 12, 60, 12]), new[] { SnapshotHeaders }.Concat(rows.Select((x, i) => new[] { (i + 1).ToString(Invariant), x.Character, x.Frame.ToString(Invariant), x.Length.ToString(Invariant), x.Serif, x.Target.Layer.ToString(Invariant) })));
                WriteRows(AddSheet(book, "_Meta", true, [24, 60]), new[] { new[] { "Schema", Schema }, new[] { "Scene", sceneName }, new[] { "Rows", rows.Count.ToString(Invariant) } });
                WriteRows(AddSheet(book, "使い方", false, [25, 85]), new[]
                {
                    new[] { "YMM4 Template Placer", "Assignment Snapshot" },
                    new[] { "1. 編集", "Assignmentsシートのテンプレート列をプルダウンで選択します。空欄の行には配置しません。" },
                    new[] { "2. 読み込み", "保存してプラグインの［Excelから読み込み］を実行します。まだタイムラインは変更しません。" },
                    new[] { "3. 配置", "一覧を確認して［配置］。未選択は何もせず、既存アイテムは削除しません。" },
                    new[] { "編集する列", "テンプレート列だけです。他の列、行数、非表示シートは変更しないでください。" },
                    new[] { "YMM4側を変更したら", "音声やテンプレートを変更したら再出力してください。自動同期・差分マージは行いません。" },
                    new[] { "配置レイヤー", "登録テンプレート内の表情アイテムのレイヤーを使います。重なりは配置前に停止します。" },
                    new[] { "テンプレート名", "YMM4の登録名（フォルダを含む）を使います。同名の曖昧な候補は読み込めません。" }
                });
                book.Workbook.Save();
            }
            File.Move(temporary, full, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
