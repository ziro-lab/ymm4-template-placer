using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnRound4Settings(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R4-C automatic Settings and session rollback";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!;
        var character = new Character { Name = "R4C Voice" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 30, Layer = 20 };
        var source = scope.AddTemplate("R4C/face", new TachieFaceItem(character) { Length = 11 });
        var style = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "開始の汎用", null, [])
        { Layer = new() { UseTemplateLayer = false, Minimum = 2, Maximum = 20, Preferred = 8 } };
        var expression = new IntentPalette(Guid.NewGuid(), "開始の表情", "表情", new()
        { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name }, new(), [new(source.Id)])
        { ExpressionCandidates = true };
        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.Library = [source]; fixture.Palettes = [style]; fixture.IntentPalettes = [expression];
        fixture.ImportedExpressionSources = [source.Source]; fixture.ExpressionBootstrapComplete = true; fixture.IntentPaletteRevision = 1;
        fixture.ManualStylePaletteId = style.Id; fixture.ManualCharacterPaletteId = null;
        scope.Apply(fixture, [voice], [voice]); vm.BeginIntentSettings();
        vm.SaveIntentSettings(); // Fixture persistence only; assertions below never use a Save button.
        view.SelectionTab.IsSelected = true; await Idle();
        var panel = view.RelativeSettingsSurface; var session = vm.IntentSettings!;
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsGeneric); await Idle();
        var draft = session.SelectedGenericSet!; var opening = JsonSerializer.Serialize(scope.Current);
        var signature = Signature(timeline); var sourceSignature = source.Source;
        var rows = vm.Rows.ToArray();
        Round4Assert(panel.FindName("SaveButton") == null, "C1", "normal Settings no longer contains a Save button");
        Round4Assert(panel.FindName("DiscardButton") == null, "C2", "normal Settings no longer contains the old generic Discard button");
        Round4Assert(panel.RollbackButton.IsVisible && panel.RollbackButton.Command == vm.RollbackIntentSettingsCommand &&
            panel.RollbackButton.Content?.ToString() == "今回の変更を戻す", "C3", "the visible session-rollback button is bound to the real root command");
        var revision = vm.GetSettingsAutoCommitCountForProof();
        var bytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        panel.GenericSettingsSurface.GenericSetNameBox.Text = "自動反映されたセット";
        panel.GenericSettingsSurface.GenericSetNameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        session.Presentation.FixedColumns = "5";
        Round4Assert(session.HasChanges && JsonSerializer.Serialize(scope.Current) == opening &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(bytes), "C5", "the working draft is separate from live/opening state until the queued commit runs");
        await Idle();
        Round4Assert(!vm.HasError && !session.HasChanges && scope.Current.Palettes.Single().Name == "自動反映されたセット" &&
            new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.FixedColumns == 5,
            "C6", "real TextBox edits automatically persist valid settings with no Save input");
        Round4Assert(vm.GetSettingsAutoCommitCountForProof() == revision + 1,
            "C7", "multiple synchronous edit notifications coalesce into one root-dispatched commit");
        Round4Assert(vm.GetSettingsOpeningForProof() == opening && vm.HasSessionSettingsChanges,
            "C4", "successful automatic commits leave the immutable session-opening baseline intact");
        Round4Assert(ReferenceEquals(session, vm.IntentSettings) && ReferenceEquals(draft, session.SelectedGenericSet) && session.IsGenericContext,
            "C13", "automatic publication retains the selected Settings context, Set and bound draft identity");
        bytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath); revision = vm.GetSettingsAutoCommitCountForProof();
        draft.Preferred = "入力途中"; draft.Name = ""; await Idle();
        Round4Assert(vm.GetSettingsAutoCommitCountForProof() == revision && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(bytes),
            "C8", "complete draft validation prevents all writes when any relevant setting is invalid");
        Round4Assert(scope.Current.Palettes.Single().Layer.Preferred == 8 && scope.Current.Palettes.Single().Name == "自動反映されたセット",
            "C9", "incomplete numeric and empty-name text cannot overwrite the last valid saved values");
        Round4Assert(draft.Preferred == "入力途中" && draft.Name == "" && session.HasChanges &&
            panel.SettingsCommitNoticeText.Text.StartsWith("反映待ち", StringComparison.Ordinal),
            "C10", "invalid input is retained in its existing controls with a visible pending-validation notice");
        view.PaletteTab.IsSelected = true; await Idle(); view.SelectionTab.IsSelected = true; await Idle();
        Round4Assert(ReferenceEquals(session, vm.IntentSettings) && draft.Preferred == "入力途中" && draft.Name == "" &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(bytes),
            "C22", "leaving and reopening Settings retains an invalid session rather than silently discarding it");
        draft.Name = "修正済み"; draft.Preferred = "9"; await Idle();
        Round4Assert(!vm.HasError && !session.HasChanges &&
            JsonSerializer.Serialize(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load()) == JsonSerializer.Serialize(scope.Current),
            "C11", "corrected input commits through the existing protected store and roundtrips exactly");
        session.Palettes.Single().ExpressionCandidates = false; await Idle();
        var missing = !vm.Rows.Single().HasCandidates;
        session.Palettes.Single().ExpressionCandidates = true; await Idle();
        Round4Assert(missing && vm.Rows.Single().HasCandidates && vm.Rows.SequenceEqual(rows),
            "C14", "automatic Settings commits refresh expression candidates without rebuilding unchanged Voice rows");
        var added = scope.AddTemplate("R4C/new-expression", new TachieFaceItem(character) { Length = 13 });
        await Idle();
        Round4Assert(!scope.Current.Library.Any(x => x.Source == added.Source) && vm.RescanIntentExpressionsCommand.CanExecute(null),
            "C19", "new host expression templates are not implicitly imported; import remains an explicit command");
        vm.RescanIntentExpressionsCommand.Execute(null); await Idle();
        Round4Assert(scope.Current.Library.Any(x => x.Source == added.Source) && vm.GetSettingsOpeningForProof() == opening,
            "C20", "explicit expression import auto-commits inside the same rollback session");
        await InvokeSelectionButton(panel.RollbackButton); await Idle();
        Round4Assert(JsonSerializer.Serialize(scope.Current) == opening && !vm.HasSessionSettingsChanges,
            "C15", "one user rollback restores the exact opening snapshot including this session's explicit import");
        Round4Assert(JsonSerializer.Serialize(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load()) == opening,
            "C16", "session rollback itself persists through the same protected atomic store");
        Round4Assert(Signature(timeline) == signature && timeline.Items.Single() == voice,
            "C17", "all automatic Settings changes and rollback leave Timeline content untouched");
        Round4Assert(TemplateResolver.RequireBundle(source).Items.Single() is TachieFaceItem { Length: 11 } && source.Source == sourceSignature &&
            ItemSettings.Default.Templates.Any(x => added.Source.Matches(x)),
            "C18", "rollback never edits original YMM4 templates, including the newly discovered source");
        session = vm.IntentSettings!; session.SelectedGenericSet!.Name = "退出時反映";
        view.PaletteTab.IsSelected = true;
        Round4Assert(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Palettes.Single().Name == "退出時反映" && !session.HasChanges,
            "C21", "leaving Settings synchronously flushes a valid queued edit before closing its session");
        await Idle(); view.SelectionTab.IsSelected = true; await Idle();
        var current = JsonSerializer.Serialize(scope.Current); bytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        session = vm.IntentSettings!; session.SelectedGenericSet!.Name = "外部変更と競合";
        var external = bytes.Concat(new byte[] { 10 }).ToArray(); File.WriteAllBytes(PlacerSettingsStore.DefaultPath, external);
        await Idle();
        var rejectedAuto = vm.HasError && session.HasChanges && JsonSerializer.Serialize(scope.Current) == current &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(external);
        vm.RollbackIntentSettingsCommand.Execute(null);
        Round4Assert(rejectedAuto && vm.HasError && ReferenceEquals(session, vm.IntentSettings) && session.SelectedGenericSet.Name == "外部変更と競合" &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(external),
            "C12", "automatic commit and explicit rollback both reject external conflicts without overwriting bytes or losing the draft");
        File.WriteAllBytes(PlacerSettingsStore.DefaultPath, bytes);
        SaveNamedView(view, "v042-round4-c-settings-auto-rollback-360.png");
        Round4Phase("C");
    }
}

public sealed partial class PlacerViewModel
{
    internal int GetSettingsAutoCommitCountForProof() => settingsCommitRevision;
    internal string GetSettingsOpeningForProof() => JsonSerializer.Serialize(RequireSettingsTransaction().CreateRollbackCandidate());
}
