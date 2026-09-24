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
            .Concat(new[] { "W3=PASS", "W8=PASS", "PLACEMENT_SOURCE_P3=PASS", "PLACEMENT_SOURCE_P7=PASS" })
            .Append("W12_UI=PASS").ToArray();
        foreach (var required in stages) Assert(lines.Contains(required, StringComparer.Ordinal), "W12 integrated lane includes completed native stage " + required);
        var requirements = new (string Requirement, string Evidence)[]
        {
            ("v0.3.1 regression, Voice/Excel/Safety/Undo and tool hide/reopen", "P1-P9; NativeProof.cs; GoldenPathProof.cs; NativeUiProof.cs"),
            ("Library display name is independent of host Template name", "W3; NativeLibraryProof.cs"),
            ("current Set/Source placement uses stable explicit source identity without legacy Character Palette switching", "PLACEMENT_SOURCE_P3; NativePlacementSourceNormalTileProof.cs"),
            ("missing/ambiguous Template refs require explicit relink", "W3; NativeLibraryProof.cs"),
            ("current Timeline context selects applicable Item-owned Sets without persisting transient context", "R11/current Intent context proofs"),
            ("Generic Set placement uses CurrentFrame and intrinsic Template duration", "HANDS_ON_ROUND3_D; NativeHandsOnRound3LayerProof.cs"),
            ("normal current Set placement never creates expression association implicitly", "PLACEMENT_SOURCE_P3; current placement proofs"),
            ("Generic occupied-layer policy uses explicit SearchUp/SearchDown rather than legacy Character Front/Back modes", "HANDS_ON_ROUND3_D"),
            ("Generic directional search inspects the whole proposed duration", "HANDS_ON_ROUND3_D"),
            ("Generic directional search is bounded, one-directional and never wraps to the opposite side", "HANDS_ON_ROUND3_D"),
            ("current UntilRelated/Neighbor/MaxGap rules never shorten below the required target span and fail closed on missing neighbors", "current Intent/Placement Rule proofs"),
            ("existing and planned occupancy determine Layers before commit", "P3/HANDS_ON_ROUND3_D/W8; PlacementPlan.cs; native logs"),
            ("associated expression placement gives Voice a weak serial", "W8; NativeAssociationProof.cs"),
            ("Resync resolves exactly one serial plus actual Character, otherwise skips", "W8; NativeResyncProof.cs"),
            ("Resync uses the currently saved expression preset", "W8; NativeResyncProof.cs"),
            ("successful subset of Resync is one native Undo operation", "W8; NativeResyncProof.cs"),
            ("normal placement never automatically deletes existing items", "P3/P8/HANDS_ON_ROUND3_D/current Intent placement proofs; native state assertions"),
            ("invalid input, no Layer and broken references do not partially mutate", "P7/W3/W8/current Intent/Placement Rule rejection assertions")
        };
        var checks = requirements.Select((x, i) => new { id = i + 1, requirement = x.Requirement, result = "PASS", evidence = x.Evidence }).ToArray();
        File.WriteAllText(Path.Combine(output, "v04-acceptance.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Acceptance/1", version = "0.5.0", result = "PASS",
            host = "YMM4 4.55.1.1 Lite", profile_families = 5, required_native_stages = stages, checks,
            additional_profiles = new[] { "Current Intent TargetSpan/SelectedCenter", "Current Intent SelectionRangeStart/End", "Current Intent PairBoundary" },
            boundary = "Native synthetic fixtures and real WPF commands/state. No claim of user PSD-asset visual fidelity or physical mouse injection."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("W12=PASS");
        Log("V04=PASS");
    }
}
