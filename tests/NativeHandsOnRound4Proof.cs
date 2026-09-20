using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    internal static void TraceRound4Activation(int row, bool content, bool trial, bool sameVoice,
        int frame, int selectedCount, bool selectedVoice, bool error, string status)
    {
        if (!stage.StartsWith("R4-A", StringComparison.Ordinal)) return;
        Log($"R4-A activate row={row} content={content} trial={trial} sameVoice={sameVoice} frame={frame} selected={selectedCount}/{selectedVoice} error={error} status={status}");
        ViewModel?.InstallRound4TrialDiagnostics(Log);
    }
    private static readonly Dictionary<string, string> round4Checks = new(StringComparer.Ordinal);
    private static void Round4Assert(bool condition, string id, string evidence)
    {
        Assert(condition, $"R4 {id}: {evidence}");
        if (!round4Checks.TryAdd(id, evidence)) throw new InvalidOperationException("Duplicate Round 4 check: " + id);
    }
    private static void Round4Phase(string phase)
    {
        if (phase == "A") ViewModel?.RemoveRound4TrialDiagnostics();
        // A subcheckpoint such as CT must never leak into the later full C manifest.
        var checks = round4Checks.Where(x => x.Key.Length > phase.Length && x.Key.StartsWith(phase, StringComparison.Ordinal) &&
                x.Key[phase.Length..].All(c => c is >= '0' and <= '9'))
            .Select(x => new { id = x.Key, result = "PASS", evidence = x.Value }).ToArray();
        File.WriteAllText(Path.Combine(output, "hands-on-round4-" + phase.ToLowerInvariant() + ".json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Round4-Phase/1", phase, host = "YMM4 4.55.1.1 Lite", result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            runAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"), checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("HANDS_ON_ROUND4_" + phase + "=PASS");
    }
}
