using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ymm4TemplatePlacer;

// A compact Timeline descriptor. Long plugin/candidate identities stay in the
// immutable capability model; the Timeline stores only deterministic digests.
internal sealed record TachiePresetAssociationTag(
    Guid Group,
    int Index,
    int Count,
    string CapabilityHash,
    string CandidateHash,
    string StateHash)
{
    public const string Prefix = "CWT_TPL:T=";
    private const int HashLength = 64;
    private const int MaxLineLength = 256;

    public string Line => Prefix + string.Join(";",
        "1",
        Group.ToString("N"),
        Index.ToString(CultureInfo.InvariantCulture),
        Count.ToString(CultureInfo.InvariantCulture),
        CapabilityHash,
        CandidateHash,
        StateHash);

    public TachiePresetAssociationTag
    {
        if (Group == Guid.Empty) throw new InvalidDataException("立ち絵プリセット関連付けのBundle IDがありません。");
        if (Count is < 1 or > 2048 || Index < 0 || Index >= Count)
            throw new InvalidDataException("立ち絵プリセット関連付けのBundle位置が不正です。");
        RequireHash(CapabilityHash, nameof(CapabilityHash));
        RequireHash(CandidateHash, nameof(CandidateHash));
        RequireHash(StateHash, nameof(StateHash));
        if (Line.Length > MaxLineLength)
            throw new InvalidDataException("立ち絵プリセット関連付けタグが上限を超えています。");
    }

    public bool SameGroup(TachiePresetAssociationTag other) =>
        Group == other.Group &&
        Count == other.Count &&
        CapabilityHash == other.CapabilityHash &&
        CandidateHash == other.CandidateHash &&
        StateHash == other.StateHash;

    public static TachiePresetAssociationTag Create(
        Guid group,
        int index,
        int count,
        TachiePresetCandidateDescriptor candidate,
        string stateHash) =>
        new(group, index, count, CapabilityIdentity(candidate.Fingerprint), CandidateIdentity(candidate), stateHash);

    public static AssociationTagState Read(string? remark, out TachiePresetAssociationTag? tag)
    {
        tag = null;
        var lines = (remark ?? "").Split('\n').Select(x => x.TrimEnd('\r'))
            .Where(x => x.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
        if (lines.Length == 0) return AssociationTagState.None;
        if (lines.Length != 1 || lines[0].Length > MaxLineLength) return AssociationTagState.Invalid;
        var parts = lines[0][Prefix.Length..].Split(';');
        if (parts.Length != 7 || parts[0] != "1" ||
            !Guid.TryParseExact(parts[1], "N", out var group) || group == Guid.Empty ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
            count is < 1 or > 2048 || index < 0 || index >= count ||
            !IsHash(parts[4]) || !IsHash(parts[5]) || !IsHash(parts[6]))
            return AssociationTagState.Invalid;
        try
        {
            var value = new TachiePresetAssociationTag(group, index, count, parts[4], parts[5], parts[6]);
            if (value.Line != lines[0]) return AssociationTagState.Invalid;
            tag = value;
            return AssociationTagState.Valid;
        }
        catch (InvalidDataException)
        {
            return AssociationTagState.Invalid;
        }
    }

    internal static string CapabilityIdentity(TachiePresetCapabilityFingerprint fingerprint)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        return HashParts(
            "tachie-capability-v1",
            fingerprint.HostIdentity,
            fingerprint.CharacterIdentity,
            fingerprint.CharacterConfigIdentity,
            fingerprint.PluginRuntimeType,
            fingerprint.FaceParameterRuntimeType,
            fingerprint.PluginModuleMvid.ToString("N"));
    }

    internal static string CandidateIdentity(TachiePresetCandidateDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return HashParts(
            "tachie-candidate-v1",
            CapabilityIdentity(candidate.Fingerprint),
            ((int)candidate.Route.Kind).ToString(CultureInfo.InvariantCulture),
            candidate.Route.PropertyIdentity,
            candidate.Route.EditorIdentity ?? "",
            candidate.CandidateIdentity,
            ((int)candidate.Confidence).ToString(CultureInfo.InvariantCulture));
    }

    private static string HashParts(params string[] parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var part in parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static void RequireHash(string value, string field)
    {
        if (!IsHash(value)) throw new InvalidDataException(field + " はcanonical SHA-256ではありません。");
    }

    private static bool IsHash(string? value) =>
        value?.Length == HashLength && value.All(char.IsAsciiHexDigitLower);
}
