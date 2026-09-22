using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyTachiePresetPerformanceCheckpoint(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P10 performance checkpoint";
        var checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P10 " + name); checks.Add(name); }

        var signature = Signature(timeline);
        var config = new P3Config();
        var a = new Character { Name = "P10 A" };
        var b = new Character { Name = "P10 B" };
        var c = new Character { Name = "P10 C" };
        var resolves = 0;
        TachiePresetProbeTarget Resolve(Character character)
        {
            resolves++;
            return new(character, config, typeof(P3Config), () => new P3DirectFace(), () => true);
        }

        using var coordinator = new TachiePresetCapabilityCoordinator(Resolve);
        var voices = Enumerable.Repeat(a, 500)
            .Concat(Enumerable.Repeat(b, 300))
            .Concat(Enumerable.Repeat(c, 200))
            .ToArray();

        var first = await coordinator.ScanAsync(voices, () => true);
        Check(first.Count == 3 &&
            first.All(x => x.Capability is { Level: TachiePresetCapabilityLevel.Strong }) &&
            coordinator.Diagnostics.CharacterScans == 3 && resolves == 3,
            "1000 Voice inputs scan exactly three distinct Characters, not 1000 Voices");

        var beforeCache = coordinator.Diagnostics;
        var second = await coordinator.ScanAsync(voices, () => true);
        var afterCache = coordinator.Diagnostics;
        Check(second.Count == 3 &&
            afterCache.CharacterScans == beforeCache.CharacterScans &&
            afterCache.CacheHits == beforeCache.CacheHits + 3 &&
            resolves == 6,
            "unchanged distinct Characters reuse three immutable capability cache entries");

        var beforeInactive = coordinator.Diagnostics;
        var resolvesBeforeInactive = resolves;
        var inactive = await coordinator.ScanAsync(voices, () => false);
        var afterInactive = coordinator.Diagnostics;
        Check(inactive.Count == 0 &&
            afterInactive.CharacterScans == beforeInactive.CharacterScans &&
            afterInactive.CacheHits == beforeInactive.CacheHits &&
            resolves == resolvesBeforeInactive,
            "inactive scope performs zero preset resolution, discovery or cache work");

        Check(Signature(timeline) == signature &&
            coordinator.Diagnostics.ActiveEditors == 0 &&
            P3EditorFixture.Handlers.Count == 0,
            "P10 distinct-Character checkpoint leaves Timeline untouched and no editor binding alive");

        File.WriteAllText(Path.Combine(output, "tachie-preset-performance-checkpoint.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Performance-Checkpoint/1",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_PERFORMANCE_P10=PASS");
    }
}
