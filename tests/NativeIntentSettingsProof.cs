using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
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
        stage = "R11 settings workspace";
        var apiPath = Path.Combine(output, "relative-public-api.txt");
        if (File.Exists(apiPath)) foreach (var line in File.ReadAllLines(apiPath)) Log("R10 public API: " + line);
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (PlacerSettings)field.GetValue(vm)!;
        var rootStore = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var originalDisk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
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
            Log($"R11 before create: selected={timeline.SelectedItems.Count}; context={string.Join("|", timeline.SelectedItems.Select(x => x.GetType().Name + ":" + ItemCharacters.Get(x)?.Name))}; draftCount={vm.IntentSettings?.Palettes.Count}; sameVM={ReferenceEquals(panel.DataContext, vm)}");
            await InvokeSelectionButton(panel.NewPaletteButton);
            var session = vm.IntentSettings!; var draft = session.SelectedPalette!;
            Log($"R11 after create: count={session.Palettes.Count}; target={draft?.CharacterName}; expected={character.Name}; keys={string.Join("|", draft?.TypeChoices.Where(x => x.Selected).Select(x => x.Key) ?? [])}; selection={timeline.SelectedItems.Count}; error={vm.HasError}; status={vm.Status}");
            Assert(session.Palettes.Count == 1 && draft != null && draft.TypeChoices.Single(x => x.Selected).Key == IntentSelectionContext.TypeKey(typeof(VoiceItem)) && draft.CharacterName == character.Name,
                "R11 create captures explicit live runtime type and logical CharacterName without a closed item enum");
            draft = session.SelectedPalette!;
            panel.SetNameBox.Text = "表情セット"; panel.SetNameBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
            Assert(panel.FindName("IntentNameBox") == null && draft.Intent == "表情", "R11/R2-C new Voice Set automatically retains bounded compatibility Intent without a second name input");
            Assert(vm.IntentAlignments.Select(x => x.Value).SequenceEqual([IntentAlignment.StartAtAnchor, IntentAlignment.CenterAtAnchor, IntentAlignment.EndAtAnchor]),
                "PLACEMENT_RULE P1 Settings exposes start/center/end alignment in user order");
            draft.Anchor = IntentAnchor.SelectedCenter; draft.Duration = IntentDuration.Fixed; draft.FixedDuration = "30"; draft.Alignment = IntentAlignment.CenterAtAnchor;
            Assert(draft.Summary.Contains("演出の中央", StringComparison.Ordinal) && draft.Summary.Contains("選択アイテムの中央", StringComparison.Ordinal),
                "PLACEMENT_RULE P1 human-readable Set summary truthfully describes center alignment");
            Log("PLACEMENT_RULE_P1=PASS");
            draft.Alignment = IntentAlignment.StartAtAnchor; draft.Duration = IntentDuration.TargetSpan;
            Assert(vm.IntentLayerModes.Select(x => x.Value).SequenceEqual([LayerPlacementMode.RelativeToTarget, LayerPlacementMode.Absolute]),
                "PLACEMENT_RULE P2 Settings exposes target-relative then absolute layer modes");
            draft.LayerMode = LayerPlacementMode.Absolute; draft.AbsoluteLayer = "42"; draft.Direction = RelativeLayerDirection.Down;
            await Idle();
            Assert(draft.ShowAbsoluteLayerPlacement && !draft.ShowRelativeLayerPlacement &&
                panel.AbsoluteLayerRow.IsVisible && !panel.RelativeLayerRow.IsVisible &&
                draft.Summary.Contains("レイヤー42", StringComparison.Ordinal) && draft.Summary.Contains("下", StringComparison.Ordinal),
                "PLACEMENT_RULE P2 absolute Settings shows only relevant controls and summary describes layer/direction");
            draft.LayerMode = LayerPlacementMode.RelativeToTarget; draft.Direction = RelativeLayerDirection.Up;
            await Idle();
            Assert(draft.ShowRelativeLayerPlacement && !draft.ShowAbsoluteLayerPlacement &&
                panel.RelativeLayerRow.IsVisible && !panel.AbsoluteLayerRow.IsVisible,
                "PLACEMENT_RULE P2 switching back restores the existing target-relative Settings surface");
            draft.ExpressionCandidates = true;
            session.Sources.Single(x => ReferenceEquals(x.Source, sourceA)).Selected = true;
            session.Sources.Single(x => ReferenceEquals(x.Source, sourceB)).Selected = true;
            session.SourceSearch = "R11/Bundle";
            Assert(session.Sources.Count(x => x.Selected) == 2 && session.VisibleSources.Cast<object>().Count() == 1,
                "R11 filtering preserves checked sources across a bulk registration draft");
            Assert(panel.SourceEditor.IsVisible,
                "COMPACT_SETTINGS P1 bulk Template addition is a visible first-level section without a disclosure click");
            await Idle();
            await InvokeSelectionButton(panel.AddSourcesButton);
            Assert(draft.Entries.Count == 2 && draft.Entries.Single(x => x.Name == "Bundle").UseTemplateDuration,
                "R11 one bulk action includes singleton and multi-item templates with bundle duration preserved");
            var r11Disk = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            var r11ActiveTask = typeof(PlacerViewModel).GetField("activeTask", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm);
            Log($"R11 autosave trace: timelineSame={Signature(timeline) == signature}; baselineSame={JsonSerializer.Serialize(fixture) == baseline}; diskSets={r11Disk.IntentPalettes.Count}; diskEntries={(r11Disk.IntentPalettes.Count == 1 ? r11Disk.IntentPalettes[0].Entries.Count : -1)}; activeTask={r11ActiveTask}; commitNotice={vm.SettingsCommitNotice}");
            Assert(Signature(timeline) == signature && JsonSerializer.Serialize(fixture) == baseline &&
                r11Disk.IntentPalettes.Single().Entries.Count == 2,
                "R11/R4 valid bulk edits persist automatically; the opening snapshot, Timeline and original templates are not mutated");
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
            draft.Duration = IntentDuration.Fixed; draft.FixedDuration = "invalid";
            RejectWithoutMutation(timeline, () => session.Build(), "R11 invalid relevant numeric draft is rejected, not silently coerced");
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
            Assert(panel.SetManagement.IsVisible && panel.TargetEditor.IsVisible && panel.RelationEditor.IsVisible &&
                panel.EntryEditor.IsVisible && panel.SourceEditor.IsVisible &&
                !panel.TargetAdvanced.IsExpanded && !panel.RelationAdvanced.IsExpanded &&
                !panel.PresentationSettingsSurface.PresentationExpander.IsExpanded,
                "COMPACT_SETTINGS P1 ordinary Set sections are first-level visible while detailed/global disclosure stays folded");

            var sourceY = panel.SourceEditor.TranslatePoint(new Point(0, 0), panel.SettingsScroll).Y;
            var presentationY = panel.PresentationSettingsSurface.TranslatePoint(new Point(0, 0), panel.SettingsScroll).Y;
            Assert(presentationY > sourceY,
                "COMPACT_SETTINGS P1 global presentation controls are lower than the current Set/source editing flow");
            var shapeY = panel.SetShapeButtons.TranslatePoint(new Point(0, 0), panel.SettingsScroll).Y;
            Assert(shapeY > presentationY,
                "COMPACT_SETTINGS P4 Set shape remains available but moves below global presentation as a secondary full-Settings route");
            Assert(panel.FindName("DirectDeletePaletteButton") == null && panel.ManageDeletePaletteButton.IsVisible &&
                ReferenceEquals(panel.ManageDeletePaletteButton.Command, vm.DeleteIntentPaletteCommand),
                "COMPACT_SETTINGS P4 compact Settings has one visible destructive Set-delete route under Set management");

            var width = view.Width; var height = view.Height;
            try
            {
                view.Width = 360; view.Height = 400; await Idle(); panel.UpdateLayout();
                var sourceLeft = panel.SourceList.TranslatePoint(new Point(0, 0), panel.SourceEditor).X;
                var sourceRight = panel.SourceEditor.ActualWidth -
                    panel.SourceList.TranslatePoint(new Point(panel.SourceList.ActualWidth, 0), panel.SourceEditor).X;
                Assert(sourceLeft > sourceRight && sourceRight > 0 && panel.SourceList.ActualWidth > 0 &&
                    panel.SourceList.ActualWidth < panel.SourceEditor.ActualWidth &&
                    panel.SourceList.ActualWidth >= panel.SourceEditor.ActualWidth * 0.70,
                    "COMPACT_SETTINGS P2 narrow bulk-source list leaves wider left outer-scroll escape space without aggressively shrinking the list");
                Assert(Math.Abs(panel.PalettePicker.ActualHeight - panel.NewPaletteButton.ActualHeight) <= 4,
                    "COMPACT_SETTINGS P3 targeted Set picker and plus control have comparable native heights after duplicate delete removal");
                var entryLeft = panel.EntryList.TranslatePoint(new Point(0, 0), panel).X;
                var sourceListLeft = panel.SourceList.TranslatePoint(new Point(0, 0), panel).X;
                Assert(Math.Abs(entryLeft - sourceListLeft) <= 1,
                    "COMPACT_SETTINGS P4 entry and Template source lists share one visual content lane");
                Assert(panel.RollbackButton.IsVisible && panel.RollbackButton.ActualWidth > 0 && panel.ActualWidth <= 360 &&
                    panel.SettingsScroll.ExtentWidth <= panel.SettingsScroll.ViewportWidth + 1,
                    "COMPACT_SETTINGS P2 narrow Settings keeps rollback accessible and introduces no horizontal width overflow");
                Assert(panel.ContextNoticeText.TextWrapping == TextWrapping.Wrap,
                    "COMPACT_SETTINGS P4 narrow contextual text wraps instead of being horizontally clipped");
                SaveNamedView(view, "compact-settings-narrow-top.png");
                panel.SettingsScroll.ScrollToEnd(); await Idle(); panel.SourceList.BringIntoView(); await Idle(); panel.UpdateLayout();
                var gutterPoint = panel.SourceList.TranslatePoint(new Point(-8, Math.Max(1, panel.SourceList.ActualHeight / 2)), panel.SettingsScroll);
                Assert(gutterPoint.X >= 0 && gutterPoint.Y >= 0 &&
                    gutterPoint.X <= panel.SettingsScroll.ActualWidth && gutterPoint.Y <= panel.SettingsScroll.ActualHeight,
                    "COMPACT_SETTINGS P5 fixture brings the left source-list gutter into the live Settings viewport");
                var gutterHit = NestedWheelRouting.ResolveCurrentSource(panel.SettingsScroll, gutterPoint);
                var gutterBefore = panel.SettingsScroll.VerticalOffset;
                var gutterAccepted = gutterHit != null && NestedWheelRouting.TryScroll(panel.SettingsScroll, gutterHit, 120, ModifierKeys.None);
                await Idle(); panel.SettingsScroll.UpdateLayout();
                Log($"COMPACT_SETTINGS P5 gutter trace: hit={gutterHit?.GetType().FullName}; accepted={gutterAccepted}; before={gutterBefore:0.###}; after={panel.SettingsScroll.VerticalOffset:0.###}; scrollable={panel.SettingsScroll.ScrollableHeight:0.###}");
                Assert(gutterAccepted && panel.SettingsScroll.VerticalOffset < gutterBefore,
                    "COMPACT_SETTINGS P5 left source-list gutter is real outer-scroll space, not a wheel dead zone");
                panel.SettingsScroll.ScrollToEnd(); await Idle();
                SaveNamedView(view, "compact-settings-narrow-source.png");

                view.Width = 520; view.Height = 520; await Idle(); panel.SettingsScroll.ScrollToEnd(); panel.UpdateLayout();
                var normalLeft = panel.SourceList.TranslatePoint(new Point(0, 0), panel.SourceEditor).X;
                var normalRight = panel.SourceEditor.ActualWidth -
                    panel.SourceList.TranslatePoint(new Point(panel.SourceList.ActualWidth, 0), panel.SourceEditor).X;
                Assert(normalLeft > normalRight && normalRight > 0 &&
                    panel.SettingsScroll.ExtentWidth <= panel.SettingsScroll.ViewportWidth + 1,
                    "COMPACT_SETTINGS P2 normal-width source list retains asymmetric outer-scroll escape space without horizontal overflow");
                SaveNamedView(view, "compact-settings-normal-source.png");
                Log("COMPACT_SETTINGS_P1=PASS");
                Log("COMPACT_SETTINGS_P2=PASS");
                Log("COMPACT_SETTINGS_P3=PASS");
                Log("COMPACT_SETTINGS_P4=PASS");
                Log("COMPACT_SETTINGS_P5=PASS");
            }
            finally { view.Width = width; view.Height = height; panel.SettingsScroll.ScrollToHome(); await Idle(); }
            await InvokeSelectionButton(panel.RollbackButton);
            Assert(vm.IntentSettings!.Palettes.Count == 0 && !vm.IntentSettings.HasChanges && JsonSerializer.Serialize(fixture) == baseline && Signature(timeline) == signature,
                "R11/R4 session rollback restores opening settings and a clean draft without Timeline writes");
            Log("R11=PASS");
        }
        finally
        {
            if (originalDisk == null) File.Delete(PlacerSettingsStore.DefaultPath); else File.WriteAllBytes(PlacerSettingsStore.DefaultPath, originalDisk);
            rootStore.Load();
            ItemSettings.Default.Templates.Remove(sourceA); ItemSettings.Default.Templates.Remove(sourceB);
            field.SetValue(vm, original); timeline.Items = before; timeline.SelectedItems = selected; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(legacy); vm.RefreshIntentWorkspace(); vm.ResetIntentSettings();
        }
    }
}
