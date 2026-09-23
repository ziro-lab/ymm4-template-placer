using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record IntentExpressionSource(IntentPalette Palette, IntentEntry Entry, LibraryEntry Library, TemplateBundle Bundle)
{
    public bool Matches(VoiceItem voice) => Palette.ExpressionCandidates && Palette.Target.TypeMatch == IntentTypeMatch.UniformType &&
        Palette.Target.MinimumCount <= 1 && Palette.Target.MaximumCount >= 1 &&
        Palette.Target.ItemTypeKeys.Contains(IntentSelectionContext.TypeKey(voice.GetType()), StringComparer.Ordinal) &&
        (Palette.Target.CharacterName == null || Palette.Target.CharacterName == voice.CharacterName);

    public (IntentPalette Palette, IntentEntry Entry, TemplateBundle Bundle) ResolveCurrent(PlacerSettings settings)
    {
        var palette = settings.IntentPalettes.SingleOrDefault(x => x.Id == Palette.Id);
        var entry = palette?.Entries.SingleOrDefault(x => x.LibraryEntryId == Entry.LibraryEntryId);
        var library = settings.Library.SingleOrDefault(x => x.Id == Library.Id);
        if (palette == null || !palette.ExpressionCandidates || entry == null || library == null || library.Source != Library.Source)
            throw new InvalidOperationException("選択後に表情パレットの所属・元テンプレートが変更されました。候補を更新して選び直してください。");
        Bundle.ValidateCurrent();
        var bundle = TemplateResolver.RequireBundle(library);
        if (!ReferenceEquals(bundle.Template, Bundle.Template))
            throw new InvalidOperationException("表情の元テンプレートが変更されました。推測して配置しません。");
        return (palette, entry, bundle);
    }
}

public static class IntentExpressionCatalog
{
    public static IReadOnlyList<FaceTemplate> Read(PlacerSettings settings)
    {
        var result = new List<FaceTemplate>();
        // Palette order, then entry order, is authoritative. Repeated source membership is a union: the first set wins.
        // Build the Library index once instead of performing a linear lookup for every tile.
        var library = settings.Library.ToDictionary(x => x.Id);
        var seen = new HashSet<YukkuriMovieMaker.Settings.ItemTemplate>(ReferenceEqualityComparer.Instance);
        foreach (var palette in settings.IntentPalettes.Where(x => x.ExpressionCandidates && x.Target.TypeMatch == IntentTypeMatch.UniformType &&
            x.Target.MinimumCount <= 1 && x.Target.MaximumCount >= 1))
        {
            foreach (var tile in palette.Entries)
            {
                library.TryGetValue(tile.LibraryEntryId, out var source);
                if (source == null) continue;
                var resolution = TemplateResolver.ResolveBundle(source);
                var bundle = resolution.Bundle;
                if (bundle == null || !bundle.HasFace || bundle.CharacterName == null ||
                    (palette.Target.CharacterName != null && palette.Target.CharacterName != bundle.CharacterName) || seen.Contains(bundle.Template)) continue;
                var face = bundle.Items.OfType<TachieFaceItem>().FirstOrDefault(x => ItemCharacters.Name(x) == bundle.CharacterName);
                if (face == null) continue;
                seen.Add(bundle.Template);
                result.Add(new(bundle.Template, face, bundle.Template.Name, bundle.CharacterName)
                { IntentSource = new(palette, tile, source, bundle) });
            }
        }
        return result;
    }
    public static IReadOnlyList<TemplateChoice> Choices(VoiceSnapshot target, IReadOnlyList<FaceTemplate> catalog)
    {
        var candidates = TemplateCatalog.ForVoice(target.Voice, catalog);
        var aliases = candidates.Select(x => x.IntentSource?.Library.DisplayName ?? x.Name).GroupBy(x => x, StringComparer.Ordinal)
            .Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        return new[] { new TemplateChoice(null, candidates.Count == 0 ? "— 候補なし —" : "— 配置しない —") }.Concat(candidates.Select(x =>
        {
            var name = x.IntentSource?.Library.DisplayName ?? x.Name;
            var label = aliases.Contains(name) ? $"{name} — {x.IntentSource?.Palette.Name} / {x.Name}" : name;
            return new TemplateChoice(x, label, label);
        })).ToArray();
    }
}


internal sealed record RegisteredPresetExpressionSource(
    Guid PaletteId,
    Guid SourceId,
    string Character,
    string DisplayName,
    string PaletteName,
    string SourceSemanticHash,
    TachiePresetSourceEntry Source)
{
    internal bool MatchesCandidate(TachiePresetCandidateDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return Character == candidate.Fingerprint.CharacterIdentity &&
            Source.PluginRuntimeType == candidate.Fingerprint.PluginRuntimeType &&
            Source.PluginModuleMvid == candidate.Fingerprint.PluginModuleMvid &&
            Source.FaceParameterRuntimeType == candidate.Fingerprint.FaceParameterRuntimeType &&
            Source.Route() == candidate.Route &&
            Source.CandidateIdentity == candidate.CandidateIdentity;
    }

    internal (IntentPalette Palette, IntentEntry Entry, TachiePresetSourceEntry Source) ResolveCurrent(PlacerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var palette = settings.IntentPalettes.SingleOrDefault(x => x.Id == PaletteId);
        var entry = palette?.Entries.SingleOrDefault(x => x.SourceId == SourceId);
        var source = settings.TachiePresetSources.SingleOrDefault(x => x.Id == SourceId);
        if (palette == null || !palette.ExpressionCandidates || entry == null || source == null ||
            source.SemanticHash() != SourceSemanticHash ||
            source.CharacterName != Character)
            throw new InvalidOperationException("選択後に登録済み立ち絵プリセットSourceまたは所属Setが変更されました。候補を更新して選び直してください。");
        return (palette, entry, source);
    }
}

internal static class RegisteredPresetExpressionCatalog
{
    internal static IReadOnlyList<RegisteredPresetExpressionSource> Read(PlacerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var voiceType = IntentSelectionContext.TypeKey(typeof(VoiceItem));
        var result = new List<RegisteredPresetExpressionSource>();
        foreach (var palette in settings.IntentPalettes.Where(x =>
            x.ExpressionCandidates &&
            x.Target.TypeMatch == IntentTypeMatch.UniformType &&
            x.Target.MinimumCount <= 1 &&
            x.Target.MaximumCount >= 1 &&
            x.Target.ItemTypeKeys.Contains(voiceType, StringComparer.Ordinal)))
        {
            foreach (var entry in palette.Entries)
            {
                PlacementSourceRegistration registration;
                try { registration = PlacementSourceRegistry.Resolve(settings, entry.SourceId); }
                catch (InvalidOperationException) { continue; }
                if (registration.Kind != PlacementSourceKind.TachiePreset) continue;
                var source = registration.TachiePreset!;
                if (palette.Target.CharacterName == null ||
                    !string.Equals(palette.Target.CharacterName, source.CharacterName, StringComparison.Ordinal))
                    continue;
                result.Add(new(
                    palette.Id,
                    source.Id,
                    source.CharacterName,
                    IntentTileAppearance.Label(entry, registration),
                    palette.Name,
                    source.SemanticHash(),
                    source));
            }
        }
        return result.AsReadOnly();
    }
}
