using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed record IntentBootstrapResult(PlacerSettings Settings, int AddedEntries, IReadOnlyList<string> Diagnostics);

public static class IntentPaletteBootstrap
{
    public static IntentBootstrapResult Scan(PlacerSettings original, bool explicitImport = false)
    {
        if (original.ExpressionBootstrapComplete && !explicitImport) return new(original, 0, []);
        var next = PlacerSettingsStore.Copy(original); IntentPaletteSettings.Upgrade(next);
        var diagnostics = new List<string>(); var added = 0;
        foreach (var template in ItemSettings.Default.Templates.OrderBy(x => x.Name, StringComparer.Ordinal)
            .ThenBy(x => TemplateLocator.Capture(x).PathJson, StringComparer.Ordinal).ThenBy(x => x.SceneId))
        {
            if (!template.Items.Any(x => x is TachieFaceItem)) continue;
            var locator = TemplateLocator.Capture(template);
            if (next.ImportedExpressionSources.Contains(locator)) continue;
            var names = template.Items.Select(x => ItemCharacters.Get(x)?.Name).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
            if (names.Length != 1 || string.IsNullOrWhiteSpace(names[0]))
            { diagnostics.Add($"{template.Name}: キャラクターが一意でないため、自動取り込みしませんでした。"); continue; }
            var character = names[0];
            var refs = next.Library.Where(x => x.Source == locator && (x.CharacterName == null || x.CharacterName == character)).Take(2).ToArray();
            if (refs.Length > 1) { diagnostics.Add($"{template.Name}: 同じ元への登録が複数あるため、使用する登録を設定で選んでください。"); continue; }
            var entry = refs.FirstOrDefault() ?? new LibraryEntry(StableId("source", JsonSerializer.Serialize(locator)), locator, template.Name, character);
            var resolution = TemplateResolver.ResolveBundle(entry);
            if (resolution.Bundle == null) { diagnostics.Add($"{template.Name}: {resolution.Message}"); continue; }
            if (refs.Length == 0) next.Library.Add(entry);
            var index = next.IntentPalettes.FindIndex(x => x.ExpressionCandidates && x.Target.CharacterName == character &&
                x.Target.TypeMatch == IntentTypeMatch.UniformType && x.Target.MinimumCount == 1 &&
                x.Target.ItemTypeKeys.Contains(IntentSelectionContext.TypeKey(typeof(VoiceItem)), StringComparer.Ordinal));
            if (index < 0)
            {
                next.IntentPalettes.Add(new(StableId("character", character), character, "表情",
                    new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character },
                    new IntentRelation { Duration = IntentDuration.UntilRelated, Neighbor = IntentNeighbor.NextSameTypeAndCharacter,
                        Fallback = IntentFallback.CurrentTargetEnd }, []) { ExpressionCandidates = true });
                index = next.IntentPalettes.Count - 1;
            }
            var palette = next.IntentPalettes[index];
            if (!palette.Entries.Any(x => x.LibraryEntryId == entry.Id))
            {
                next.IntentPalettes[index] = palette with { Entries = [.. palette.Entries, new IntentEntry(entry.Id) { UseTemplateDuration = resolution.Bundle.Items.Count > 1 }] };
                added++;
            }
            next.ImportedExpressionSources.Add(locator);
        }
        next.ExpressionBootstrapComplete = true;
        PlacerSettingsStore.Validate(next);
        return new(next, added, diagnostics.AsReadOnly());
    }
    private static Guid StableId(string kind, string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"Ymm4TemplatePlacer/v042/{kind}/{value}")).AsSpan(0, 16));
}
