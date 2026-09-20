using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ymm4TemplatePlacer;

/// <summary>A bounded weak association descriptor on each generated member; no second settings/body database.</summary>
public sealed record IntentAssociationTag(Guid Group, Guid Palette, Guid Entry, int Index, int Count, string GeometryHash)
{
    public const string Prefix = "CWT_TPL:B=";
    public string Line => Prefix + string.Join(";", "1", Group.ToString("N"), Palette.ToString("N"), Entry.ToString("N"),
        Index.ToString(CultureInfo.InvariantCulture), Count.ToString(CultureInfo.InvariantCulture), GeometryHash);
    public bool SameGroup(IntentAssociationTag other) => Group == other.Group && Palette == other.Palette && Entry == other.Entry && Count == other.Count && GeometryHash == other.GeometryHash;
    public static AssociationTagState Read(string? remark, out IntentAssociationTag? tag)
    {
        tag = null;
        var lines = (remark ?? "").Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
        if (lines.Length == 0) return AssociationTagState.None;
        if (lines.Length != 1 || lines[0].Length > 256) return AssociationTagState.Invalid;
        var parts = lines[0][Prefix.Length..].Split(';');
        if (parts.Length != 7 || parts[0] != "1" || !Guid.TryParseExact(parts[1], "N", out var group) || group == Guid.Empty ||
            !Guid.TryParseExact(parts[2], "N", out var palette) || palette == Guid.Empty || !Guid.TryParseExact(parts[3], "N", out var entry) || entry == Guid.Empty ||
            !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
            !int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count is < 1 or > 2048 || index < 0 || index >= count ||
            parts[6].Length != 64 || parts[6].Any(x => !char.IsAsciiHexDigitLower(x))) return AssociationTagState.Invalid;
        var value = new IntentAssociationTag(group, palette, entry, index, count, parts[6]);
        if (value.Line != lines[0]) return AssociationTagState.Invalid;
        tag = value; return AssociationTagState.Valid;
    }
    public static string Hash(TemplateBundle bundle)
    {
        var snapshot = JsonSerializer.Serialize(new { bundle.Entry.Source, Items = bundle.Items.Select(x => new {
            Type = IntentSelectionContext.TypeKey(x.GetType()), Frame = x.Frame - bundle.OriginFrame,
            Layer = x.Layer - bundle.MinimumLayer, x.Length, Character = ItemCharacters.Get(x)?.Name }).ToArray() });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))).ToLowerInvariant();
    }
}
