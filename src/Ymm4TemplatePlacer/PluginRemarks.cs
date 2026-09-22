using System.Text.RegularExpressions;
namespace Ymm4TemplatePlacer;

public static class PluginRemarks
{
    // Only complete reserved plugin lines are stripped from a detached clone, including bundle membership.
    // User prose, including inline mentions of the prefix, is retained.
    private static readonly Regex ReservedLine = new(@"(?m)^CWT_TPL:(?:face|CAL=[^\r\n]*|[VSBT]=[^\r\n]*)(?:\r?\n|$)", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    public static string WithoutAssociation(string? remark) => ReservedLine.Replace(remark ?? "", "");
    public static string Append(string? remark, string tag)
    {
        var text = remark ?? "";
        return text.Length == 0 || text.EndsWith('\n') ? text + tag : text + "\n" + tag;
    }
}
