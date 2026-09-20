using System.IO;
using System.Reflection;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyIntentCore(Timeline timeline, UndoRedoManager undo)
    {
        var before = timeline.Items; var selected = timeline.SelectedItems;
        var character = new Character { Name = "R4 Intent" };
        var target = new VoiceItem(character) { Frame = 100, Length = 80, Layer = 20 };
        var nextVoice = new VoiceItem(character) { Frame = 260, Length = 20, Layer = 20 };
        var previousVoice = new VoiceItem(character) { Frame = 20, Length = 30, Layer = 20 };
        var middleFace = new TachieFaceItem(character) { Frame = 200, Length = 20, Layer = 19 };
        var a = new TachieFaceItem(character) { Frame = 30, Length = 25, Layer = 8 };
        var b = new TachieFaceItem(character) { Frame = 45, Length = 35, Layer = 10 };
        var singleton = Template("R4/Neutral", [a]); var bundle = Template("R4/Bundle", [a, b]);
        ItemSettings.Default.Templates.Add(singleton); ItemSettings.Default.Templates.Add(bundle);
        var storePath = Path.Combine(output, "intent-settings-proof.json");
        try
        {
            timeline.Items = [target, nextVoice, previousVoice, middleFace]; timeline.SelectedItems = [target];
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            var entry = TemplateResolver.Reference(bundle, "Bundle", character.Name);
            var tile = new IntentEntry(entry.Id) { UseTemplateDuration = true };
            var relation = new IntentRelation { Duration = IntentDuration.UntilRelated, Neighbor = IntentNeighbor.NextSameTypeAndCharacter };
            var palette = new IntentPalette(Guid.NewGuid(), "Set 1", "表情",
                new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name }, relation, [tile]) { ExpressionCandidates = true };
            var settings = new PlacerSettings { IntentPaletteRevision = 1, Library = [entry], IntentPalettes = [palette] };
            SelectionPresetSettings.Upgrade(settings);
            var store = new PlacerSettingsStore(storePath); store.Load(); store.Save(settings);
            var bytes = File.ReadAllBytes(storePath); var reloaded = new PlacerSettingsStore(storePath).Load();
            Assert(JsonSerializer.Serialize(settings) == JsonSerializer.Serialize(reloaded) && File.ReadAllBytes(storePath).SequenceEqual(bytes),
                "R4 deterministic intent settings roundtrip; load itself does not rewrite bytes");
            var oldPath = Path.Combine(output, "intent-old-settings.json");
            File.WriteAllText(oldPath, "{\"Schema\":4,\"Library\":[],\"Palettes\":[]}");
            var oldBytes = File.ReadAllBytes(oldPath); var old = new PlacerSettingsStore(oldPath).Load();
            Assert(old.IntentPaletteRevision == 1 && old.IntentPalettes.Count == 0 && old.Library.Count == 0 &&
                File.ReadAllBytes(oldPath).SequenceEqual(oldBytes), "R4 old settings migrate in memory without invented applicability or writes");
            var invalidPath = Path.Combine(output, "intent-future-settings.json");
            File.WriteAllText(invalidPath, JsonSerializer.Serialize(settings).Replace("\"IntentPaletteRevision\":1", "\"IntentPaletteRevision\":2", StringComparison.Ordinal));
            var futureBytes = File.ReadAllBytes(invalidPath);
            RejectWithoutMutation(timeline, () => new PlacerSettingsStore(invalidPath).Load(), "R4 future intent schema is rejected");
            Assert(File.ReadAllBytes(invalidPath).SequenceEqual(futureBytes), "R4 future schema file remains byte-for-byte intact");
            var absent = palette with { Target = palette.Target with { ItemTypeKeys = ["Unavailable.Plugin.Item, Unavailable.Plugin"] } };
            reloaded.IntentPalettes.Add(absent with { Id = Guid.NewGuid() }); PlacerSettingsStore.Validate(reloaded);
            Assert(!absent.Target.Matches(IntentSelectionContext.Capture(timeline)), "R4 absent plugin type is retained but never reinterpreted as a known Item");
            Log("R4=PASS");

            var bootstrap = IntentPaletteBootstrap.Scan(new PlacerSettings());
            var own = bootstrap.Settings.IntentPalettes.Single(x => x.Target.CharacterName == character.Name);
            Assert(own.ExpressionCandidates && own.Entries.Count == 2 && own.Relation.Neighbor == IntentNeighbor.NextSameTypeAndCharacter,
                "R5 first bootstrap reads live Face templates, including multi-item bundles");
            Assert(JsonSerializer.Serialize(bootstrap.Settings) == JsonSerializer.Serialize(IntentPaletteBootstrap.Scan(new PlacerSettings()).Settings),
                "R5 first bootstrap ordering and generated IDs are deterministic");
            var removed = own.Entries[0];
            bootstrap.Settings.IntentPalettes[bootstrap.Settings.IntentPalettes.IndexOf(own)] = own with { Entries = [own.Entries[1]] };
            var unchanged = IntentPaletteBootstrap.Scan(bootstrap.Settings);
            Assert(ReferenceEquals(unchanged.Settings, bootstrap.Settings) && unchanged.AddedEntries == 0,
                "R5 normal refresh does not re-import deleted expression membership");
            var explicitScan = IntentPaletteBootstrap.Scan(bootstrap.Settings, true);
            Assert(!explicitScan.Settings.IntentPalettes.Single(x => x.Id == own.Id).Entries.Contains(removed),
                "R5 explicit new-expression import does not resurrect a previously imported/deleted entry");
            var later = Template("R4/Later", [new TachieFaceItem(character) { Length = 12 }]); ItemSettings.Default.Templates.Add(later);
            try
            {
                Assert(IntentPaletteBootstrap.Scan(bootstrap.Settings).AddedEntries == 0, "R5 later live Templates are not automatically imported");
                var imported = IntentPaletteBootstrap.Scan(bootstrap.Settings, true);
                Assert(imported.AddedEntries == 1 && imported.Settings.IntentPalettes.Single(x => x.Id == own.Id).Entries.Count == 2,
                    "R5 explicit import adds only new live expression sources and preserves user deletion");
                imported.Settings.IntentPalettes.Add(own with { Id = Guid.NewGuid(), Name = "Set 2" });
                PlacerSettingsStore.Validate(imported.Settings);
                Assert(imported.Settings.IntentPalettes.Count(x => x.Target.CharacterName == character.Name) == 2,
                    "R5 multiple independent Palette sets may share one logical CharacterName");
            }
            finally { ItemSettings.Default.Templates.Remove(later); }
            Log("R5=PASS");

            var context = IntentSelectionContext.Capture(timeline);
            Assert(palette.Target.Matches(context), "R6 exact runtime Voice type and logical CharacterName match");
            timeline.SelectedItems = [middleFace];
            Assert(!palette.Target.Matches(IntentSelectionContext.Capture(timeline)), "R6 non-Voice selection hides a Voice-only palette");
            var detachedCharacter = new Character { Name = character.Name };
            var detachedVoice = new VoiceItem(detachedCharacter) { Frame = 400, Length = 10, Layer = 22 };
            var scenario = timeline.Items;
            timeline.Items = scenario.Add(detachedVoice); timeline.SelectedItems = [detachedVoice];
            Assert(palette.Target.Matches(IntentSelectionContext.Capture(timeline)), "R6 logical CharacterName filtering does not require object identity");
            timeline.Items = scenario; timeline.SelectedItems = [target, nextVoice];
            var multiple = palette.Target with { MinimumCount = 2, MaximumCount = 8 };
            Assert(multiple.Matches(IntentSelectionContext.Capture(timeline)) && !palette.Target.Matches(IntentSelectionContext.Capture(timeline)),
                "R6 same-type multi-selection requires an explicit multi-item applicability contract");
            timeline.SelectedItems = [target, middleFace];
            var mixed = multiple with { TypeMatch = IntentTypeMatch.ExactMixedTypes,
                ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem)), IntentSelectionContext.TypeKey(typeof(TachieFaceItem))] };
            Assert(mixed.Matches(IntentSelectionContext.Capture(timeline)) && !multiple.Matches(IntentSelectionContext.Capture(timeline)),
                "R6 mixed selection only matches its explicitly configured runtime type set");
            timeline.SelectedItems = [target];
            Assert(!mixed.Matches(IntentSelectionContext.Capture(timeline)), "R6 a mixed-type contract does not degrade to single-item applicability");
            Log("R6=PASS");

            var standard = new IntentEntry(entry.Id);
            var resolved = IntentRelationResolver.Resolve(context, relation, standard, 50);
            Assert(resolved.Frame == 100 && resolved.Length == 160, "R8 next same type + Character resolves Voice start, not a closer Face item");
            var sameName = IntentRelationResolver.Resolve(context, relation with { Neighbor = IntentNeighbor.NextSameCharacter }, standard, 50);
            Assert(sameName.Length == 100, "R8 next same Character can intentionally refer across runtime types");
            var previous = IntentRelationResolver.Resolve(context, relation with { Neighbor = IntentNeighbor.PreviousSameTypeAndCharacter,
                Anchor = IntentAnchor.RelatedEnd, Duration = IntentDuration.Fixed }, standard, 50);
            Assert(previous.Frame == 50 && previous.Length == 30, "R8 previous same type/Character and neighbor-end anchor are finite reusable primitives");
            var maxGap = IntentRelationResolver.Resolve(context, relation with { MaximumNeighborGap = 50 }, standard, 50);
            Assert(maxGap.Frame == 100 && maxGap.Length == 80, "R8 MaxGap switches to the explicit current-target-end fallback");
            var overridden = IntentRelationResolver.Resolve(context, relation, standard with { FixedDurationOverride = 15 }, 50);
            Assert(overridden.Frame == 100 && overridden.Length == 15, "R8 per-entry override changes a small duration parameter, not the whole relation");
            timeline.Items = [target]; timeline.SelectedItems = [target]; var alone = IntentSelectionContext.Capture(timeline);
            Assert(IntentRelationResolver.Resolve(alone, relation, standard, 50).Length == 80, "R8 no neighbor falls back to current target end");
            Assert(IntentRelationResolver.Resolve(alone, relation with { Fallback = IntentFallback.FixedDuration }, standard, 50).Length == 30,
                "R8 fixed-duration fallback uses the saved duration");
            Assert(IntentRelationResolver.Resolve(alone, relation with { Fallback = IntentFallback.DoNotPlace }, standard, 50).Skip,
                "R8 Do Not Place fallback is an explicit zero-write result");
            var tied = new VoiceItem(character) { Frame = 260, Length = 10, Layer = 30 };
            timeline.Items = scenario.Add(tied); timeline.SelectedItems = [target];
            RejectWithoutMutation(timeline, () => IntentRelationResolver.Resolve(IntentSelectionContext.Capture(timeline), relation, standard, 50),
                "R8 equal-position neighbor ambiguity is rejected instead of first-match guessing");
            timeline.Items = scenario; timeline.SelectedItems = [target];
            var stale = IntentSelectionContext.Capture(timeline); nextVoice.Frame++;
            RejectWithoutMutation(timeline, () => stale.ValidateCurrent(timeline), "R6/R8 changed neighborhood invalidates the captured relation context"); nextVoice.Frame--;
            timeline.SelectedItems = [previousVoice, target];
            var range = IntentRelationResolver.Resolve(IntentSelectionContext.Capture(timeline), new() { Anchor = IntentAnchor.SelectionRangeStart }, standard, 50);
            Assert(range.Frame == 20 && range.Length == 160, "R8 selection-range relation uses the entire selected span");
            RejectWithoutMutation(timeline, () => IntentRelationResolver.Resolve(IntentSelectionContext.Capture(timeline), new()
                { Anchor = IntentAnchor.PairBoundary, Duration = IntentDuration.Fixed }, standard, 50), "R8 pair boundary does not guess across an unapproved gap");
            var boundary = IntentRelationResolver.Resolve(IntentSelectionContext.Capture(timeline), new()
                { Anchor = IntentAnchor.PairBoundary, Duration = IntentDuration.Fixed, BoundaryTolerance = 50 }, standard, 50);
            Assert(boundary.Frame == 50 && boundary.Length == 30, "R8 pair boundary retains the earlier exclusive end within explicit tolerance");
            Log("R8=PASS");

            timeline.SelectedItems = [target];
            var signature = Signature(timeline); var execution = IntentExecutionPlan.Create(timeline, palette, tile, [entry]);
            Assert(execution.Count == 2 && Signature(timeline) == signature, "R9 saved Palette relation plans the complete bundle without mutation");
            Assert(execution.Commit(timeline, undo) == 2, "R9 bundle execution uses the existing PlacementPlan/native Undo commit gateway");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == signature, "R9 one native Undo restores a complete Palette tile action");
            timeline.SelectedItems = [target];
            var guarded = IntentExecutionPlan.Create(timeline, palette, tile, [entry]); timeline.SelectedItems = [nextVoice];
            RejectWithoutMutation(timeline, () => guarded.Commit(timeline, undo), "R9 changed selection between plan/click and commit is zero-write");
            timeline.SelectedItems = [target];
            var singletonEntry = TemplateResolver.Reference(singleton, "single", character.Name); var singletonTile = new IntentEntry(singletonEntry.Id);
            var skipPalette = palette with { Relation = relation with { Fallback = IntentFallback.DoNotPlace }, Entries = [singletonTile] };
            timeline.Items = [target];
            var skip = IntentExecutionPlan.Create(timeline, skipPalette, singletonTile, [singletonEntry]);
            var skipSignature = Signature(timeline);
            Assert(skip.Skipped && skip.Commit(timeline, undo) == 0 && Signature(timeline) == skipSignature,
                "R9 Do Not Place fallback commits zero items and leaves the Timeline unchanged");
            Log("R9_CORE=PASS");

            var api = new List<string>();
            foreach (var type in new[] { typeof(Character), typeof(VoiceItem), typeof(TachieFaceItem), typeof(TachieItem) })
            {
                api.Add("TYPE " + type.FullName);
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(x => x.Name.Contains("Character", StringComparison.Ordinal) || x.Name == "Name"))
                    api.Add($"PROPERTY {property.Name} {property.PropertyType.FullName} public-set={property.SetMethod?.IsPublic == true}");
            }
            var registry = typeof(Character).Assembly.GetType("YukkuriMovieMaker.Settings.CharacterSettings");
            api.Add("REGISTRY " + (registry?.FullName ?? "not-found"));
            if (registry != null)
                foreach (var property in registry.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    api.Add($"REGISTRY-PROPERTY {property.Name} {property.PropertyType.FullName} public-set={property.SetMethod?.IsPublic == true}");
            File.WriteAllLines(Path.Combine(output, "relative-public-api.txt"), api);
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(singleton); ItemSettings.Default.Templates.Remove(bundle);
            timeline.Items = before; timeline.SelectedItems = selected; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        }
    }
}
