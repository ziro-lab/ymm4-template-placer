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
    private static async Task VerifyIntentSettings(Timeline timeline, UndoRedoManager undo)
    {
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (PlacerSettings)field.GetValue(vm)!;
        var before = timeline.Items; var selected = timeline.SelectedItems; var legacy = vm.UseLegacyWorkspace;
        var character = new Character { Name = "R11 Voice" };
        var voice = new VoiceItem(character) { Frame = 120, Length = 60, Layer = 20 };
        var sourceA = Template("R11/Single", [new TachieFaceItem(character) { Length = 10, Layer = 5 }]);
        var sourceB = Template("R11/Bundle", [new TachieFaceItem(character) { Frame = 15, Length = 20, Layer = 6 }, new TextItem { Frame = 30, Length = 12, Layer = 7 }]);
        ItemSettings.Default.Templates.Add(sourceA); ItemSettings.Default.Templates.Add(sourceB);
        try
        {
            var fixture = PlacerSettingsStore.Copy(original); fixture.IntentPalettes.Clear(); fixture.ExpressionBootstrapComplete = true;
            fixture.IntentPaletteRevision = 1; fixture.LegacyWorkspace = false;
            field.SetValue(vm, fixture); timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            var bytes = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : [];
            var signature = Signature(timeline); var baseline = JsonSerializer.Serialize(fixture);
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.ResetIntentSettings();
            vm.OpenIntentSettingsCommand.Execute(null); await Idle();
            Assert(ReferenceEquals(view.SelectionTab.Content, view.RelativeSettingsSurface) && view.RelativeSettingsSurface.IsVisible,
                "R11 settings is a separate native workspace, not ordinary action-tile controls");
            var panel = view.RelativeSettingsSurface;
            await InvokeSelectionButton(panel.NewPaletteButton);
            var session = vm.IntentSettings!; var draft = session.SelectedPalette!;
            Assert(session.Palettes.Count == 1 && draft.TypeChoices.Single(x => x.Selected).Key == IntentSelectionContext.TypeKey(typeof(VoiceItem)) && draft.CharacterName == character.Name,
                "R11 create captures explicit live runtime type and logical CharacterName without a closed item enum");
            panel.SetNameBox.Text = "表情セット"; panel.SetNameBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
            panel.IntentNameBox.Text = "表情"; panel.IntentNameBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
            draft.ExpressionCandidates = true;
            session.Sources.Single(x => ReferenceEquals(x.Source, sourceA)).Selected = true;
            session.Sources.Single(x => ReferenceEquals(x.Source, sourceB)).Selected = true;
            session.SourceSearch = "Bundle";
            Assert(session.Sources.Count(x => x.Selected) == 2 && session.VisibleSources.Cast<object>().Count() == 1,
                "R11 filtering preserves checked sources across a bulk registration draft");
            panel.SourceEditor.IsExpanded = true; await Idle();
            await InvokeSelectionButton(panel.AddSourcesButton);
            Assert(draft.Entries.Count == 2 && draft.Entries.Single(x => x.Name == sourceB.Name).UseTemplateDuration,
                "R11 one bulk action includes singleton and multi-item templates with bundle duration preserved");
            Assert(Signature(timeline) == signature && JsonSerializer.Serialize(fixture) == baseline &&
                (!File.Exists(PlacerSettingsStore.DefaultPath) || File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(bytes)),
                "R11 editing and bulk addition change only the settings draft, never Timeline, live settings or disk");
            var first = draft.Entries[0]; draft.SelectedEntry = first; session.MoveEntry(1);
            Assert(ReferenceEquals(draft.Entries[1], first), "R11 explicit user entry order is represented without automatic sorting");
            session.Duplicate();
            Assert(session.Palettes.Count == 2 && session.SelectedPalette!.Id != draft.Id && session.SelectedPalette.Name == "表情セット 2" &&
                !ReferenceEquals(session.SelectedPalette.Entries[0], draft.Entries[0]), "R11 duplicate creates an independent same-character set and overrides");
            session.SelectedPalette = draft;
            var bad = Template("R11/BadCharacter", [new TachieFaceItem(new Character { Name = "Other" }) { Length = 10 }]);
            ItemSettings.Default.Templates.Add(bad);
            try
            {
                var negative = new IntentSettingsSession(session.Build(), new[] { typeof(VoiceItem), typeof(TextItem) });
                negative.SelectedPalette = negative.Palettes.First();
                negative.Sources.Single(x => ReferenceEquals(x.Source, sourceA)).Selected = true;
                negative.Sources.Single(x => ReferenceEquals(x.Source, bad)).Selected = true;
                var beforeNegative = JsonSerializer.Serialize(negative.Build());
                RejectWithoutMutation(timeline, () => negative.AddSelectedSources(), "R11 mixed valid/invalid source batch fails atomically");
                Assert(JsonSerializer.Serialize(negative.Build()) == beforeNegative && negative.Sources.Count(x => x.Selected) == 2,
                    "R11 failed batch preserves all checked sources and the previous draft");
            }
            finally { ItemSettings.Default.Templates.Remove(bad); }
            draft.FixedDuration = "invalid";
            RejectWithoutMutation(timeline, () => session.Build(), "R11 invalid numeric draft is rejected, not silently coerced");
            Assert(draft.FixedDuration == "invalid" && session.HasChanges, "R11 invalid draft text remains editable after failed validation");
            draft.FixedDuration = "30";
            var built = session.Build();
            var proofPath = Path.Combine(output, "r11-settings-roundtrip.json"); var store = new PlacerSettingsStore(proofPath); store.Load(); store.Save(built);
            var read = new PlacerSettingsStore(proofPath).Load();
            Assert(JsonSerializer.Serialize(read) == JsonSerializer.Serialize(built) && read.IntentPalettes.Count == 2,
                "R11 complete settings draft roundtrips through the protected atomic settings store");
            var removed = draft.Entries[0]; draft.Entries.Remove(removed);
            session.ImportNewExpressions();
            Assert(!session.Palettes.Single(x => x.Id == draft.Id).Entries.Any(x => x.LibraryEntryId == removed.LibraryEntryId),
                "R11 explicit new-expression import does not resurrect a user-removed manual registration");
            var width = view.Width; var height = view.Height;
            try
            {
                view.Width = 360; view.Height = 400; await Idle(); SaveNamedView(view, "r11-settings-narrow.png");
                Assert(panel.SaveButton.IsVisible && panel.SaveButton.ActualWidth > 0 && panel.ActualWidth <= 360,
                    "R11 narrow native settings keeps the save action accessible outside the scrollable editor");
            }
            finally { view.Width = width; view.Height = height; await Idle(); }
            await InvokeSelectionButton(panel.DiscardButton);
            Assert(vm.IntentSettings!.Palettes.Count == 0 && !vm.IntentSettings.HasChanges && JsonSerializer.Serialize(fixture) == baseline && Signature(timeline) == signature,
                "R11 discard restores live settings into a clean draft without persistent or Timeline writes");
            Log("R11=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(sourceA); ItemSettings.Default.Templates.Remove(sourceB);
            field.SetValue(vm, original); timeline.Items = before; timeline.SelectedItems = selected; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(legacy); vm.RefreshIntentWorkspace(); vm.ResetIntentSettings();
        }
    }
}
