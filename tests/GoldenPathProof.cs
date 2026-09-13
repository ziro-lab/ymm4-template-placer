using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using S = DocumentFormat.OpenXml.Spreadsheet;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task Run(object root, Timeline timeline, UndoRedoManager undo)
    {
        Assert(!timeline.Items.Any(), "isolated native Timeline is empty");
        var ca = new Character { Name = "TestA" }; var cb = new Character { Name = "TestB" }; var cc = new Character { Name = "TestC" };
        var a = new VoiceItem(ca) { Frame = 10, Length = 30, Layer = 1, Serif = "=This is literal text, not a formula. 日本語A" };
        var b = new VoiceItem(cb) { Frame = 60, Length = 20, Layer = 1, Serif = "Test B voice" };
        var c = new VoiceItem(cc) { Frame = 110, Length = 25, Layer = 1, Serif = string.Concat(Enumerable.Repeat("長いセリフの表示確認。", 20)) };
        var fa = new TachieFaceItem(ca) { Frame = 0, Length = 100, Layer = 3 };
        var fb = new TachieFaceItem(cb) { Frame = 0, Length = 100, Layer = 4 };
        var sa = new TachieFaceItem(ca) { Frame = 0, Length = 100, Layer = 5 };
        var sb = new TachieFaceItem(cb) { Frame = 0, Length = 100, Layer = 6 };
        var manual = new TachieFaceItem(ca) { Frame = 10, Length = 30, Layer = 8, Remark = "manual fixture" };
        var nonFace = new TachieItem(ca) { Frame = 200, Length = 20, Layer = 8, Remark = PlacementEngine.Marker };
        var ta = Template("TestA/Neutral", [fa]); var tb = Template("TestB/Neutral", [fb]);
        var tas = Template("TestA/Smile", [sa]); var tbs = Template("TestB/Smile", [sb]);
        foreach (var t in new[] { ta, tb, tas, tbs, Template("Invalid/multiple", [fa, fb]), Template("Invalid/voice", [a]) }) ItemSettings.Default.Templates.Add(t);
        stage = "P1"; var catalog = TemplateCatalog.Read();
        Assert(catalog.Count(x => x.Name.StartsWith("Test", StringComparison.Ordinal)) == 4 && catalog.All(x => !x.Name.StartsWith("Invalid/", StringComparison.Ordinal)), "P1 live singleton Face catalog");
        Assert(TemplateCatalog.ForVoice(a, catalog).Count == 2 && TemplateCatalog.ForVoice(a, catalog).All(x => x.Character == "TestA"), "P1 Character-specific candidates");
        Assert(TemplateCatalog.ForVoice(c, catalog).Count == 0, "P1 no candidate Character");
        foreach (var item in new IItem[] { a, b, c, manual, nonFace }) Assert(timeline.TryAddItems([item], item.Frame, item.Layer), "fixture native insertion " + item.GetType().Name);
        undo.Record(); stage = "P2"; var voices = VoiceSnapshot.Capture(timeline);
        Assert(voices.Count == 3 && voices[0].Character == "TestA" && voices[0].Frame == 10 && voices[0].Length == 30 && voices[0].Serif == a.Serif, "P2 exact current Scene Voice snapshot");
        stage = "P3"; var rows = voices.Select((v, i) => new AssignmentRow(i + 1, v, catalog)).ToArray();
        rows[0].SelectedChoice = rows[0].Choices.Single(x => x.Template?.Name == "TestA/Neutral");
        Assert(PlacementEngine.Replace(timeline, undo, rows) == 1, "P3 shared Placement Engine insertion");
        var placed = timeline.Items.OfType<TachieFaceItem>().Single(PlacementEngine.IsOwned);
        Assert(placed.Frame == 10 && placed.Length == 30 && placed.Layer == 3 && Equals(placed.Character, ca), "P3 native Timeline coordinates and Character");
        Assert(!ReferenceEquals(placed, fa) && fa.Frame == 0 && fa.Length == 100 && fa.Remark != PlacementEngine.Marker, "P3 independent clone and original Template preserved");
        Log("P1=PASS\nP2=PASS\nP3=PASS");
        stage = "P4"; Assert(OpenTool(root), "P4 invoke actual native Tool menu command");
        for (var i = 0; i < 60 && (ViewModel == null || View == null || !View.IsLoaded); i++) await Task.Delay(100);
        var vm = ViewModel ?? throw new InvalidOperationException("Native Tool ViewModel was not created.");
        var view = View ?? throw new InvalidOperationException("Native Tool View was not created.");
        Assert(view.IsLoaded && ReferenceEquals(view.DataContext, vm), "P4 actual native-hosted View/DataContext");
        vm.Refresh(); await Idle(); Assert(vm.Rows.Count == 3 && vm.Rows[2].State == "候補なし", "P4 host-injected Timeline and missing-candidate UI");
        await SelectInDropdown(view, vm.Rows[0], "TestA/Neutral"); await SelectInDropdown(view, vm.Rows[1], "TestB/Neutral");
        await ClickPlace(view); Assert(!vm.HasError && timeline.Items.Count(PlacementEngine.IsOwned) == 2, "P4 dropdown -> actual WPF button command -> placement");
        SaveView(view); Log("P4=PASS");
        stage = "P5"; var workbook = Path.Combine(output, "GoldenPath.xlsx"); vm.ExportTo(workbook);
        using (var doc = SpreadsheetDocument.Open(workbook, false))
        {
            var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(doc).Take(15).ToArray();
            foreach (var error in errors) Log("OOXML: " + error.Description + " " + error.Path?.XPath);
            Assert(errors.Length == 0, "P5 Open XML schema validation");
            var book = doc.WorkbookPart!; var main = Sheet(book, "Assignments"); var rules = main.Worksheet.GetFirstChild<S.DataValidations>()!;
            Assert(rules.Elements<S.DataValidation>().Count() == 3 && rules.Elements<S.DataValidation>().First().Formula1!.Text.Contains("MATCH($B2,tpl_chars", StringComparison.Ordinal), "P5 Character-aware dropdown definitions");
            Assert(book.Workbook.GetFirstChild<S.Sheets>()!.Elements<S.Sheet>().Single(x => x.Name == "_Catalog").State == S.SheetStateValues.Hidden, "P5 hidden workbook-local Catalog");
            Assert(main.Worksheet.Descendants<S.Cell>().Single(x => x.CellReference == "E2").CellFormula == null, "P5 formula-looking Serif remains literal text");
        }
        Log("P5=PASS"); stage = "P6"; EditCell(workbook, "F2", "TestA/Smile");
        var beforeImport = Signature(timeline); vm.ImportFrom(workbook);
        Assert(Signature(timeline) == beforeImport && vm.Rows[0].SelectedChoice.Template?.Name == "TestA/Smile", "P6 import validates and previews without Timeline mutation");
        await ClickPlace(view);
        Assert(!vm.HasError && timeline.Items.Count(PlacementEngine.IsOwned) == 2 && timeline.Items.OfType<TachieFaceItem>().Single(x => PlacementEngine.IsOwned(x) && x.CharacterName == "TestA").Layer == 5, "P6 edited workbook -> same Placement Engine");
        Log("P6=PASS"); stage = "P7";
        var stable = Signature(timeline); vm.Place(); Assert(Signature(timeline) == stable && timeline.Items.Count(PlacementEngine.IsOwned) == 2, "P7 repeat placement does not duplicate");
        Assert(timeline.Items.Contains(manual) && manual.Remark == "manual fixture" && manual.Frame == 10 && manual.Length == 30 && manual.Layer == 8 && timeline.Items.Contains(nonFace), "P7 manual Face and same-marker non-Face preserved");
        var invalid = Path.Combine(output, "InvalidCharacter.xlsx"); File.Copy(workbook, invalid, true); EditCell(invalid, "F2", "TestB/Neutral");
        RejectWithoutMutation(timeline, () => vm.ImportFrom(invalid), "P7 cross-Character workbook rejected before mutation");
        File.Copy(workbook, invalid, true); EditCell(invalid, "A3", "1"); RejectWithoutMutation(timeline, () => vm.ImportFrom(invalid), "P7 duplicate No rejected");
        File.Copy(workbook, invalid, true); EditCell(invalid, "F2", "TestA/Smile", true); RejectWithoutMutation(timeline, () => vm.ImportFrom(invalid), "P7 Formula cell rejected");
        ItemSettings.Default.Templates.Remove(tas);
        RejectWithoutMutation(timeline, () => vm.ImportFrom(workbook), "P7 missing live Template import rejected");
        RejectWithoutMutation(timeline, () => vm.Place(), "P7 missing live Template placement rejected"); ItemSettings.Default.Templates.Add(tas);
        a.Length++;
        RejectWithoutMutation(timeline, () => vm.ImportFrom(workbook), "P7 stale exported Voice rejected");
        RejectWithoutMutation(timeline, () => vm.Place(), "P7 stale UI Voice rejected"); a.Length--;
        sa.Layer = 8; RejectWithoutMutation(timeline, () => vm.Place(), "P7 collision rejected before deleting prior placements"); sa.Layer = 5;
        File.WriteAllText(invalid, "Not an XLSX"); RejectWithoutMutation(timeline, () => vm.ImportFrom(invalid), "P7 malformed workbook rejected"); File.Delete(invalid);
        Assert(ReferenceEquals(vm.Rows[0].SelectedChoice.Template?.Template, tas), "P7 failed import preserves prior assignment choices");
        Log("P7=PASS"); stage = "P8"; undo.Record(); var before = Signature(timeline);
        await SelectInDropdown(view, vm.Rows[0], "TestA/Neutral"); await SelectInDropdown(view, vm.Rows[1], "TestB/Smile"); await ClickPlace(view);
        var after = Signature(timeline); Assert(!vm.HasError && before != after && undo.IsUndoable, "P8 batch creates native undo history");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "P8 one native Undo restores entire preceding batch");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == after, "P8 one native Redo restores entire new batch");
        foreach (var row in vm.Rows) row.SelectedChoice = row.Choices[0];
        Assert(vm.Place() == 0 && !timeline.Items.Any(PlacementEngine.IsOwned) && timeline.Items.Contains(manual), "P8 all-unselected removes only owned Face items");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == after, "P8 empty-assignment replacement also undoes once");
        Log("P8=PASS"); File.WriteAllText(Path.Combine(output, "final-timeline.txt"), Signature(timeline));
        var dist = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_DIST_DIR");
        if (!string.IsNullOrWhiteSpace(dist))
        {
            using var file = File.OpenRead(Path.Combine(dist, "Ymm4TemplatePlacer.dll")); using var pe = new PEReader(file); var metadata = pe.GetMetadataReader();
            Assert(metadata.TypeDefinitions.All(x => metadata.GetString(metadata.GetTypeDefinition(x).Name) != "NativeProof"), "distribution DLL excludes proof/fixture code");
        }
    }
}
