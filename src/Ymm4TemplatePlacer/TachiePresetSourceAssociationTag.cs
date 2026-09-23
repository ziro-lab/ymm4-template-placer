using System.Globalization;
using System.IO;

namespace Ymm4TemplatePlacer;

/// <summary>
/// Association for Set-owned registered Tachie Preset expressions.
/// Existing TachiePresetAssociationTag remains the v0.5 candidate/capability format.
/// </summary>
internal sealed record TachiePresetSourceAssociationTag
{
    public const string Prefix = "CWT_TPL:TS=";
    private const int HashLength = 64;
    private const string StateHashPrefix = "public-v1:";
    private const int MaxLineLength = 256;

    public Guid Group { get; }
    public Guid Palette { get; }
    public Guid Source { get; }
    public string SourceHash { get; }
    public string StateHash { get; }
    public int Index => 0;
    public int Count => 1;

    public string Line => Prefix + string.Join(";",
        "1",
        Group.ToString("N"),
        Palette.ToString("N"),
        Source.ToString("N"),
        SourceHash,
        StateHash);

    public TachiePresetSourceAssociationTag(
        Guid group,
        Guid palette,
        Guid source,
        string sourceHash,
        string stateHash)
    {
        if (group == Guid.Empty || palette == Guid.Empty || source == Guid.Empty)
            throw new InvalidDataException("登録済み立ち絵プリセット関連付けのIDがありません。");
        RequireHash(sourceHash, nameof(sourceHash));
        RequireStateHash(stateHash);
        Group = group;
        Palette = palette;
        Source = source;
        SourceHash = sourceHash;
        StateHash = stateHash;
        if (Line.Length > MaxLineLength)
            throw new InvalidDataException("登録済み立ち絵プリセット関連付けタグが上限を超えています。");
    }

    public bool SameGroup(TachiePresetSourceAssociationTag other) =>
        Group == other.Group &&
        Palette == other.Palette &&
        Source == other.Source &&
        SourceHash == other.SourceHash &&
        StateHash == other.StateHash;

    public static TachiePresetSourceAssociationTag Create(
        Guid group,
        Guid palette,
        TachiePresetSourceEntry source,
        string stateHash)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new(group, palette, source.Id, source.SemanticHash(), stateHash);
    }

    public static AssociationTagState Read(string? remark, out TachiePresetSourceAssociationTag? tag)
    {
        tag = null;
        var lines = (remark ?? "").Split('\n')
            .Select(x => x.TrimEnd('\r'))
            .Where(x => x.StartsWith(Prefix, StringComparison.Ordinal))
            .ToArray();
        if (lines.Length == 0) return AssociationTagState.None;
        if (lines.Length != 1 || lines[0].Length > MaxLineLength) return AssociationTagState.Invalid;

        var parts = lines[0][Prefix.Length..].Split(';');
        if (parts.Length != 6 || parts[0] != "1" ||
            !Guid.TryParseExact(parts[1], "N", out var group) || group == Guid.Empty ||
            !Guid.TryParseExact(parts[2], "N", out var palette) || palette == Guid.Empty ||
            !Guid.TryParseExact(parts[3], "N", out var source) || source == Guid.Empty ||
            !IsHash(parts[4]) || !IsStateHash(parts[5]))
            return AssociationTagState.Invalid;

        try
        {
            var value = new TachiePresetSourceAssociationTag(
                group, palette, source, parts[4], parts[5]);
            if (value.Line != lines[0]) return AssociationTagState.Invalid;
            tag = value;
            return AssociationTagState.Valid;
        }
        catch (InvalidDataException)
        {
            return AssociationTagState.Invalid;
        }
    }

    private static void RequireHash(string value, string field)
    {
        if (!IsHash(value))
            throw new InvalidDataException(field + " はcanonical SHA-256ではありません。");
    }

    private static void RequireStateHash(string value)
    {
        if (!IsStateHash(value))
            throw new InvalidDataException("stateHash はcanonical public-state fingerprintではありません。");
    }

    private static bool IsHash(string? value) =>
        value?.Length == HashLength && value.All(char.IsAsciiHexDigitLower);

    private static bool IsStateHash(string? value) =>
        value?.Length == StateHashPrefix.Length + HashLength &&
        value.StartsWith(StateHashPrefix, StringComparison.Ordinal) &&
        value[StateHashPrefix.Length..].All(char.IsAsciiHexDigitLower);
}
