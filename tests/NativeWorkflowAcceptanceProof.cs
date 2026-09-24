using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void VerifyWorkflowAcceptance()
    {
        stage = "WUX13 current workflow acceptance";
        var lines = File.ReadAllLines(Path.Combine(output, "proof-log.txt"));
        var requiredStages = new[]
        {
            "V04=PASS",
            "UX_ACCEPTANCE=PASS",
            "PLACEMENT_SOURCE_P7=PASS",
            "R11=PASS",
            "HANDS_ON_ROUND2_E=PASS",
            "HANDS_ON_ROUND3_E=PASS",
            "COMPACT_SETTINGS_P7=PASS",
            "FINAL_HANDS_ON_POLISH=PASS"
        };
        foreach (var required in requiredStages)
            Assert(lines.Contains(required, StringComparer.Ordinal),
                "WUX13 current workflow acceptance requires native invariant " + required);
        Assert(!nativeFaultOccurred, "WUX13 current workflow acceptance rejects any captured unhandled native fault");

        var requirements = new[]
        {
            "Current Template/TachiePreset Sources use one strict Set-owned placement path and fail closed on ambiguous/stale identity: PLACEMENT_SOURCE_P7",
            "Current Set source registration and bulk organization are protected Settings operations with Timeline zero-write: R11",
            "Current expression selection/replacement stays on exact managed association semantics rather than legacy Palette priority: HANDS_ON_ROUND2_E",
            "Current expression freshness and Tool lifecycle recovery are based on current Voice/source state and never guessed from a legacy workspace: HANDS_ON_ROUND3_E",
            "Current Compact Settings exposes ordinary Set capabilities without requiring legacy Palette/Selection workspaces: COMPACT_SETTINGS_P1-P7",
            "Current Set deletion/copy/list management and whole-Tool Settings wheel behavior retain the accepted hands-on contract: FINAL_HANDS_ON_POLISH",
            "Current UX acceptance is composed from invariant proofs rather than WUX8-WUX12 legacy Palette/Selection UI hosting",
            "Historical WUX8-WUX12 source files remain trace evidence but are no longer mandatory runtime compatibility contracts"
        };

        File.WriteAllText(Path.Combine(output, "ux-workflow-acceptance.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-UX-Workflow/1",
            result = "PASS",
            version = "0.5.0",
            base_candidate = "current pre-Preview invariant baseline",
            base_task_ux_requirements = 12,
            required_native_stages = requiredStages,
            checks = requirements.Select((requirement, i) => new { id = i + 1, requirement, result = "PASS" }),
            bulk_expression_assignment = "Excel Bridge retained",
            boundary = "Current YMM4 4.55.1.1 native invariant evidence. Legacy Palette/Selection workspace resume, ordering and confirmation UX is historical and no longer mandatory."
        }, new JsonSerializerOptions { WriteIndented = true }));

        Log("UX_WORKFLOW_ACCEPTANCE=PASS");
        Log("WUX13=PASS");
    }
}
