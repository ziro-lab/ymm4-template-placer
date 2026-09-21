using System.IO;

namespace Ymm4TemplatePlacer;

internal enum TachiePresetRouteKind
{
    DirectNamedProperty,
    PropertyEditorLegacy,
    PropertyEditorModern
}

internal enum TachiePresetCapabilityLevel
{
    None,
    Experimental,
    Strong
}

internal sealed record TachiePresetCapabilityFingerprint
{
    private const int HostLimit = 128;
    private const int CharacterLimit = 256;
    private const int ConfigLimit = 1024;
    private const int TypeLimit = 1024;

    public string HostIdentity { get; }
    public string CharacterIdentity { get; }
    public string CharacterConfigIdentity { get; }
    public string PluginRuntimeType { get; }
    public string FaceParameterRuntimeType { get; }
    public Guid PluginModuleMvid { get; }

    public TachiePresetCapabilityFingerprint(
        string hostIdentity,
        string characterIdentity,
        string characterConfigIdentity,
        string pluginRuntimeType,
        string faceParameterRuntimeType,
        Guid pluginModuleMvid)
    {
        HostIdentity = RequireIdentity(hostIdentity, HostLimit, nameof(hostIdentity));
        CharacterIdentity = RequireIdentity(characterIdentity, CharacterLimit, nameof(characterIdentity));
        CharacterConfigIdentity = RequireIdentity(characterConfigIdentity, ConfigLimit, nameof(characterConfigIdentity));
        PluginRuntimeType = RequireIdentity(pluginRuntimeType, TypeLimit, nameof(pluginRuntimeType));
        FaceParameterRuntimeType = RequireIdentity(faceParameterRuntimeType, TypeLimit, nameof(faceParameterRuntimeType));
        if (pluginModuleMvid == Guid.Empty) throw Bad("立ち絵プラグインのModule MVIDがありません。");
        PluginModuleMvid = pluginModuleMvid;
    }

    internal static string RequireIdentity(string value, int limit, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > limit)
            throw Bad($"{field} が空か、上限 {limit} 文字を超えています。");
        return value;
    }

    internal static InvalidDataException Bad(string message) => new(message);
}

internal sealed record TachiePresetRouteDescriptor
{
    private const int IdentityLimit = 1024;

    public TachiePresetRouteKind Kind { get; }
    public string PropertyIdentity { get; }
    public string? EditorIdentity { get; }

    public TachiePresetRouteDescriptor(
        TachiePresetRouteKind kind,
        string propertyIdentity,
        string? editorIdentity = null)
    {
        if (!Enum.IsDefined(kind)) throw TachiePresetCapabilityFingerprint.Bad("未対応の立ち絵プリセット経路です。");
        Kind = kind;
        PropertyIdentity = TachiePresetCapabilityFingerprint.RequireIdentity(
            propertyIdentity, IdentityLimit, nameof(propertyIdentity));

        if (kind == TachiePresetRouteKind.DirectNamedProperty)
        {
            if (editorIdentity != null)
                throw TachiePresetCapabilityFingerprint.Bad("直接指定経路にPropertyEditor識別子は持てません。");
            EditorIdentity = null;
        }
        else
        {
            EditorIdentity = TachiePresetCapabilityFingerprint.RequireIdentity(
                editorIdentity ?? "", IdentityLimit, nameof(editorIdentity));
        }
    }
}

internal sealed record TachiePresetCandidateDescriptor
{
    private const int CandidateLimit = 1024;
    private const int LabelLimit = 512;

    public TachiePresetCapabilityFingerprint Fingerprint { get; }
    public TachiePresetRouteDescriptor Route { get; }
    public string CandidateIdentity { get; }
    public string Label { get; }
    public TachiePresetCapabilityLevel Confidence { get; }

    public TachiePresetCandidateDescriptor(
        TachiePresetCapabilityFingerprint fingerprint,
        TachiePresetRouteDescriptor route,
        string candidateIdentity,
        string label,
        TachiePresetCapabilityLevel confidence)
    {
        Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
        Route = route ?? throw new ArgumentNullException(nameof(route));
        CandidateIdentity = TachiePresetCapabilityFingerprint.RequireIdentity(
            candidateIdentity, CandidateLimit, nameof(candidateIdentity));
        Label = TachiePresetCapabilityFingerprint.RequireIdentity(label, LabelLimit, nameof(label));

        if (confidence is not TachiePresetCapabilityLevel.Strong and not TachiePresetCapabilityLevel.Experimental)
            throw TachiePresetCapabilityFingerprint.Bad("候補のconfidenceはStrongまたはExperimentalである必要があります。");
        Confidence = confidence;
    }
}

internal sealed record TachiePresetCapabilityResult
{
    private const int ReasonLimit = 512;
    private readonly IReadOnlyList<TachiePresetCandidateDescriptor> candidates;

    public TachiePresetCapabilityFingerprint Fingerprint { get; }
    public TachiePresetCapabilityLevel Level { get; }
    public IReadOnlyList<TachiePresetCandidateDescriptor> Candidates => candidates;
    public string? UnavailableReason { get; }
    public bool HasCandidates => candidates.Count > 0;

    private TachiePresetCapabilityResult(
        TachiePresetCapabilityFingerprint fingerprint,
        TachiePresetCapabilityLevel level,
        IReadOnlyList<TachiePresetCandidateDescriptor> candidates,
        string? unavailableReason)
    {
        Fingerprint = fingerprint;
        Level = level;
        this.candidates = candidates;
        UnavailableReason = unavailableReason;
    }

    public static TachiePresetCapabilityResult Supported(
        TachiePresetCapabilityFingerprint fingerprint,
        IEnumerable<TachiePresetCandidateDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(candidates);

        var snapshot = candidates.ToArray();
        if (snapshot.Length == 0) return None(fingerprint, "利用できる立ち絵プリセット候補がありません。");
        if (snapshot.Any(x => x == null))
            throw TachiePresetCapabilityFingerprint.Bad("立ち絵プリセット候補にnullは含められません。");
        if (snapshot.Any(x => x.Fingerprint != fingerprint))
            throw TachiePresetCapabilityFingerprint.Bad("候補のcapability fingerprintが一致していません。");
        if (snapshot.GroupBy(x => x.CandidateIdentity, StringComparer.Ordinal).Any(x => x.Count() != 1))
            return None(fingerprint, "立ち絵プリセット候補の識別子が重複しているため、安全に選択できません。");

        var level = snapshot.All(x => x.Confidence == TachiePresetCapabilityLevel.Strong)
            ? TachiePresetCapabilityLevel.Strong
            : TachiePresetCapabilityLevel.Experimental;

        return new(
            fingerprint,
            level,
            Array.AsReadOnly(snapshot),
            null);
    }

    public static TachiePresetCapabilityResult None(
        TachiePresetCapabilityFingerprint fingerprint,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        var boundedReason = TachiePresetCapabilityFingerprint.RequireIdentity(
            reason, ReasonLimit, nameof(reason));
        return new(
            fingerprint,
            TachiePresetCapabilityLevel.None,
            Array.AsReadOnly(Array.Empty<TachiePresetCandidateDescriptor>()),
            boundedReason);
    }
}
