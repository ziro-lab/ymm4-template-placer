using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifySettingsCharacterFilter(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Settings character-aware Set visibility";
        using var scope = new Round3Fixture(timeline, undo);
        var voiceKey = IntentSelectionContext.TypeKey(typeof(VoiceItem));
        var textKey = IntentSelectionContext.TypeKey(typeof(TextItem));
        var a = new VoiceItem(new Character { Name = "Filter A" }) { Frame = 50, Length = 20, Layer = 20 };
        var b = new VoiceItem(new Character { Name = "Filter B" }) { Frame = 100, Length = 20, Layer = 21 };
        IntentPalette Set(string name, string? character, string? key = null, int minimum = 1, int maximum = 1) =>
            new(Guid.NewGuid(), name, "legacy/intent", new() { ItemTypeKeys = [key ?? voiceKey],
                CharacterName = character, MinimumCount = minimum, MaximumCount = maximum }, new(), []);
        var other = Set("B first", "Filter B");
        var common = Set("Common", null, maximum: 2);
        var pair = Set("A pair", "Filter A", minimum: 2, maximum: 2);
        var text = Set("Text", null, textKey);
        var own = Set("A single", "Filter A");
        var third = Set("C last", "Filter C");
        var legacy = Set("Legacy", "Filter B") with { Target = new() { ItemTypeKeys = [voiceKey, textKey], CharacterName = "Filter B" } };
        var generic = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "Generic", null, []);
        var fixture = new PlacerSettings { ExpressionBootstrapComplete = true, IntentPaletteRevision = 1,
            IntentPalettes = [other, common, pair, text, own, third, legacy], Palettes = [generic], ManualStylePaletteId = generic.Id };
        var sourceJson = JsonSerializer.Serialize(fixture);
        var order = fixture.IntentPalettes.Select(x => x.Id).ToArray();
        IntentSettingsSession Session(params IItem[] selection) => new(fixture, new[] { typeof(VoiceItem), typeof(TextItem) }, selection);
        static Guid[] Visible(IntentSettingsSession session) => session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).ToArray();
        void VoiceParent(IntentSettingsSession session) => session.SelectedItemContext = session.ItemContexts.Single(x => x.Key == voiceKey);
        var session = Session(a);
        Assert(!session.ShowOtherCharacterSets && Visible(session).SequenceEqual(new[] { common.Id, own.Id }),
            "SET_CHARACTER_FILTER old/default presentation uses OFF and retains strict current-selection count/character applicability");
        VoiceParent(session);
        Assert(session.HasSettingsReferenceCharacter && Visible(session).SequenceEqual(new[] { common.Id, pair.Id, own.Id }),
            "SET_CHARACTER_FILTER definite live character shows its Sets plus unrestricted Sets in saved interleaved order, independent of placement count");
        session.ShowOtherCharacterSets = true;
        Assert(Visible(session).SequenceEqual(new[] { other.Id, common.Id, pair.Id, own.Id, third.Id }) &&
            session.Palettes.Select(x => x.Id).SequenceEqual(order),
            "SET_CHARACTER_FILTER ON reveals other characters only within the Item parent without sorting, rewriting order or revealing other Item types");
        session.ShowOtherCharacterSets = false;
        session.OpenPaletteForEditing(session.Palettes.Single(x => x.Id == other.Id));
        Assert(session.SettingsReferenceCharacterLabel.Contains("Filter B", StringComparison.Ordinal) &&
            Visible(session).SequenceEqual(new[] { other.Id, common.Id }),
            "SET_CHARACTER_FILTER explicitly opened restricted Set takes reference precedence over selected-item character");
        session.UpdateSelectionContext([a, b]);
        session.SelectedPalette = session.Palettes.Single(x => x.Id == common.Id);
        Assert(session.SettingsReferenceCharacterLabel.Contains("Filter B", StringComparison.Ordinal) &&
            Visible(session).SequenceEqual(new[] { other.Id, common.Id }),
            "SET_CHARACTER_FILTER dropdown switches and later mixed Timeline selection retain the editing-session reference");

        var liveFallback = Session(a); liveFallback.UpdateSelectionContext([b]);
        liveFallback.OpenPaletteForEditing(liveFallback.Palettes.Single(x => x.Id == common.Id));
        Assert(liveFallback.SettingsReferenceCharacterLabel.Contains("Filter B", StringComparison.Ordinal) &&
            Visible(liveFallback).SequenceEqual(new[] { other.Id, common.Id }),
            "SET_CHARACTER_FILTER explicitly opened unrestricted Set captures the then-current definite live selection");
        var mixedFallback = Session(a, b); mixedFallback.OpenPaletteForEditing(mixedFallback.Palettes.Single(x => x.Id == common.Id));
        Assert(!mixedFallback.HasSettingsReferenceCharacter && Visible(mixedFallback).Length == 5,
            "SET_CHARACTER_FILTER unrestricted Set opened from mixed characters keeps all same-type Sets");
        var noCharacter = Session(new TextItem { Length = 10 }); VoiceParent(noCharacter);
        var mixed = Session(a, b); VoiceParent(mixed);
        var empty = Session(); VoiceParent(empty);
        var allVoice = new[] { other.Id, common.Id, pair.Id, own.Id, third.Id };
        Assert(new[] { noCharacter, mixed, empty }.All(x => !x.HasSettingsReferenceCharacter && Visible(x).SequenceEqual(allVoice)),
            "SET_CHARACTER_FILTER no-character, mixed-character and empty contexts invent no character restriction");
        var sameName = Session(a, new VoiceItem(new Character { Name = "Filter A" }) { Length = 10 }); VoiceParent(sameName);
        Assert(Visible(sameName).SequenceEqual(new[] { common.Id, pair.Id, own.Id }),
            "SET_CHARACTER_FILTER distinct host Characters sharing one logical name remain a definite reference");
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsMultiTypeCompatibility);
        Assert(!session.HasSettingsReferenceCharacter && Visible(session).SequenceEqual(new[] { legacy.Id }),
            "SET_CHARACTER_FILTER compatibility context is not narrowed by a captured character");
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsGeneric);
        Assert(!session.HasSettingsReferenceCharacter && Visible(session).Length == 0 && session.SelectedGenericSet?.Id == generic.Id,
            "SET_CHARACTER_FILTER Generic retains its separate Set model and never applies this targeted character filter");

        var editing = Session(a); editing.OpenPaletteForEditing(editing.Palettes.Single(x => x.Id == own.Id));
        editing.ShowOtherCharacterSets = true;
        var active = editing.Palettes.Single(x => x.Id == other.Id); editing.SelectedPalette = active;
        active.MinimumCount = "入力途中"; active.CharacterName = "Filter C";
        active.CharacterRestricted = false; active.CharacterRestricted = true;
        active.TypeChoices.Single(x => x.Key == voiceKey).Selected = false;
        editing.ShowOtherCharacterSets = false;
        Assert(ReferenceEquals(editing.SelectedPalette, active) && Visible(editing).Contains(active.Id) &&
            active.MinimumCount == "入力途中" && active.CharacterName == "Filter C" && !active.TypeChoices.Single(x => x.Key == voiceKey).Selected &&
            editing.Palettes.Select(x => x.Id).SequenceEqual(order),
            "SET_CHARACTER_FILTER toggling or editing conditions retains the active Set, invalid text, incomplete target and every underlying Set/order");
        active.TypeChoices.Single(x => x.Key == voiceKey).Selected = true; active.MinimumCount = "1";
        active.CharacterName = "Filter B";
        editing.ShowOtherCharacterSets = true; editing.SelectedPalette = editing.Palettes.Single(x => x.Id == common.Id);
        editing.ShowOtherCharacterSets = false;
        Assert(!Visible(editing).Contains(other.Id) && editing.SettingsReferenceCharacterLabel.Contains("Filter A", StringComparison.Ordinal),
            "SET_CHARACTER_FILTER the previous other-character draft stops being pinned once a different Set is actively edited; reference remains fixed");
        Assert(JsonSerializer.Serialize(fixture) == sourceJson,
            "SET_CHARACTER_FILTER all visibility and incomplete edits leave the original settings snapshot unchanged");

        var directory = Path.Combine(output, "settings-character-filter"); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json"); if (File.Exists(path)) File.Delete(path);
        var store = new PlacerSettingsStore(path); store.Load(); store.Save(fixture);
        var transaction = new SettingsEditTransaction(store, fixture);
        var savedDraft = Session(a); VoiceParent(savedDraft); savedDraft.ShowOtherCharacterSets = true;
        var committed = transaction.Commit(fixture, savedDraft.Build()); savedDraft.AcceptCommitted(committed);
        var loaded = new PlacerSettingsStore(path).Load();
        var reloaded = new IntentSettingsSession(loaded, new[] { typeof(VoiceItem) }, [a]); VoiceParent(reloaded);
        var presentation = new PalettePresentationDraft(loaded.Presentation); presentation.FixedColumns = "6";
        Assert(loaded.Presentation.ShowOtherCharacterSets && reloaded.ShowOtherCharacterSets && !savedDraft.HasChanges &&
            Visible(reloaded).SequenceEqual(allVoice) && presentation.Build().ShowOtherCharacterSets &&
            PlacerSettingsStore.Copy(loaded).Presentation.ShowOtherCharacterSets && loaded.IntentPalettes.Select(x => x.Id).SequenceEqual(order),
            "SET_CHARACTER_FILTER protected commit, JSON reload, presentation reconstruction and source copy retain visibility preference and manual order");
        var stable = File.ReadAllBytes(path);
        var invalidDraft = new IntentSettingsSession(committed, new[] { typeof(VoiceItem) }, [a]); VoiceParent(invalidDraft);
        var invalidActive = invalidDraft.SelectedPalette!; invalidActive.MinimumCount = "入力途中"; invalidDraft.ShowOtherCharacterSets = false;
        var invalidRejected = false;
        try { transaction.Commit(committed, invalidDraft.Build()); } catch (InvalidOperationException) { invalidRejected = true; }
        Assert(invalidRejected && invalidActive.MinimumCount == "入力途中" && !invalidDraft.ShowOtherCharacterSets &&
            File.ReadAllBytes(path).SequenceEqual(stable),
            "SET_CHARACTER_FILTER toggling with invalid input retains both pending values and cannot persist through a second route");
        var rolledBack = transaction.Rollback(committed);
        Assert(!rolledBack.Presentation.ShowOtherCharacterSets && !new PlacerSettingsStore(path).Load().Presentation.ShowOtherCharacterSets,
            "SET_CHARACTER_FILTER the existing session rollback restores the opening UI preference");
        var external = File.ReadAllBytes(path).Concat(new byte[] { 10 }).ToArray(); File.WriteAllBytes(path, external);
        var rejected = false;
        try { transaction.Commit(rolledBack, committed); } catch (InvalidOperationException) { rejected = true; }
        Assert(rejected && File.ReadAllBytes(path).SequenceEqual(external),
            "SET_CHARACTER_FILTER visibility preference saves obey the existing external-modification/digest guard");

        foreach (var damaged in new[] { "{ broken json", "{\"Schema\":5}" })
        {
            var damagedPath = Path.Combine(directory, "damaged.json"); File.WriteAllText(damagedPath, damaged);
            var damagedStore = new PlacerSettingsStore(damagedPath);
            try { damagedStore.Load(); } catch (Exception ex) when (ex is InvalidDataException or JsonException) { }
            var saveRejected = false;
            try { damagedStore.Save(committed); } catch (InvalidOperationException) { saveRejected = true; }
            Assert(saveRejected && File.ReadAllText(damagedPath) == damaged,
                "SET_CHARACTER_FILTER visibility preference has no bypass for corrupt or future-version protected settings");
        }

        scope.Apply(fixture, [a, b], [a]);
        var vm = ViewModel!; vm.BeginIntentSettings();
        vm.IntentSettings!.OpenPaletteForEditing(vm.IntentSettings.Palettes.Single(x => x.Id == own.Id));
        var timelineBefore = Signature(timeline);
        var runtimeContext = IntentSelectionContext.Capture(timeline);
        vm.IntentSettings.ShowOtherCharacterSets = true; vm.SaveIntentSettings();
        Assert(!other.Target.Matches(runtimeContext) && own.Target.Matches(runtimeContext) && scope.Current.Presentation.ShowOtherCharacterSets,
            "SET_CHARACTER_FILTER exposing other-character Sets never weakens runtime Target.Matches applicability");
        vm.BeginPanelQuickSettings(); vm.PanelQuickPresentation!.FixedColumns = "7"; await Idle();
        Assert(scope.Current.Presentation.ShowOtherCharacterSets && scope.Current.Presentation.FixedColumns == 7 &&
            new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.ShowOtherCharacterSets,
            "SET_CHARACTER_FILTER real panel quick-settings protected save preserves the full Settings visibility preference");
        vm.EndPanelQuickSettings();
        vm.BeginIntentSettings(); vm.IntentSettings!.OpenPaletteForEditing(vm.IntentSettings.Palettes.Single(x => x.Id == own.Id));
        vm.IntentSettings.ShowOtherCharacterSets = false; vm.SaveIntentSettings();
        vm.IntentSettings.UpdateSelectionContext([b]); vm.RollbackSessionSettings();
        Assert(scope.Current.Presentation.ShowOtherCharacterSets && vm.IntentSettings!.ShowOtherCharacterSets &&
            vm.IntentSettings.SettingsReferenceCharacterLabel.Contains("Filter A", StringComparison.Ordinal) &&
            vm.IntentSettings.SelectedPalette?.Id == own.Id && Signature(timeline) == timelineBefore,
            "SET_CHARACTER_FILTER root rollback restores the opening preference, editing character and active Set while Timeline stays zero-write");
        Log("SETTINGS_CHARACTER_FILTER=PASS");
    }
}
