using System.Globalization;

namespace Ymm4TemplatePlacer;

public enum AssociationTagState { None, Valid, Invalid }
public sealed record SourceAssociation(long Serial, string Profile);

/// <summary>Reserved full lines only. Duplicate or malformed tags are never guessed or repaired.</summary>
public static class AssociationTag
{
    private static string[] Lines(string? remark, string prefix) => (remark ?? "").Split('\n')
        .Select(x => x.TrimEnd('\r')).Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
    private static bool Serial(string value, out long serial) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out serial) && serial > 0 &&
        value == serial.ToString(CultureInfo.InvariantCulture);

    public static AssociationTagState Voice(string? remark, out long serial)
    {
        serial = 0;
        var lines = Lines(remark, "CWT_TPL:V=");
        if (lines.Length == 0) return AssociationTagState.None;
        return lines.Length == 1 && Serial(lines[0][10..], out serial)
            ? AssociationTagState.Valid : AssociationTagState.Invalid;
    }
    public static AssociationTagState Source(string? remark, out SourceAssociation? association)
    {
        association = null;
        var lines = Lines(remark, "CWT_TPL:S=");
        if (lines.Length == 0) return AssociationTagState.None;
        if (lines.Length != 1) return AssociationTagState.Invalid;
        var parts = lines[0][10..].Split(';');
        if (parts.Length != 2 || !Serial(parts[0], out var serial) || parts[1] != "P=expression")
            return AssociationTagState.Invalid;
        association = new(serial, "expression");
        return AssociationTagState.Valid;
    }
    public static string TargetLine(long serial)
    {
        if (serial <= 0) throw new InvalidOperationException("関連付けIDは正の整数である必要があります。");
        return "CWT_TPL:V=" + serial.ToString(CultureInfo.InvariantCulture);
    }
    public static string SourceLine(long serial) => TargetLine(serial).Replace("CWT_TPL:V=", "CWT_TPL:S=", StringComparison.Ordinal) + ";P=expression";
}
