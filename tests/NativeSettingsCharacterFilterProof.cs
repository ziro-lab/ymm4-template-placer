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
        stage = "Settings explicit character Set filter";
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
            IntentPalettes = [other, common, pair, text, own, third, legacy], Palettes = [generic], ManualStylePaletteId = generic.Id,
            Presentation = new() { ShowOtherCharacterSets = true } };
        var sourceJson = JsonSerializer.Serialize(fixture);
        IntentSettingsSession Session(params IItem[] selection) => new(fixture, new[] { typeof(VoiceItem), typeof(TextItem) }, selection);
        static Guid[] Visible(IntentSettingsSession session) => session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).ToArray();
        void VoiceParent(IntentSettingsSession session) => session.SelectedItemContext = session.ItemContexts.Single(x => x.Key == voiceKey);
        var allVoice = new[] { other.Id, common.Id, pair.Id, own.Id, third.Id };

        var session = Session(a);
        Assert(!session.HasCharacterFilterOptions && session.CharacterFilterCharacter == null &&
            Visible(session).SequenceEqual(new[] { common.Id, own.Id }),
            "SET_CHARACTER_FILTER current-selection context keeps strict applicability and does not expose the manual character filter");

        VoiceParent(session);
        Assert(session.HasCharacterFilterOptions && session.CharacterFilterCharacter == null &&
            session.CharacterFilterOptions.Select(x => x.Label).SequenceEqual(new[] { "すべて", "Filter B", "Filter A", "Filter C" }) &&
            Visible(session).SequenceEqual(allVoice),
            "SET_CHARACTER_FILTER manual Item parent derives unique character names from saved Set order and defaults to all Sets");

        session.CharacterFilterCharacter = "Filter A";
        Assert(Visible(session).SequenceEqual(new[] { pair.Id, own.Id }) && !Visible(session).Contains(common.Id) && !session.HasChanges,
            "SET_CHARACTER_FILTER selecting a character shows only Sets that explicitly contain that character, excluding unrestricted Sets");
        session.CharacterFilterCharacter = "Filter B";
        Assert(Visible(session).SequenceEqual(new[] { other.Id }) && session.SelectedPalette?.Id == other.Id,
            "SET_CHARACTER_FILTER switching the explicit character immediately moves selection into the filtered list");
        session.CharacterFilterCharacter = null;
        Assert(Visible(session).SequenceEqual(allVoice),
            "SET_CHARACTER_FILTER all restores every same-Item Set without changing manual order");

        session.CharacterFilterCharacter = "Filter A";
        session.SelectedPalette = session.Palettes.Single(x => x.Id == own.Id);
        var active = session.SelectedPalette!;
        active.CharacterName = "Filter C";
        Assert(Visible(session).Contains(active.Id) && session.CharacterFilterCharacter == "Filter A",
            "SET_CHARACTER_FILTER an actively edited Set remains bound while its own character field changes");
        session.CharacterFilterCharacter = "Filter B";
        Assert(!Visible(session).Contains(active.Id) && session.SelectedPalette?.Id == other.Id,
            "SET_CHARACTER_FILTER an explicit filter change clears the edit pin and is authoritative");

        session.SelectedItemContext = session.ItemContexts.Single(x => x.Key == textKey);
        Assert(session.CharacterFilterCharacter == null && !session.HasCharacterFilterOptions &&
            Visible(session).SequenceEqual(new[] { text.Id }),
            "SET_CHARACTER_FILTER changing Item parent resets the transient filter and hides it when that parent has no configured character");
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsGeneric);
        Assert(!session.HasCharacterFilterOptions && session.CharacterFilterCharacter == null &&
            Visible(session).Length == 0 && session.SelectedGenericSet?.Id == generic.Id,
            "SET_CHARACTER_FILTER Generic keeps its separate Set model and never exposes the character filter");
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsMultiTypeCompatibility);
        Assert(!session.HasCharacterFilterOptions && Visible(session).SequenceEqual(new[] { legacy.Id }),
            "SET_CHARACTER_FILTER legacy multi-type compatibility keeps its own unfiltered semantics");

        var opened = Session();
        opened.OpenPaletteForEditing(opened.Palettes.Single(x => x.Id == own.Id));
        Assert(opened.SelectedItemContext?.Key == voiceKey && opened.CharacterFilterCharacter == null &&
            opened.HasCharacterFilterOptions && opened.SelectedPalette?.Id == own.Id,
            "SET_CHARACTER_FILTER opening a Set directly enters its Item parent with the explicit filter at all");
        Assert(JsonSerializer.Serialize(fixture) == sourceJson,
            "SET_CHARACTER_FILTER navigation and filtering never mutate the source settings snapshot");

        scope.Apply(fixture, [a, b], [a]);
        var vm = ViewModel!; vm.BeginIntentSettings(); await Idle();
        Assert(vm.IntentSettings!.SelectedItemContext?.IsCurrentSelection == true &&
            View!.RelativeSettingsSurface.SettingsCharacterFilterRow.Visibility == System.Windows.Visibility.Collapsed,
            "SET_CHARACTER_FILTER actual UI hides the filter in strict current-selection mode");

        vm.IntentSettings.OpenPaletteForEditing(vm.IntentSettings.Palettes.Single(x => x.Id == own.Id)); await Idle();
        var panel = View!.RelativeSettingsSurface;
        Assert(panel.SettingsCharacterFilterRow.Visibility == System.Windows.Visibility.Visible &&
            panel.CharacterFilterPicker.Items.Count == 4 &&
            panel.CharacterFilterPicker.Text == "すべて",
            "SET_CHARACTER_FILTER actual UI shows キャラで絞り込み with all plus the configured character names");

        panel.CharacterFilterPicker.SelectedValue = "Filter A"; await Idle();
        Assert(vm.IntentSettings.CharacterFilterCharacter == "Filter A" &&
            vm.IntentSettings.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(new[] { pair.Id, own.Id }) &&
            !vm.IntentSettings.HasChanges,
            "SET_CHARACTER_FILTER actual ComboBox filters the Set picker to the selected character without creating a settings edit");

        panel.CharacterFilterPicker.SelectedValue = "Filter B"; await Idle();
        Assert(vm.IntentSettings.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(new[] { other.Id }) &&
            vm.IntentSettings.SelectedPalette?.Id == other.Id,
            "SET_CHARACTER_FILTER actual ComboBox switches to another character and reselects a visible Set");

        panel.CharacterFilterPicker.SelectedIndex = 0; await Idle();
        Assert(vm.IntentSettings.CharacterFilterCharacter == null &&
            vm.IntentSettings.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(allVoice),
            "SET_CHARACTER_FILTER actual すべて entry restores all same-Item Sets");

        var timelineBefore = Signature(timeline);
        var runtimeContext = IntentSelectionContext.Capture(timeline);
        panel.CharacterFilterPicker.SelectedValue = "Filter B"; await Idle();
        Assert(!other.Target.Matches(runtimeContext) && own.Target.Matches(runtimeContext) &&
            Signature(timeline) == timelineBefore && JsonSerializer.Serialize(fixture) == sourceJson,
            "SET_CHARACTER_FILTER Settings-only filtering never weakens runtime Target.Matches or writes Timeline/settings");
        Log("SETTINGS_CHARACTER_FILTER=PASS");
    }
}
