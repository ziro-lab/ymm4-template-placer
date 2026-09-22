using System.IO;

namespace Ymm4TemplatePlacer;

public sealed record TachiePresetLearnedAdapter(
    string PluginRuntimeType,
    Guid PluginModuleMvid,
    string CharacterParameterRuntimeType,
    string FaceParameterRuntimeType,
    string RouteKind,
    string PropertyIdentity,
    string? EditorIdentity)
{
    private const int IdentityLimit = 1024;

    internal void Validate()
    {
        static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > IdentityLimit)
                throw new InvalidDataException($"表情プリセット認識の{name}が空か、上限を超えています。");
        }
        Require(PluginRuntimeType, "Plugin型");
        Require(CharacterParameterRuntimeType, "CharacterParameter型");
        Require(FaceParameterRuntimeType, "FaceParameter型");
        Require(PropertyIdentity, "Property");
        if (PluginModuleMvid == Guid.Empty)
            throw new InvalidDataException("表情プリセット認識のPlugin MVIDがありません。");
        if (!Enum.TryParse<TachiePresetRouteKind>(RouteKind, false, out var kind) || !Enum.IsDefined(kind))
            throw new InvalidDataException("表情プリセット認識の経路種類が不正です。");
        if (kind == TachiePresetRouteKind.DirectNamedProperty)
        {
            if (EditorIdentity != null)
                throw new InvalidDataException("直接表情プリセット経路にEditor識別子は保存できません。");
        }
        else if (string.IsNullOrWhiteSpace(EditorIdentity) || EditorIdentity.Length > IdentityLimit)
            throw new InvalidDataException("表情プリセット認識のEditor識別子が不正です。");
    }

    internal TachiePresetRouteDescriptor Descriptor()
    {
        Validate();
        var kind = Enum.Parse<TachiePresetRouteKind>(RouteKind, false);
        return new(kind, PropertyIdentity, EditorIdentity);
    }

    internal bool Matches(TachiePresetProbeTarget target) =>
        PluginRuntimeType == target.Fingerprint.PluginRuntimeType &&
        PluginModuleMvid == target.Fingerprint.PluginModuleMvid &&
        CharacterParameterRuntimeType == target.CharacterParameterRuntimeType &&
        FaceParameterRuntimeType == target.Fingerprint.FaceParameterRuntimeType;

    internal static TachiePresetLearnedAdapter From(TachiePresetProbeTarget target, TachiePresetRouteDescriptor route) =>
        new(
            target.Fingerprint.PluginRuntimeType,
            target.Fingerprint.PluginModuleMvid,
            target.CharacterParameterRuntimeType,
            target.Fingerprint.FaceParameterRuntimeType,
            route.Kind.ToString(),
            route.PropertyIdentity,
            route.EditorIdentity);
}

public sealed partial class PlacerSettings
{
    public List<TachiePresetLearnedAdapter> TachiePresetAdapters { get; set; } = [];
}

public static class TachiePresetLearnedAdapterSettings
{
    private const int MaxAdapters = 64;

    public static void Validate(PlacerSettings settings)
    {
        if (settings.TachiePresetAdapters == null || settings.TachiePresetAdapters.Count > MaxAdapters ||
            settings.TachiePresetAdapters.Any(x => x == null))
            throw new InvalidDataException("表情プリセット認識の保存件数が不正です。");
        foreach (var adapter in settings.TachiePresetAdapters) adapter.Validate();
        var keys = settings.TachiePresetAdapters.Select(x =>
            (x.PluginRuntimeType, x.PluginModuleMvid, x.CharacterParameterRuntimeType, x.FaceParameterRuntimeType));
        if (keys.Distinct().Count() != settings.TachiePresetAdapters.Count)
            throw new InvalidDataException("同じ立ち絵Plugin面の表情プリセット認識が重複しています。");
    }

    internal static TachiePresetRouteDescriptor? Resolve(PlacerSettings settings, TachiePresetProbeTarget target)
    {
        var matches = settings.TachiePresetAdapters.Where(x => x.Matches(target)).Take(2).ToArray();
        return matches.Length == 1 ? matches[0].Descriptor() : null;
    }

    internal static void Upsert(PlacerSettings settings, TachiePresetProbeTarget target, TachiePresetRouteDescriptor route)
    {
        var next = TachiePresetLearnedAdapter.From(target, route);
        settings.TachiePresetAdapters.RemoveAll(x => x.Matches(target));
        settings.TachiePresetAdapters.Add(next);
        Validate(settings);
    }
}
