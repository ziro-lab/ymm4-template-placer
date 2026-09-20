using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void VerifyHandsOnRound4SettingsSession(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R4-C/CS root session integration";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.ExpressionBootstrapComplete = true; fixture.IntentPaletteRevision = 1;
        fixture.Library = []; fixture.IntentPalettes = [];
        var style = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "開始時", null, [])
        { Layer = new() { UseTemplateLayer = false, Minimum = 2, Maximum = 20, Preferred = 8 } };
        fixture.Palettes = [style]; fixture.ManualStylePaletteId = style.Id; fixture.ManualCharacterPaletteId = null;
        scope.Apply(fixture, [], []); vm.BeginIntentSettings();
        var initial = JsonSerializer.Serialize(scope.Current); var timelineBefore = Signature(timeline);
        var session = vm.IntentSettings!;
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsGeneric);
        var draft = session.SelectedGenericSet!; session.SourceSearch = "retain-filter";
        Round4Assert(!vm.HasSessionSettingsChanges && !session.HasChanges, "CS1", "root session opens clean without a settings edit");
        draft.Name = "一回目"; vm.SaveIntentSettings();
        Round4Assert(ReferenceEquals(vm.IntentSettings, session) && ReferenceEquals(session.SelectedGenericSet, draft) &&
            session.SourceSearch == "retain-filter" && session.IsGenericContext && !session.HasChanges,
            "CS2", "successful root commit retains bound draft identity, selected Set/context and source filter");
        Round4Assert(vm.HasSessionSettingsChanges && scope.Current.Palettes.Single().Name == "一回目" &&
            new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Palettes.Single().Name == "一回目",
            "CS3", "root publishes the committed settings only after durable protected persistence");
        draft.Name = "二回目"; session.Presentation.FixedColumns = "5"; vm.SaveIntentSettings();
        Round4Assert(ReferenceEquals(vm.IntentSettings, session) && !session.HasChanges && scope.Current.Presentation.FixedColumns == 5,
            "CS4", "a second root commit uses latest transaction fingerprint without replacing the opening session");
        var stable = File.ReadAllBytes(PlacerSettingsStore.DefaultPath); draft.Preferred = "途中";
        var rejected = false;
        try { vm.SaveIntentSettings(); } catch (InvalidOperationException) { rejected = true; }
        Round4Assert(rejected && session.HasChanges && draft.Preferred == "途中" &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(stable),
            "CS5", "invalid root draft remains editable and does not overwrite committed settings");
        vm.RollbackSessionSettings();
        Round4Assert(JsonSerializer.Serialize(scope.Current) == initial &&
            JsonSerializer.Serialize(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load()) == initial &&
            !vm.HasSessionSettingsChanges, "CS6", "root rollback after multiple commits restores the original session-opening state");
        Round4Assert(vm.IntentSettings!.IsGenericContext && vm.IntentSettings.SelectedGenericSet?.Id == style.Id &&
            vm.IntentSettings.SourceSearch == "retain-filter", "CS7", "rollback preserves navigational context for surviving Sets");
        vm.IntentSettings.SelectedGenericSet!.Name = "また変更"; vm.SaveIntentSettings(); vm.RollbackSessionSettings();
        Round4Assert(JsonSerializer.Serialize(scope.Current) == initial && !vm.HasSessionSettingsChanges,
            "CS8", "root edit after rollback still uses the immutable original session baseline");
        stable = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        var external = stable.Concat(new byte[] { 10 }).ToArray(); File.WriteAllBytes(PlacerSettingsStore.DefaultPath, external);
        var kept = vm.IntentSettings; rejected = false;
        try { vm.RollbackSessionSettings(); } catch (InvalidOperationException) { rejected = true; }
        Round4Assert(rejected && ReferenceEquals(kept, vm.IntentSettings) &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(external) && JsonSerializer.Serialize(scope.Current) == initial,
            "CS9", "root rollback rejects external changes without publishing a replacement draft or overwriting bytes");
        Round4Assert(Signature(timeline) == timelineBefore, "CS10", "root settings commit and rollback tests are Timeline zero-write");
        Round4Phase("CS");
    }
}
