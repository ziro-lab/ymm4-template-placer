using System.IO;
using System.IO.Compression;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Ymm4TemplatePlacer;

public static partial class WorkbookBridge
{
    public static IReadOnlyList<AssignmentRow> Import(string path, string sceneName, IReadOnlyList<VoiceSnapshot> voices, IReadOnlyList<FaceTemplate> catalog)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 16 * 1024 * 1024) throw Bad("Excelファイルが16 MiBを超えています。");
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            if (zip.Entries.Count > 256 || zip.Entries.Sum(x => x.Length) > 128L * 1024 * 1024 || zip.Entries.Any(x => x.Length > 64L * 1024 * 1024) ||
                zip.Entries.Select(x => x.FullName).Distinct(StringComparer.Ordinal).Count() != zip.Entries.Count) throw Bad("Excelファイルが大きすぎるか、ZIP構造が不正です。");
        }
        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, false, new OpenSettings { MaxCharactersInPart = 64L * 1024 * 1024 });
        if (document.DocumentType != SpreadsheetDocumentType.Workbook) throw Bad("通常の.xlsxを読み込んでください。マクロ付きExcelファイルには対応していません。");
        var book = document.WorkbookPart ?? throw Bad("Excelファイルのブック情報がありません。");
        var strings = book.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().Select(RichText).ToArray() ?? [];
        var meta = ReadRows(GetSheet(book, "_Meta"), strings, 2);
        if (meta.Count != 3 || meta[0][0] != "Schema" || meta[0][1] != Schema || meta[1][0] != "Scene" || meta[1][1] != sceneName || meta[2][0] != "Rows" || Integer(meta[2][1]) != voices.Count)
            throw Bad("このシーンのExcel出力時の情報と一致しません。対象シーンを確認し、Excelを再出力してください。");
        var snapshot = ReadRows(GetSheet(book, "_Snapshot"), strings, 6); var main = ReadRows(GetSheet(book, "Assignments"), strings, 6);
        if (voices.Count > MaxRows || snapshot.Count != voices.Count + 1 || main.Count != voices.Count + 1 || !snapshot[0].SequenceEqual(SnapshotHeaders) || !main[0].SequenceEqual(Headers))
            throw Bad("行数・見出しが変更されています。Excelを再出力し、テンプレート列だけを編集してください。");
        var exportedCatalog = ReadRows(GetSheet(book, "_Catalog"), strings, 3);
        if (exportedCatalog.Count == 0 || !exportedCatalog[0].SequenceEqual(new[] { "TemplateId", "Character", "TemplateName" })) throw Bad("テンプレート一覧が不正です。");
        var validNames = new HashSet<(string Character, string Name)>(); var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in exportedCatalog.Skip(1).Where(x => x.Any(y => y.Length > 0)))
            if (entry[0].Length == 0 || !ids.Add(entry[0]) || !validNames.Add((entry[1], entry[2]))) throw Bad("テンプレート一覧に重複または不正な行があります。");
        var byNo = new Dictionary<int, string[]>();
        foreach (var row in main.Skip(1))
        {
            var no = Integer(row[0]); if (no < 1 || no > voices.Count || !byNo.TryAdd(no, row)) throw Bad("Noが重複・変更されています。Excelを再出力してください。");
        }
        var result = new List<AssignmentRow>();
        for (var i = 0; i < voices.Count; i++)
        {
            var voice = voices[i]; var expected = snapshot[i + 1]; var row = byNo[i + 1];
            if (Integer(expected[0]) != i + 1 || expected[1] != voice.Character || Integer(expected[2]) != voice.Frame || Integer(expected[3]) != voice.Length || expected[4] != voice.Serif ||
                Integer(expected[5]) != voice.Layer || !row.Take(5).SequenceEqual(expected.Take(5))) throw Bad($"No {i + 1}: 音声またはExcel出力時の情報が変更されています。Excelを再出力してください。");
            var assignment = new AssignmentRow(i + 1, voice, catalog);
            if (row[5].Length > 0)
            {
                if (!validNames.Contains((voice.Character, row[5]))) throw Bad($"No {i + 1}: このキャラクターの候補ではありません: {row[5]}");
                var matches = assignment.Choices.Where(x => x.Template?.Name == row[5]).ToArray();
                if (matches.Length != 1) throw Bad($"No {i + 1}: 参照テンプレートが削除・変更されているか、同名で曖昧です: {row[5]}。登録を確認して再出力してください。");
                assignment.SelectedChoice = matches[0];
            }
            result.Add(assignment);
        }
        return result;
    }
}
