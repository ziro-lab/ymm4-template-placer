using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ymm4TemplatePlacer;

public sealed record TachiePresetSourceEntry(
    Guid Id,
    string DisplayName,
    string CharacterName,
    string PluginRuntimeType,
    Guid PluginModuleMvid,
    string CharacterParameterRuntimeType,
    string FaceParameterRuntimeType,
    string RouteKind,
    string PropertyIdentity,
    string? EditorIdentity,
    string CandidateIdentity)
{
    private const int DisplayNameLimit = 256;
    private const int CharacterLimit = 256;
    private const int IdentityLimit = 1024;

    public void Validate()
    {
        static void Require(string value, int limit, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit)
                throw new InvalidDataException($"{label}が空か、上限{limit}文字を超えています。");
        }

        if (Id == Guid.Empty)
            throw new InvalidDataException("立ち絵プリセットSourceのIDがありません。");
        Require(DisplayName, DisplayNameLimit, "立ち絵プリセットSourceの表示名");
        Require(CharacterName, CharacterLimit, "立ち絵プリセットSourceのキャラクター名");
        Require(PluginRuntimeType, IdentityLimit, "立ち絵プリセットSourceのPlugin型");
        Require(CharacterParameterRuntimeType, IdentityLimit, "立ち絵プリセットSourceのCharacterParameter型");
        Require(FaceParameterRuntimeType, IdentityLimit, "立ち絵プリセットSourceのFaceParameter型");
        Require(PropertyIdentity, IdentityLimit, "立ち絵プリセットSourceのProperty");
        Require(CandidateIdentity, IdentityLimit, "立ち絵プリセットSourceの候補");
        if (PluginModuleMvid == Guid.Empty)
            throw new InvalidDataException("立ち絵プリセットSourceのPlugin MVIDがありません。");
        if (!Enum.TryParse<TachiePresetRouteKind>(RouteKind, false, out var kind) || !Enum.IsDefined(kind))
            throw new InvalidDataException("立ち絵プリセットSourceの経路種類が不正です。");
        if (kind == TachiePresetRouteKind.DirectNamedProperty)
        {
            if (EditorIdentity != null)
                throw new InvalidDataException("直接立ち絵プリセットSourceにEditor識別子は保存できません。");
        }
        else if (string.IsNullOrWhiteSpace(EditorIdentity) || EditorIdentity.Length > IdentityLimit)
            throw new InvalidDataException("立ち絵プリセットSourceのEditor識別子が不正です。");
    }

    internal TachiePresetRouteDescriptor Route()
    {
        Validate();
        var kind = Enum.Parse<TachiePresetRouteKind>(RouteKind, false);
        return new(kind, PropertyIdentity, EditorIdentity);
    }

    internal bool Matches(TachiePresetProbeTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return CharacterName == target.Character.Name &&
            PluginRuntimeType == target.Fingerprint.PluginRuntimeType &&
            PluginModuleMvid == target.Fingerprint.PluginModuleMvid &&
            CharacterParameterRuntimeType == target.CharacterParameterRuntimeType &&
            FaceParameterRuntimeType == target.Fingerprint.FaceParameterRuntimeType;
    }

    internal string SemanticHash()
    {
        Validate();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var length = new byte[4];

        void Add(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        Add("tachie-preset-source-v1");
        Add(CharacterName);
        Add(PluginRuntimeType);
        Add(PluginModuleMvid.ToString("N"));
        Add(CharacterParameterRuntimeType);
        Add(FaceParameterRuntimeType);
        Add(RouteKind);
        Add(PropertyIdentity);
        Add(EditorIdentity ?? "");
        Add(CandidateIdentity);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    internal static TachiePresetSourceEntry From(
        TachiePresetProbeTarget target,
        TachiePresetCandidateDescriptor candidate,
        string? displayName = null,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.Fingerprint != target.Fingerprint)
            throw new InvalidOperationException("保存する立ち絵プリセット候補が現在のキャラクター設定と一致しません。");
        var name = string.IsNullOrWhiteSpace(displayName) ? candidate.Label : displayName.Trim();
        var value = new TachiePresetSourceEntry(
            id ?? Guid.NewGuid(),
            name,
            target.Character.Name,
            target.Fingerprint.PluginRuntimeType,
            target.Fingerprint.PluginModuleMvid,
            target.CharacterParameterRuntimeType,
            target.Fingerprint.FaceParameterRuntimeType,
            candidate.Route.Kind.ToString(),
            candidate.Route.PropertyIdentity,
            candidate.Route.EditorIdentity,
            candidate.CandidateIdentity);
        value.Validate();
        return value;
    }
}

public sealed partial class PlacerSettings
{
    public List<TachiePresetSourceEntry> TachiePresetSources { get; set; } = [];
}

public static class TachiePresetSourceSettings
{
    public const int MaximumSources = 2048;

    public static void Validate(PlacerSettings settings)
    {
        if (settings.TachiePresetSources == null ||
            settings.TachiePresetSources.Count > MaximumSources ||
            settings.TachiePresetSources.Any(x => x == null))
            throw new InvalidDataException("立ち絵プリセットSourceの保存件数が不正です。");
        foreach (var source in settings.TachiePresetSources) source.Validate();
        if (settings.TachiePresetSources.Select(x => x.Id).Distinct().Count() != settings.TachiePresetSources.Count)
            throw new InvalidDataException("立ち絵プリセットSourceのIDが重複しています。");
        var templateIds = settings.Library.Select(x => x.Id).ToHashSet();
        if (settings.TachiePresetSources.Any(x => templateIds.Contains(x.Id)))
            throw new InvalidDataException("Templateと立ち絵プリセットSourceのIDが重複しています。");
    }
}

internal enum PlacementSourceKind
{
    Template,
    TachiePreset
}

internal sealed record PlacementSourceRegistration(
    Guid SourceId,
    PlacementSourceKind Kind,
    LibraryEntry? Template,
    TachiePresetSourceEntry? TachiePreset)
{
    public string DisplayName => Kind == PlacementSourceKind.Template
        ? Template!.DisplayName
        : TachiePreset!.DisplayName;

    public string? CharacterName => Kind == PlacementSourceKind.Template
        ? Template!.CharacterName
        : TachiePreset!.CharacterName;
}

internal static class PlacementSourceRegistry
{
    public static PlacementSourceRegistration Resolve(PlacerSettings settings, Guid sourceId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (sourceId == Guid.Empty)
            throw new InvalidOperationException("配置SourceのIDがありません。");

        var templates = settings.Library.Where(x => x.Id == sourceId).Take(2).ToArray();
        var presets = settings.TachiePresetSources.Where(x => x.Id == sourceId).Take(2).ToArray();
        if (templates.Length + presets.Length != 1)
            throw new InvalidOperationException("配置Sourceを一意に特定できません。設定を確認してください。");
        return templates.Length == 1
            ? new(sourceId, PlacementSourceKind.Template, templates[0], null)
            : new(sourceId, PlacementSourceKind.TachiePreset, null, presets[0]);
    }
}
