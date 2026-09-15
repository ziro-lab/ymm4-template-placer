using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void VerifyWorkflowAcceptance()
    {
        stage = "WUX13 workflow acceptance";
        var lines = File.ReadAllLines(Path.Combine(output, "proof-log.txt"));
        var requiredStages = Enumerable.Range(8, 5).Select(x => $"WUX{x}=PASS").ToArray();
        foreach (var required in requiredStages)
            Assert(lines.Contains(required, StringComparer.Ordinal), "WUX13 workflow acceptance requires real native stage " + required);
        Assert(lines.Contains("V04=PASS", StringComparer.Ordinal) && lines.Contains("UX_ACCEPTANCE=PASS", StringComparer.Ordinal),
            "WUX13 workflow acceptance layers on top of the original v0.4 and Task UX acceptance");
        Assert(!nativeFaultOccurred, "WUX13 workflow acceptance rejects any captured unhandled native fault");
        var requirements = new[]
        {
            "Session-only exact resume restores unfinished assignments/drafts across native Tool ViewModel replacement without settings or Timeline writes: WUX8",
            "Changed resume targets are skipped rather than guessed and partial recovery is reported truthfully: WUX8",
            "Single and multi-Template Palette add share one all-or-nothing batch preflight/commit with exact LibraryEntry reuse: WUX9",
            "Bulk selection survives filtering; already-added and incompatible Character sources are removed from unnecessary decisions: WUX9",
            "Expression candidate-zero recovery can collect several exact-Character Face Templates without inventing assignments: WUX9",
            "Palette order is user-defined, persisted per Palette, editable by drag handle or up/down, and leaves other Palettes/Timeline untouched: WUX10",
            "Expression candidate priority follows the user's Character Palette order while row double-click Quick Drop remains separate: WUX10",
            "Rare destructive Palette delete and Library unregister show actual consequences; Cancel is byte-for-byte zero-write: WUX11",
            "Discard/navigation/reload actions use distinct Cancel, Back and Scene Refresh wording: WUX11",
            "Duplicate Palette names and Expression aliases become distinguishable only on collision while strict source identity and 360px usability remain intact: WUX12"
        };
        File.WriteAllText(Path.Combine(output, "ux-workflow-acceptance.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-UX-Workflow/1", result = "PASS", version = "0.4.1",
            base_candidate = "v0.4.0 Task UX", base_task_ux_requirements = 12,
            required_native_stages = requiredStages, checks = requirements.Select((requirement, i) => new { id = i + 1, requirement, result = "PASS" }),
            bulk_expression_assignment = "Deferred; Excel Bridge remains the low-risk bulk row-assignment path.",
            boundary = "Same-session native Tool resume, actual WPF controls/commands and synthetic YMM4 Items. No app-restart crash recovery, arbitrary PSD fidelity, physical pointer/installer or future-YMM4 guarantee."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("UX_WORKFLOW_ACCEPTANCE=PASS");
        Log("WUX13=PASS");
    }
}
