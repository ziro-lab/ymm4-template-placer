using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnRound3Final(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-G retained safety, old-field defaults and consolidated evidence";
        using (var scope = new Round3Fixture(timeline, undo))
        {
            var vm = ViewModel!; var view = View!;
            var fixture = PlacerSettingsStore.Copy(scope.Original);
            var source = scope.AddTemplate("R3G/default", new TextItem { Length = 11, Layer = 9 });
            fixture.Library = [source]; fixture.IntentPalettes = [];
            fixture.Palettes = [new(Guid.NewGuid(), PaletteKind.Style, "旧設定", null, [source.Id])
                { Layer = new() { UseTemplateLayer = false, Minimum = 4, Maximum = 12, Preferred = 8 } }];
            fixture.ManualStylePaletteId = fixture.Palettes[0].Id; fixture.ManualCharacterPaletteId = null;
            fixture.ExpressionBootstrapComplete = true;
            var oldJson = JsonSerializer.SerializeToNode(fixture)!.AsObject(); oldJson.Remove("Presentation");
            oldJson["LegacyWorkspace"] = true;
            foreach (var palette in oldJson["Palettes"]!.AsArray()) palette!["Layer"]!.AsObject().Remove("SearchMode");
            var path = Path.Combine(output, "round3-missing-fields.json"); File.WriteAllText(path, oldJson.ToJsonString());
            var bytes = File.ReadAllBytes(path); var loaded = new PlacerSettingsStore(path).Load();
            Assert(loaded.Presentation.LayoutMode == PaletteLayoutMode.Auto && loaded.Presentation.FixedColumns == 4 &&
                !loaded.Presentation.ShortcutsEnabled && loaded.Presentation.PositionShortcuts.Count == 0 &&
                loaded.Presentation.ViewportFollow == ExpressionViewportFollow.WhenOutside &&
                loaded.Palettes.Single().Layer.SearchMode == LayerSearchMode.Legacy &&
                !JsonSerializer.Serialize(loaded).Contains("LegacyWorkspace", StringComparison.Ordinal) &&
                File.ReadAllBytes(path).SequenceEqual(bytes),
                "R3-G old JSON with a historical LegacyWorkspace field loads current defaults and original layer semantics without reviving that runtime flag or rewriting bytes");
            scope.Apply(loaded, [], []); vm.BeginIntentSettings(); view.SelectionTab.IsSelected = true; await Idle();
            var surface = view.RelativeSettingsSurface.PresentationSettingsSurface;
            var expander = (Expander)surface.Content; expander.IsExpanded = true; await Idle();
            Assert(surface.ViewportFollowPicker.Items.Count == 3 &&
                Equals(surface.ViewportFollowPicker.SelectedValue, ExpressionViewportFollow.WhenOutside),
                "R3-G actual Settings exposes exactly Off/WhenOutside/Always with the old-file default selected");
            var before = Signature(timeline);
            foreach (var mode in new[] { ExpressionViewportFollow.Off, ExpressionViewportFollow.Always, ExpressionViewportFollow.WhenOutside })
            {
                surface.ViewportFollowPicker.SelectedValue = mode; await Idle();
                Assert(vm.IntentSettings!.Presentation.ViewportFollow == mode, "R3-G viewport Settings selection is two-way bound: " + mode);
                vm.SaveIntentSettings(); await Idle();
                Assert(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.ViewportFollow == mode && Signature(timeline) == before,
                    "R3-G explicit viewport preference save is durable and Timeline zero-write: " + mode);
            }
        }
        var lines = File.ReadAllLines(Path.Combine(output, "proof-log.txt"));
        bool Has(string marker) => lines.Count(x => x == marker) == 1;
        var retained = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "W3", "W8", "W12", "W12_UI", "V04",
                "PORTABLE_SETTINGS", "PLACEMENT_SOURCE_P0", "PLACEMENT_SOURCE_P1", "PLACEMENT_SOURCE_P2", "PLACEMENT_SOURCE_P3",
                "PLACEMENT_SOURCE_P4", "PLACEMENT_SOURCE_P5", "PLACEMENT_SOURCE_P6", "PLACEMENT_SOURCE_P7",
                "PLACEMENT_RULE_P1", "PLACEMENT_RULE_P2", "PLACEMENT_RULE_P3",
                "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9_CORE", "R9_UI", "R10", "R11", "R12", "R13", "R14_NATIVE",
                "V042_ACCEPTANCE", "TEMPLATE_FIDELITY", "RELATIVE_UIUX", "HANDS_ON_UX_POLISH",
                "HANDS_ON_ROUND2_A", "HANDS_ON_ROUND2_B", "HANDS_ON_ROUND2_C", "HANDS_ON_ROUND2_D", "HANDS_ON_ROUND2_E", "HANDS_ON_ROUND2",
                "HANDS_ON_ROUND3_A", "HANDS_ON_ROUND3_B", "HANDS_ON_ROUND3_C", "HANDS_ON_ROUND3_D", "HANDS_ON_ROUND3_E", "HANDS_ON_ROUND3_F" }
            .Select(x => x + "=PASS").ToArray();
        Round3Assert(Has("R9_CORE=PASS") && Has("R9_UI=PASS") && round3Checks.ContainsKey("D8"), "G1", "retained targeted add-only and Generic original-item preservation gates passed on this native run");
        Round3Assert(Has("R13=PASS") && Has("HANDS_ON_H3_H4_H5=PASS"), "G2", "exact-association whole-bundle replacement/removal regressions passed");
        Round3Assert(Has("TEMPLATE_FIDELITY=PASS") && Has("R1=PASS") && Has("R2=PASS"), "G3", "strict source identity, canonical Character and clone fidelity gates passed");
        Round3Assert(Has("P8=PASS") && Has("P9=PASS") && round3Checks.ContainsKey("B14"), "G4", "native Undo/Redo and shared mouse/key command routes passed");
        Round3Assert(Has("R14_NATIVE=PASS") && round3Checks.ContainsKey("A10") && round3Checks.ContainsKey("D10"), "G5", "staged settings and external-change guards remain native-proven");
        var host = ExpressionNavigationHost.Resolve();
        Round3Assert(TimelinePointerIntentClassifier.DependencySurfaceAvailableFor(_ => true) && host.CanSeek && host.CanFollow && round3Checks.ContainsKey("F15"),
            "G6", "known public capabilities, not YMM4 version numbers, admit the current host");
        Round3Assert(!TimelinePointerIntentClassifier.DependencySurfaceAvailableFor(_ => false) &&
            ExpressionNavigationHost.FromKnownInstances(host.PreviewOwner, null).CanSeek &&
            ExpressionNavigationHost.FromKnownInstances(null, host.ViewportOwner).CanFollow && round3Checks.ContainsKey("F6") && round3Checks.ContainsKey("F12"),
            "G7", "unavailable pointer dependencies do not govern Preview/viewport adapters and their two degraded native paths passed independently");
        Round3Assert(retained.All(Has) && !lines.Any(x => x.StartsWith("ASSERT FAIL:", StringComparison.Ordinal) || x.StartsWith("FAIL ", StringComparison.Ordinal)),
            "G8", "all current core/source/rule/Round2/Round3/fidelity/UIUX native gates available at this point passed with no failed assertion");
        var phases = new[] { ("A", 10, "appearance"), ("B", 15, "shortcuts"), ("C", 8, "settings"), ("D", 11, "layer"), ("E", 10, "freshness"), ("F", 15, "navigation") };
        var expected = phases.SelectMany(x => Enumerable.Range(1, x.Item2).Select(n => x.Item1 + n)).Concat(Enumerable.Range(1, 9).Select(x => "G" + x)).ToArray();
        var phaseFiles = new List<object>();
        foreach (var (group, count, suffix) in phases)
        {
            var file = $"hands-on-round3-{suffix}.json"; var path = Path.Combine(output, file);
            using var json = JsonDocument.Parse(File.ReadAllText(path)); var root = json.RootElement;
            var checks = root.GetProperty("checks").EnumerateArray().ToArray();
            Assert(Has($"HANDS_ON_ROUND3_{group}=PASS") && root.GetProperty("schema").GetString() == "YMM4-Template-Placer-Round3-Phase/1" &&
                root.GetProperty("phase").GetString() == group && root.GetProperty("host").GetString() == "YMM4 4.55.1.1 Lite" && root.GetProperty("result").GetString() == "PASS" &&
                checks.Length == count && checks.Select(x => x.GetProperty("id").GetString()).ToHashSet().SetEquals(Enumerable.Range(1, count).Select(n => group + n)) &&
                checks.All(x => x.GetProperty("result").GetString() == "PASS" && round3Checks[x.GetProperty("id").GetString()!] == x.GetProperty("evidence").GetString()),
                "R3-G exact current phase identity, check IDs and evidence match the live assertion collection: " + file);
            phaseFiles.Add(new { file, sha256 = Round3Hash(path) });
        }
        Round3Assert(round3Checks.Count == 77 && expected.Where(x => x != "G9").All(round3Checks.ContainsKey), "G9", "all 78 A-F/G1-G9 checks have one authoritative current native manifest; release gates G10-G15 remain separate");
        var manifest = new
        {
            schema = "YMM4-Template-Placer-Hands-On-Round3/1", version = "0.5.0", host = "YMM4 4.55.1.1 Lite", result = "PASS",
            source_head = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkout_tree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            run_id = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), run_attempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"),
            native_scope = "A1-F15 plus G1-G9 (78 checks)",
            release_scope = "G10: independent evidence consumer; G11-G14: package/build/smoke gates; G15: external final Git metadata verification. Native PASS does not claim these future steps.",
            phases = phaseFiles,
            playback = new { file = "hands-on-round3-playback-observation.json", sha256 = Round3Hash(Path.Combine(output, "hands-on-round3-playback-observation.json")) },
            checks = expected.Select(id => new { id, result = "PASS", evidence = round3Checks[id] }).ToArray()
        };
        var manifestPath = Path.Combine(output, "hands-on-round3.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        using var written = JsonDocument.Parse(File.ReadAllText(manifestPath));
        Assert(written.RootElement.GetProperty("checks").GetArrayLength() == 78, "R3-G consolidated manifest is written and readable before its success marker");
        Log("HANDS_ON_ROUND3=PASS");
    }
    private static string Round3Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}
