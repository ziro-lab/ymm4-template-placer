using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void VerifyHandsOnRound2Final()
    {
        stage = "R2-F consolidated Round 2 evidence";
        var logPath = Path.Combine(output, "proof-log.txt");
        var lines = File.ReadAllLines(logPath);
        foreach (var marker in new[]
        {
            "HANDS_ON_ROUND2_A=PASS", "HANDS_ON_ROUND2_B=PASS", "HANDS_ON_ROUND2_C=PASS",
            "HANDS_ON_ROUND2_D=PASS", "HANDS_ON_ROUND2_E=PASS",
            "HANDS_ON_UX_POLISH=PASS", "TEMPLATE_FIDELITY=PASS", "RELATIVE_UIUX=PASS", "V042_ACCEPTANCE=PASS"
        })
            Assert(lines.Count(x => string.Equals(x, marker, StringComparison.Ordinal)) == 1, "R2-F retained native marker is present exactly once: " + marker);

        RequireRound2Phase("hands-on-round2-input.json", "YMM4-Template-Placer-Round2-Input/1", true);
        RequireRound2Phase("hands-on-round2-sets.json", "YMM4-Template-Placer-Round2-Sets/1", false);
        RequireRound2Phase("hands-on-round2-settings.json", "YMM4-Template-Placer-Round2-Settings/1", true);
        RequireRound2Phase("hands-on-round2-tiles.json", "YMM4-Template-Placer-Round2-Tiles/1", true);
        RequireRound2Phase("hands-on-round2-expression.json", "YMM4-Template-Placer-Round2-Expression/1", true);

        var ids = Enumerable.Range(1, 12).Select(x => $"A{x}")
            .Concat(Enumerable.Range(1, 10).Select(x => $"B{x}"))
            .Concat(Enumerable.Range(1, 10).Select(x => $"C{x}"))
            .Concat(Enumerable.Range(1, 10).Select(x => $"D{x}"))
            .Concat(Enumerable.Range(1, 12).Select(x => $"E{x}"))
            .Concat(Enumerable.Range(1, 13).Select(x => $"F{x}"))
            .Concat(Enumerable.Range(1, 8).Select(x => $"G{x}"))
            .Concat(Enumerable.Range(1, 4).Select(x => $"H{x}")).ToArray();
        Assert(ids.Length == 79 && ids.Distinct(StringComparer.Ordinal).Count() == ids.Length,
            "R2-F consolidated native acceptance has 79 unique A-G/H1-H4 IDs");

        var manifest = new
        {
            schema = "YMM4-Template-Placer-Hands-On-Round2/1",
            version = "0.5.0",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            native_scope = "A1-G8 plus H1-H4",
            release_scope = "H5-H9 are enforced independently by ValidateRelativeEvidence/PackageVerified; H10 is external PR metadata.",
            checks = ids.Select(id => new
            {
                id,
                result = "PASS",
                evidence = id[0] switch
                {
                    'A' => "R2-A exact-host pointer/context/lifecycle proof.",
                    'B' => "R2-B unified Set/tile adapter and backend-delegation proof.",
                    'C' or 'D' => "R2-C Set-first Settings / finite structured relation proof.",
                    'E' => "R2-D real WPF tile gesture/appearance proof and retained staged-draft guard.",
                    'F' or 'G' => "R2-E expression/dirty-scope/session-serial proof plus retained strict association/Undo gates.",
                    _ => "Retained mandatory native markers plus this consolidated current manifest."
                }
            }).ToArray()
        };
        File.WriteAllText(Path.Combine(output, "hands-on-round2.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        Log("HANDS_ON_ROUND2=PASS");
    }

    private static void RequireRound2Phase(string fileName, string schema, bool requireHost)
    {
        var path = Path.Combine(output, fileName);
        Assert(File.Exists(path), "R2-F phase manifest exists: " + fileName);
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        Assert(root.GetProperty("schema").GetString() == schema && root.GetProperty("result").GetString() == "PASS" &&
            (!requireHost || root.GetProperty("host").GetString() == "YMM4 4.55.1.1 Lite"),
            "R2-F phase manifest identity/result is current: " + fileName);
    }
}
