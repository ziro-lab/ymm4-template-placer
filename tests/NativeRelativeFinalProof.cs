using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyRelativeFinal(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R12/R14 final normal-work acceptance";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var items = timeline.Items; var selection = timeline.SelectedItems;
        var mode = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
        var character = new Character { Name = "R14 Character" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 60, Layer = 20 };
        var next = new VoiceItem(character) { Frame = 220, Length = 20, Layer = 20 };
        var text = new TextItem { Frame = 150, Length = 40, Layer = 25 };
        var sources = Enumerable.Range(1, 24).Select(i => Template($"R14/Face{i:00}", [new TachieFaceItem(character) { Length = 15, Layer = 4 }])).ToArray();
        foreach (var source in sources) ItemSettings.Default.Templates.Add(source);
        try
        {
            var node = JsonSerializer.SerializeToNode(original)!.AsObject();
            foreach (var name in new[] { "IntentPaletteRevision", "IntentPalettes", "ExpressionBootstrapComplete", "ImportedExpressionSources", "LegacyWorkspace" }) node.Remove(name);
            var migrationPath = Path.Combine(output, "r14-old-settings.json");
            File.WriteAllText(migrationPath, node.ToJsonString()); var oldBytes = File.ReadAllBytes(migrationPath);
            var migrated = new PlacerSettingsStore(migrationPath).Load();
            Assert(File.ReadAllBytes(migrationPath).SequenceEqual(oldBytes) && migrated.IntentPaletteRevision == 1 && migrated.IntentPalettes.Count == 0 &&
                JsonSerializer.Serialize(migrated.Library) == JsonSerializer.Serialize(original.Library) &&
                JsonSerializer.Serialize(migrated.Palettes) == JsonSerializer.Serialize(original.Palettes) &&
                JsonSerializer.Serialize(migrated.ExpressionPresets) == JsonSerializer.Serialize(original.ExpressionPresets) &&
                JsonSerializer.Serialize(migrated.SelectionPresets) == JsonSerializer.Serialize(original.SelectionPresets),
                "R14 migration leaves original bytes and old Library/Palette/Expression/Selection meanings unchanged; ambiguous relative meanings are not invented");

            var references = sources.Select((x, i) => TemplateResolver.Reference(x, $"表情{i + 1:00}", character.Name)).ToList();
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var palette = new IntentPalette(Guid.NewGuid(), "セット1", "表情", target, new(), references.Select(x => new IntentEntry(x.Id)).ToList()) { ExpressionCandidates = true };
            var uniform = palette with { Id = Guid.NewGuid(), Intent = "範囲に配置", Target = target with { MinimumCount = 2, MaximumCount = 2 },
                Relation = new() { Anchor = IntentAnchor.SelectionRangeStart }, Entries = [palette.Entries[0]] };
            var mixed = uniform with { Id = Guid.NewGuid(), Intent = "組み合わせ用", Target = new() { TypeMatch = IntentTypeMatch.ExactMixedTypes,
                ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem)), IntentSelectionContext.TypeKey(typeof(TextItem))], MinimumCount = 2, MaximumCount = 2 } };
            var fixture = PlacerSettingsStore.Copy(original); fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true;
            fixture.Library = references; fixture.IntentPalettes = [palette, uniform, mixed]; fixture.LegacyWorkspace = false;
            field.SetValue(vm, fixture); timeline.Items = [voice, next, text]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.Refresh(); vm.ResetIntentSettings();
            vm.OpenIntentSettingsCommand.Execute(null); await Idle();
            var panel = view.RelativeSettingsSurface;
            Assert(panel.RollbackButton.Command == vm.RollbackIntentSettingsCommand && panel.NewPaletteButton.Command == vm.CreateIntentPaletteCommand,
                "R14 deferred native settings controls hold the real commands, not null no-op bindings");
            var signature = Signature(timeline); var draft = vm.IntentSettings!;
            draft.SelectedPalette!.Name = "保存したセット";
            await Idle(); // Valid edits persist through the real root auto-commit, without a Save click.
            var saved = (PlacerSettings)field.GetValue(vm)!;
            Assert(!vm.HasError && saved.IntentPalettes[0].Name == "保存したセット" && !vm.IntentSettings!.HasChanges &&
                new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().IntentPalettes[0].Name == "保存したセット" && Signature(timeline) == signature,
                "R14 automatic commit persists validated settings atomically, accepts the bound draft, and writes zero Timeline items");
            var savedBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
            var savedJson = JsonSerializer.Serialize(saved);
            vm.IntentSettings!.SelectedPalette!.Name = "外部変更と競合する下書き";
            var external = savedBytes.Concat(Encoding.UTF8.GetBytes("\n")).ToArray(); File.WriteAllBytes(PlacerSettingsStore.DefaultPath, external);
            await Idle(); // Valid edits persist through the real root auto-commit, without a Save click.
            Assert(vm.HasError && vm.IntentSettings!.HasChanges && vm.IntentSettings.SelectedPalette!.Name == "外部変更と競合する下書き" &&
                File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(external) && JsonSerializer.Serialize(field.GetValue(vm)) == savedJson && Signature(timeline) == signature,
                "R14 automatic commit rejects external settings modification without losing the editable draft or overwriting any external byte");
            File.WriteAllBytes(PlacerSettingsStore.DefaultPath, savedBytes); store.Load();
            await InvokeSelectionButton(panel.RollbackButton);
            Assert(!vm.IntentSettings!.HasChanges && vm.IntentSettings.SelectedPalette!.Name == "セット1" && Signature(timeline) == signature,
                "R14/R4 explicit session rollback restores the opening configuration after the external fixture conflict is resolved, without Timeline writes");

            view.PaletteTab.IsSelected = true; view.Width = 360; view.Height = 440; await Idle();
            var surface = view.RelativePaletteSurface; surface.UpdateLayout();
            var buttons = RelativeVisuals(surface.IntentTileItems).OfType<Button>().Where(x => x.CommandParameter is IntentTileChoice).ToArray();
            var points = buttons.Select(x => x.TranslatePoint(new Point(0, 0), surface.IntentTileItems)).ToArray();
            Assert(buttons.Length == 24 && points.Select(x => Math.Round(x.X)).Distinct().Count() >= 2 && points.Select(x => Math.Round(x.Y)).Distinct().Count() >= 3 &&
                buttons.All(x => x.ActualWidth > 0 && x.TranslatePoint(new Point(x.ActualWidth, 0), surface).X <= surface.ActualWidth + 1),
                "R14 dense native tiles wrap into multiple columns and rows at 360px without horizontal clipping");
            SaveNamedView(view, "v042-palette-360.png");
            timeline.SelectedItems = [voice, next]; await Idle();
            Assert(vm.IntentSets.Count == 1 && vm.IntentSets[0].Targeted?.Intent == "範囲に配置", "R12 explicit uniform multi-selection displays only its configured range intent");
            var tile = vm.IntentTiles.Single(); var count = vm.ExecuteIntentTile(tile);
            var added = timeline.Items.Single(x => x != voice && x != next && x != text);
            Assert(count == 1 && added.Frame == 100 && added.Length == 140 && added.Layer == 19,
                "R12 the common action surface executes a saved multi-selection range without a separate Selection Placement mode");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == signature, "R12 one Undo restores a multi-selection range tile action");
            timeline.SelectedItems = [voice, text]; await Idle();
            Assert(vm.IntentSets.Count == 1 && vm.IntentSets[0].Targeted?.Intent == "組み合わせ用", "R12 exact mixed-type context exposes only a deliberately configured mixed intent");
            timeline.SelectedItems = [text]; await Idle();
            Assert(vm.IntentSets.Count == 0 && vm.IntentTiles.Count == 0, "R12 no ordinary selection path falls back to the complete unrelated Library");
            timeline.SelectedItems = [voice]; await Idle();
            view.Width = 640; view.Height = 640; await Idle(); SaveNamedView(view, "v042-palette-640.png");
            view.ExpressionTab.IsSelected = true; await Idle(); SaveNamedView(view, "v042-expression.png");
            view.SelectionTab.IsSelected = true; await Idle(); SaveNamedView(view, "v042-settings.png");
            Log("R12=PASS"); Log("R14_NATIVE=PASS");
        }
        finally
        {
            foreach (var source in sources) ItemSettings.Default.Templates.Remove(source);
            store.Load(); store.Save(original); field.SetValue(vm, original);
            timeline.Items = items; timeline.SelectedItems = selection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            view.Width = width; view.Height = height; vm.SetLegacyWorkspace(mode); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
    private static IEnumerable<DependencyObject> RelativeVisuals(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); yield return child;
            foreach (var descendant in RelativeVisuals(child)) yield return descendant;
        }
    }
    private static void VerifyRelativeAcceptance()
    {
        var lines = File.ReadAllLines(Path.Combine(output, "proof-log.txt"));
        var required = Enumerable.Range(1, 8).Select(x => $"R{x}=PASS").Concat(new[] { "R9_CORE=PASS", "R9_UI=PASS", "R10=PASS", "R11=PASS", "R12=PASS", "R13=PASS", "TEMPLATE_FIDELITY=PASS", "RELATIVE_UIUX=PASS", "R14_NATIVE=PASS", "V04=PASS", "UX_ACCEPTANCE=PASS", "UX_WORKFLOW_ACCEPTANCE=PASS" }).ToArray();
        foreach (var marker in required) Assert(lines.Contains(marker, StringComparer.Ordinal), "R14 final acceptance requires completed native stage " + marker);
        Assert(!nativeFaultOccurred, "R14 no swallowed or unhandled native fault is accepted");
        var checks = new (string Requirement, string Evidence)[] {
            ("Strict 1..N live Template resolution, no source body database, missing/ambiguous/stale rejection", "R1-R3; NativeRelativeFoundationProof"),
            ("Minimum Frame normalization; internal Frame/Layer/Length; independent clones and source preservation", "R2-R3; NativeRelativeFoundationProof"),
            ("Up is smaller; Down is larger; full-span same-direction bundle collision escape; no wrap or partial placement", "R3; NativeRelativeFoundationProof"),
            ("Native Undo/Redo is one whole-bundle action", "R3/R7/R10/R13"),
            ("Versioned settings; deterministic roundtrip; future/corrupt/external-modified settings are not overwritten", "R4/R11/R14; NativeIntentCoreProof/NativeRelativeFinalProof"),
            ("Face-template first bootstrap, explicit new import, deletion ownership and same-character multiple sets", "R5/R11"),
            ("Actual runtime types, logical CharacterName and explicit uniform/mixed cardinalities", "R6/R7/R12"),
            ("Context -> Set -> single native tile click; Set changes are zero-write (Round 2 supersedes the separate Intent navigation layer)", "R7; NativeIntentSurfaceProof"),
            ("Finite anchors/neighbors, previous/next same type/character, MaxGap, ranges/boundaries and explicit fallback", "R8; NativeIntentCoreProof"),
            ("Do Not Place is zero-write; saved relations and stale-plan guards use the shared PlacementPlan gateway", "R8/R9_CORE/R9_UI"),
            ("Expression list and normal Palette share ordered live sources; detached same-name Character is not suppressed", "R10; NativeRelativeExpressionProof"),
            ("Actual host duplicate Character definitions fail closed; no object-identity/fuzzy substitution", "R10; native duplicate registry fixture"),
            ("Palette-backed bundle Excel export/import retains source identity and does not mutate Timeline", "R10; NativeRelativeExpressionProof"),
            ("Settings isolated from execution; real bulk registration/reorder/duplicate/invalid text/automatic commit/session rollback", "R11/R14; NativeIntentSettingsProof/NativeRelativeFinalProof"),
            ("Dense wrapping tiles at 360px; sticky settings rollback; no ordinary unrelated full-Library selection", "R7/R11/R12/R14"),
            ("Mental-model UI: context -> optional Set -> one tile; progressive Settings; natural-language summary; actionable empty states", "RELATIVE_UIUX; NativeRelativeUiUxProof"),
            ("Expression-list Template fidelity rebinds detached same-name Face clones to the target Voice Character while preserving cloned effect identity/value and source Template", "TEMPLATE_FIDELITY; NativeTemplateFidelityProof"),
            ("Weak bundle associations; selected non-Face member/Voice scope; whole-bundle manual Resync and native Undo", "R13; NativeRelativeExpressionProof"),
            ("Missing/copied/source-changed bundle refuses reconstruction; legacy Resync cannot mutate one relative member", "R13; NativeRelativeExpressionProof"),
            ("Old Library/Character-Style palettes/Expression-Selection presets remain readable without load-time writes", "R14 migration proof; old meanings retained in explicit compatibility workspace"),
            ("Original safety, association, Excel, Tool lifecycle and task UX regression ladders retained", "P1-P9/V04/UX_ACCEPTANCE/UX_WORKFLOW_ACCEPTANCE")
        };
        File.WriteAllText(Path.Combine(output, "v042-acceptance.json"), JsonSerializer.Serialize(new {
            schema = "YMM4-Template-Placer-Relative-Acceptance/1", version = "0.5.0", result = "PASS", host = "YMM4 4.55.1.1 Lite",
            required_native_stages = required, checks = checks.Select((x, i) => new { id = i + 1, requirement = x.Requirement, evidence = x.Evidence, result = "PASS" }),
            packaging = "Separate exact distribution DLL smoke and stable-root archive gates must still PASS before distribution.",
            boundary = "Actual WPF commands and synthetic Items; no physical mouse/installer, arbitrary PSD rendering, third-party effect fidelity, crash recovery, or future-host compatibility claim. Character registry uses a fixed read-only host compatibility adapter."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("V042_ACCEPTANCE=PASS");
    }
}
