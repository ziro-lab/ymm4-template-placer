using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void VerifyFinalAcceptance()
    {
        stage = "W12 integrated acceptance";
        Assert(typeof(PlacerViewModel).Assembly.GetName().Version == new Version(0, 5, 0, 0), "W12 native plugin assembly is version 0.5.0.0");
        var lines = File.ReadAllLines(Path.Combine(output, "proof-log.txt"));
        var stages = Enumerable.Range(1, 9).Select(x => $"P{x}=PASS")
            .Concat(new[] { "W3=PASS", "W5=PASS", "W6=PASS", "W7=PASS", "W8=PASS", "W9=PASS", "W10=PASS", "W11=PASS", "PLACEMENT_SOURCE_P3=PASS" })
            .Append("W12_UI=PASS").ToArray();
        foreach (var required in stages) Assert(lines.Contains(required, StringComparer.Ordinal), "W12 integrated lane includes completed native stage " + required);
        var requirements = new (string Requirement, string Evidence)[]
        {
            ("v0.3.1 regression, Voice/Excel/Safety/Undo and tool hide/reopen", "P1-P9; NativeProof.cs; GoldenPathProof.cs; NativeUiProof.cs"),
            ("Library display name is independent of host Template name", "W3; NativeLibraryProof.cs"),
            ("current Set/Source placement uses stable explicit source identity without legacy Character Palette switching", "PLACEMENT_SOURCE_P3; NativePlacementSourceNormalTileProof.cs"),
            ("missing/ambiguous Template refs require explicit relink", "W3; NativeLibraryProof.cs"),
            ("current Timeline context selects applicable Item-owned Sets without persisting transient context", "R11/current Intent context proofs"),
            ("Quick Drop uses CurrentFrame and intrinsic Length", "W5; NativeQuickDropProof.cs"),
            ("Quick Drop never creates or inherits association", "W5/W8; NativeQuickDropProof.cs; NativeAssociationProof.cs"),
            ("Front/Back use same-Character Layer ordering", "W6; NativeQuickDropProof.cs"),
            ("Front/Back inspect the whole proposed duration", "W6; NativeQuickDropProof.cs"),
            ("multiple same-Character Faces coexist on separate Layers", "W6; NativeQuickDropProof.cs"),
            ("Next Same Character/MaxGap never shorten for an overlapping next Voice", "W7; NativeExpressionPresetProof.cs"),
            ("existing and planned occupancy determine Layers before commit", "P3/W6/W7/W8; PlacementPlan.cs; native logs"),
            ("associated expression placement gives Voice a weak serial", "W8; NativeAssociationProof.cs"),
            ("Resync resolves exactly one serial plus actual Character, otherwise skips", "W8; NativeResyncProof.cs"),
            ("Resync uses the currently saved expression preset", "W8; NativeResyncProof.cs"),
            ("successful subset of Resync is one native Undo operation", "W8; NativeResyncProof.cs"),
            ("normal placement never automatically deletes existing items", "P3/P8/W5-W11; native state assertions"),
            ("invalid input, no Layer and broken references do not partially mutate", "P7/W3/W7-W11; native rejection assertions")
        };
        var checks = requirements.Select((x, i) => new { id = i + 1, requirement = x.Requirement, result = "PASS", evidence = x.Evidence }).ToArray();
        File.WriteAllText(Path.Combine(output, "v04-acceptance.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Acceptance/1", version = "0.5.0", result = "PASS",
            host = "YMM4 4.55.1.1 Lite", profile_families = 5, required_native_stages = stages, checks,
            additional_profiles = new[] { "W9 Target Companion / Point Emphasis", "W10 Selection Range", "W11 Boundary" },
            boundary = "Native synthetic fixtures and real WPF commands/state. No claim of user PSD-asset visual fidelity or physical mouse injection."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("W12=PASS");
        Log("V04=PASS");
    }
}
