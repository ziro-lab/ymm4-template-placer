using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyPlacementSourceModel(Timeline timeline)
    {
        stage = "Placement Source P0 model compatibility";
        var root = Path.Combine(output, "placement-source-p0");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);
        var timelineSignature = Signature(timeline);

        static bool Rejected(Action action)
        {
            try { action(); return false; }
            catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or JsonException)
            { return true; }
        }

        try
        {
            var path = Path.Combine(root, "sources.json");
            var store = new PlacerSettingsStore(path);
            var settings = store.Load();

            var templateId = Guid.Parse("11111111-2222-4333-8444-555555555555");
            var presetId = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
            var template = new LibraryEntry(
                templateId,
                new TemplateLocator("P0/Template", "[]", Guid.Parse("01234567-89ab-4cde-8f01-23456789abcd")),
                "Template source",
                "P0 Character");
            var preset = new TachiePresetSourceEntry(
                presetId,
                "笑顔",
                "P0 Character",
                "Example.Plugin, Example",
                Guid.Parse("12345678-90ab-4cde-8f01-23456789abcd"),
                "Example.CharacterParameter, Example",
                "Example.FaceParameter, Example",
                TachiePresetRouteKind.DirectNamedProperty.ToString(),
                "Example.FaceParameter.Preset",
                null,
                "smile");

            settings.Library.Add(template);
            settings.TachiePresetSources.Add(preset);
            settings.IntentPalettes.Add(new(
                Guid.Parse("fedcba98-7654-4321-8fed-cba987654321"),
                "P0 Set",
                "表情",
                new IntentTargetContext
                {
                    ItemTypeKeys = ["P0.Voice"],
                    CharacterName = "P0 Character"
                },
                new IntentRelation(),
                [new IntentEntry(templateId), new IntentEntry(presetId)]));
            store.Save(settings);

            var raw = File.ReadAllText(path);
            var read = new PlacerSettingsStore(path).Load();
            Assert(read.TachiePresetSources.Count == 1 &&
                read.TachiePresetSources[0] == preset &&
                read.IntentPalettes.Single().Entries.Select(x => x.SourceId).SequenceEqual([templateId, presetId]) &&
                !raw.Contains("\"SourceId\"", StringComparison.Ordinal),
                "PLACEMENT_SOURCE P0 preserves legacy LibraryEntryId JSON while exposing the same GUID as SourceId");

            var templateRegistration = PlacementSourceRegistry.Resolve(read, templateId);
            var presetRegistration = PlacementSourceRegistry.Resolve(read, presetId);
            Assert(templateRegistration.Kind == PlacementSourceKind.Template &&
                templateRegistration.Template == template &&
                templateRegistration.TachiePreset == null &&
                presetRegistration.Kind == PlacementSourceKind.TachiePreset &&
                presetRegistration.Template == null &&
                presetRegistration.TachiePreset == preset,
                "PLACEMENT_SOURCE P0 resolves Template and TachiePreset registrations through one exact SourceId namespace");

            var renamed = preset with { DisplayName = "笑顔（表示名変更）" };
            var changedCandidate = preset with { CandidateIdentity = "different" };
            Assert(preset.SemanticHash() == renamed.SemanticHash() &&
                preset.SemanticHash() != changedCandidate.SemanticHash(),
                "PLACEMENT_SOURCE P0 source semantic hash ignores presentation-only display name but tracks preset identity");

            var presetJson = JsonSerializer.Serialize(preset);
            Assert(!presetJson.Contains("CharacterConfigIdentity", StringComparison.Ordinal) &&
                !presetJson.Contains("HostIdentity", StringComparison.Ordinal) &&
                !presetJson.Contains("StateHash", StringComparison.Ordinal) &&
                !presetJson.Contains("TachieFaceItem", StringComparison.Ordinal),
                "PLACEMENT_SOURCE P0 persisted preset source contains locator metadata, not transient host/config/state/item bodies");

            var collision = PlacerSettingsStore.Copy(read);
            collision.TachiePresetSources[0] = collision.TachiePresetSources[0] with { Id = templateId };
            Assert(Rejected(() => PlacerSettingsStore.Validate(collision)),
                "PLACEMENT_SOURCE P0 rejects SourceId collisions across Template and TachiePreset registries");

            var duplicate = PlacerSettingsStore.Copy(read);
            duplicate.TachiePresetSources.Add(duplicate.TachiePresetSources[0] with { DisplayName = "duplicate" });
            Assert(Rejected(() => PlacerSettingsStore.Validate(duplicate)),
                "PLACEMENT_SOURCE P0 rejects duplicate preset SourceIds");

            Assert(Rejected(() => PlacementSourceRegistry.Resolve(read, Guid.NewGuid())),
                "PLACEMENT_SOURCE P0 missing SourceId fails closed instead of falling back by label");

            var legacyPath = Path.Combine(root, "legacy-v05.json");
            var legacyStore = new PlacerSettingsStore(legacyPath);
            var legacy = legacyStore.Load();
            legacy.Library.Add(template);
            legacy.IntentPalettes.Add(new(
                Guid.Parse("99999999-8888-4777-8666-555555555555"),
                "Legacy Set",
                "表情",
                new IntentTargetContext { ItemTypeKeys = ["P0.Voice"], CharacterName = "P0 Character" },
                new IntentRelation(),
                [new IntentEntry(templateId)]));
            legacyStore.Save(legacy);
            var legacyNode = JsonNode.Parse(File.ReadAllBytes(legacyPath))!.AsObject();
            legacyNode.Remove(nameof(PlacerSettings.TachiePresetSources));
            File.WriteAllText(legacyPath, legacyNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            var oldLoaded = new PlacerSettingsStore(legacyPath).Load();
            Assert(oldLoaded.TachiePresetSources.Count == 0 &&
                oldLoaded.Library.Single().Id == templateId &&
                oldLoaded.IntentPalettes.Single().Entries.Single().LibraryEntryId == templateId,
                "PLACEMENT_SOURCE P0 accepted v0.5 settings without TachiePresetSources load losslessly with an empty additive registry");

            Assert(Signature(timeline) == timelineSignature,
                "PLACEMENT_SOURCE P0 settings/source model proof performs zero Timeline writes");
            Log("PLACEMENT_SOURCE_P0=PASS");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
