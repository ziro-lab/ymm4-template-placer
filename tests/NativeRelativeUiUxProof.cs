using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyRelativeUiUx(Timeline timeline, UndoRedoManager undo)
    {
        stage = "v0.4.2 relative UI/UX acceptance";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var original = (PlacerSettings)field.GetValue(vm)!; var items = timeline.Items; var selection = timeline.SelectedItems;
        var mode = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
        var character = new Character { Name = "UX Character" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 60, Layer = 20, Serif = "今日もやっていきましょう" };
        var next = new VoiceItem(character) { Frame = 220, Length = 30, Layer = 20, Serif = "次のセリフ" };
        var text = new TextItem { Frame = 160, Length = 30, Layer = 30 };
        var sources = new[]
        {
            Template("UX/Smile", [new TachieFaceItem(character) { Length = 15, Layer = 4 }]),
            Template("UX/Doya", [new TachieFaceItem(character) { Length = 15, Layer = 4 }]),
            Template("UX/React", [new TextItem { Length = 20, Layer = 3 }])
        };
        foreach (var source in sources) ItemSettings.Default.Templates.Add(source);
        try
        {
            var refs = new[]
            {
                TemplateResolver.Reference(sources[0], "笑顔", character.Name),
                TemplateResolver.Reference(sources[1], "どや", character.Name),
                TemplateResolver.Reference(sources[2], "強調", null)
            };
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var expressionRelation = new IntentRelation { Duration = IntentDuration.UntilRelated, Neighbor = IntentNeighbor.NextSameTypeAndCharacter,
                Fallback = IntentFallback.CurrentTargetEnd, Layer = new() { Direction = RelativeLayerDirection.Up, Offset = 1, Minimum = 0, Maximum = 99 } };
            var first = new IntentPalette(Guid.NewGuid(), "通常", "表情", target, expressionRelation,
                [new IntentEntry(refs[0].Id), new IntentEntry(refs[1].Id)]) { ExpressionCandidates = true };
            var second = first with { Id = Guid.NewGuid(), Name = "別セット", Entries = [new IntentEntry(refs[1].Id)] };
            var reaction = first with { Id = Guid.NewGuid(), Name = "リアクション", Intent = "リアクション", Relation = new(), Entries = [new IntentEntry(refs[2].Id)], ExpressionCandidates = false };
            var fixture = PlacerSettingsStore.Copy(original); fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true;
            fixture.Library = [.. refs]; fixture.IntentPalettes = [first, second, reaction]; fixture.LegacyWorkspace = false;
            field.SetValue(vm, fixture); timeline.Items = [voice, next, text]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.Refresh(); view.PaletteTab.IsSelected = true; await Idle();
            var surface = view.RelativePaletteSurface;

            Assert(view.PaletteTab.Header?.ToString() == "配置" && view.ExpressionTab.Header?.ToString() == "表情をまとめて" && view.SelectionTab.Header?.ToString() == "設定",
                "UIUX normal top level is task language, not Palette / Selection Placement implementation taxonomy");
            Assert(vm.IntentContextTitle == "UX Character ボイス" && vm.IntentContextDetail.Contains("今日も", StringComparison.Ordinal) &&
                surface.IntentContextTitleText.IsVisible && surface.IntentContextDetailText.IsVisible,
                "UIUX selection itself exposes the editing context before any feature choice");
            Assert(vm.IntentSets.Select(x => x.Id).SequenceEqual(new[] { first.Id, second.Id, reaction.Id }) && surface.FindName("IntentTabStrip") == null,
                "UIUX/R2-B selected Voice shows applicable Sets directly and has no separate Intent control");
            Assert(vm.IntentSets.Count == 3 && vm.UseSegmentedIntentSets && !vm.UseIntentSetPicker && surface.IntentSetSegments.IsVisible && !surface.IntentSetPicker.IsVisible,
                "UIUX a small high-frequency set choice is always-visible segments instead of a ComboBox");
            var beforeSwitch = Signature(timeline); surface.IntentSetSegments.SelectedIndex = 1; await Idle();
            Assert(vm.SelectedIntentSet?.Targeted?.Id == second.Id && Signature(timeline) == beforeSwitch,
                "UIUX switching a set is one visible operation and is zero-write");
            surface.IntentSetSegments.SelectedIndex = 0; await Idle();
            var tileButton = RelativeVisuals(surface.IntentTileItems).OfType<Button>().First(x => x.CommandParameter is IntentTileChoice);
            Assert(tileButton.Command == vm.ExecuteIntentTileCommand && tileButton.IsEnabled &&
                RelativeVisuals(surface).OfType<ComboBox>().All(x => !x.IsVisible),
                "UIUX normal tile is the direct action; Profile/Preset/Layer/Relation pickers are absent from the common path");
            await InvokeSelectionButton(tileButton);
            var placed = timeline.Items.Except(new IItem[] { voice, next, text }).Single();
            Assert(placed.Frame == voice.Frame && placed.Layer < voice.Layer, "UIUX one tile click directly performs the saved relative placement");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == beforeSwitch, "UIUX the direct tile action remains one native Undo");

            view.Width = 360; view.Height = 360; await Idle(); surface.UpdateLayout();
            var narrowButtons = RelativeVisuals(surface.IntentTileItems).OfType<Button>().Where(x => x.CommandParameter is IntentTileChoice).ToArray();
            Assert(surface.IntentContextHeader.IsVisible && surface.IntentSetSegments.IsVisible && narrowButtons.Length == 2 &&
                narrowButtons.All(x => x.ActualWidth > 0 && x.TranslatePoint(new Point(x.ActualWidth, 0), surface).X <= surface.ActualWidth + 1) &&
                surface.PanelQuickSettingsButton.IsVisible && surface.PanelQuickSettingsButton.ActualHeight > 0 &&
                surface.PanelQuickSettingsButton.TranslatePoint(new Point(surface.PanelQuickSettingsButton.ActualWidth, surface.PanelQuickSettingsButton.ActualHeight), surface).Y <= surface.ActualHeight + 1,
                "UIUX 360px keeps context, directly segmented Sets, action tiles and quick-settings discovery understandable");
            SaveNamedView(view, "v042-uiux-edit-360.png");

            var many = PlacerSettingsStore.Copy(fixture);
            many.IntentPalettes = Enumerable.Range(1, 5).Select(i => first with { Id = Guid.NewGuid(), Name = $"セット{i}" }).Append(reaction).ToList();
            field.SetValue(vm, many); vm.Refresh(); view.PaletteTab.IsSelected = true; await Idle();
            Assert(vm.IntentSets.Count == 6 && !vm.UseSegmentedIntentSets && vm.UseIntentSetPicker && !surface.IntentSetSegments.IsVisible && surface.IntentSetPicker.IsVisible,
                "UIUX many sets deliberately collapse to a ComboBox instead of overflowing the high-frequency surface");

            field.SetValue(vm, fixture); timeline.SelectedItems = [text]; vm.Refresh(); view.PaletteTab.IsSelected = true; await Idle();
            Assert(vm.IntentSets.Count == 0 && vm.IntentTiles.Count == 0 && vm.ShowIntentEmptyAction && surface.IntentEmptyActionButton.IsVisible &&
                surface.IntentEmptyActionButton.Content?.ToString() == "新しく設定する" && vm.IntentNotice.Contains("まだありません", StringComparison.Ordinal),
                "UIUX an unsupported selected Item gives an actionable empty state rather than an unrelated Library");
            var emptySignature = Signature(timeline); await InvokeSelectionButton(surface.IntentEmptyActionButton);
            Assert(view.SelectionTab.IsSelected && ReferenceEquals(view.SelectionTab.Content, view.RelativeSettingsSurface) && Signature(timeline) == emptySignature,
                "UIUX empty-state recovery opens Settings without mutating Timeline");
            var emptyPanel = view.RelativeSettingsSurface; await InvokeSelectionButton(emptyPanel.NewPaletteButton);
            var createdForText = vm.IntentSettings!.SelectedPalette!;
            Assert(createdForText.TypeChoices.Single(x => x.Selected).Key == IntentSelectionContext.TypeKey(typeof(TextItem)) && createdForText.MinimumCount == "1" &&
                createdForText.MaximumCount == "1" && Signature(timeline) == emptySignature,
                "UIUX empty-state recovery continues into a new set for the actually selected Item instead of a hidden Voice default");
            vm.ResetIntentSettings();

            timeline.SelectedItems = [voice]; field.SetValue(vm, fixture); vm.Refresh(); vm.BeginIntentSettings(); view.SelectionTab.IsSelected = true; await Idle();
            var panel = view.RelativeSettingsSurface; var draft = vm.IntentSettings!.Palettes.Single(x => x.Id == first.Id); vm.IntentSettings!.SelectedPalette = draft; await Idle();
            Assert(panel.RelationSummaryText.Text.Contains("UX Character", StringComparison.Ordinal) && panel.RelationSummaryText.Text.Contains("次の同じ種類・同じキャラ", StringComparison.Ordinal) &&
                panel.RelationSummaryText.Text.Contains("上", StringComparison.Ordinal) && panel.RelationSummaryText.Text.Contains("塞がっていれば", StringComparison.Ordinal),
                "UIUX Settings explains the current relation in natural editing language");
            Assert(panel.NeighborPanel.IsVisible && panel.NeighborEdgePanel.IsVisible && panel.FallbackPanel.IsVisible && !panel.FixedDurationPanel.IsVisible &&
                !panel.BoundaryTolerancePanel.IsVisible && !panel.AlignmentPanel.IsVisible && !panel.RelationAdvanced.IsExpanded,
                "UIUX related-duration Settings initially show only the parameters needed for that decision");

            draft.Duration = IntentDuration.TargetSpan; await Idle();
            Assert(!panel.NeighborPanel.IsVisible && !panel.NeighborEdgePanel.IsVisible && !panel.FallbackPanel.IsVisible && !panel.FixedDurationPanel.IsVisible && panel.AlignmentPanel.IsVisible,
                "UIUX TargetSpan hides neighbor/fallback/fixed-duration parameters");
            draft.Duration = IntentDuration.Fixed; panel.RelationAdvanced.IsExpanded = true; await Idle();
            Assert(panel.FixedDurationPanel.IsVisible && !panel.NeighborPanel.IsVisible, "UIUX/R2-C fine tuning reveals fixed length only when it is actually needed");
            draft.Anchor = IntentAnchor.PairBoundary; await Idle();
            Assert(panel.BoundaryTolerancePanel.IsVisible, "UIUX/R2-C fine tuning reveals boundary tolerance only for pair-boundary mode");
            draft.Anchor = IntentAnchor.SelectedStart; draft.Duration = IntentDuration.UntilRelated; panel.RelationAdvanced.IsExpanded = false; await Idle();
            Assert(panel.NeighborPanel.IsVisible && panel.FallbackPanel.IsVisible && !panel.AlignmentPanel.IsVisible,
                "UIUX choosing a neighbor-based relation reveals its neighbor and fallback while hiding irrelevant alignment");

            var preservedCharacterDraft = draft.CharacterName; draft.CharacterRestricted = false; await Idle();
            Assert(!panel.CharacterNamePanel.IsVisible && draft.CharacterName == preservedCharacterDraft, "UIUX disabling Character restriction hides the detail without discarding unfinished draft input");
            draft.CharacterRestricted = true; await Idle();
            Assert(panel.CharacterNamePanel.IsVisible && panel.RelationSummaryText.Text.Contains(character.Name, StringComparison.Ordinal),
                "UIUX enabling Character restriction reveals the one required Character input and updates the summary");
            Assert(!panel.TargetAdvanced.IsExpanded && panel.FindName("TargetTypeChoices") == null && panel.FindName("TypeMatchPanel") == null,
                "H1 normal Item-first Settings never expose the old runtime type matrix or multi-type matching decision");
            panel.TargetAdvanced.IsExpanded = true; await Idle();
            Assert(panel.FindName("TargetTypeChoices") == null && panel.FindName("TypeMatchPanel") == null &&
                panel.OwnerItemTypeText.Text.Contains("ボイス", StringComparison.Ordinal),
                "UIUX expanded target details keep one readable Item owner and hide raw runtime type keys");

            var emptySession = new IntentSettingsSession(fixture, new[] { typeof(VoiceItem), typeof(TextItem) });
            RejectWithoutMutation(timeline, () => emptySession.Create([]), "UIUX creating a set with no Timeline context never silently defaults to Voice");
            RejectWithoutMutation(timeline, () => emptySession.Create([voice, text]), "UIUX mixed-runtime selection cannot create a new shared multi-type Set");
            view.Width = 360; view.Height = 360; panel.RelationAdvanced.IsExpanded = false; panel.TargetAdvanced.IsExpanded = false; await Idle();
            Assert(panel.RollbackButton.IsVisible && panel.RollbackButton.ActualHeight > 0 && panel.RelationSummaryText.IsVisible && panel.ActualWidth <= 360 &&
                panel.RollbackButton.TranslatePoint(new Point(panel.RollbackButton.ActualWidth, panel.RollbackButton.ActualHeight), panel).Y <= panel.ActualHeight + 1,
                "UIUX narrow Settings keep the result summary and rollback action available while advanced parameters stay collapsed");
            SaveNamedView(view, "v042-uiux-settings-360.png");

            var checks = new (string Requirement, string Evidence)[]
            {
                ("Item selection exposes only matching Sets for the current context", "Voice context native Set filtering; legacy Intent metadata retained"),
                ("Normal top-level wording is task-oriented instead of Palette / Selection Placement taxonomy", "native Placement / bulk expression / Settings headers"),
                ("Context leads directly to Set; small Sets are visible segments and large Sets have deliberate fallback (Round 2 supersedes Intent navigation)", "native 3-set and 6-set layouts"),
                ("One tile directly performs the saved placement without Profile, Preset, Layer, Relation or Template-management decisions", "native tile command plus normal-surface control audit"),
                ("Settings progressive disclosure follows duration, neighbor, fallback, boundary, Character and type-match choices", "native visibility transitions"),
                ("Settings expose a natural-language relation summary and keep rare parameters under collapsed Advanced sections", "native summary plus Advanced state"),
                ("Unsupported contexts provide an actionable recovery and never fall back to an unrelated Library", "native Text empty-state -> Text set creation"),
                ("360px keeps context, Set, tiles, Settings discovery and rollback understandable", "native narrow edit/settings captures"),
                ("Raw runtime type keys and internal IDs stay out of ordinary user-facing controls", "native Settings control metadata audit"),
                ("Navigation and Set switching are zero-write; tile placement retains shared planning and one native Undo", "native signatures and Undo proof plus retained regression ladder")
            };
            File.WriteAllText(Path.Combine(output, "v042-uiux-acceptance.json"), JsonSerializer.Serialize(new
            {
                schema = "YMM4-Template-Placer-Relative-UIUX/1", version = "0.4.2", result = "PASS", host = "YMM4 4.55.1.1 Lite",
                checks = checks.Select((x, i) => new { id = i + 1, requirement = x.Requirement, evidence = x.Evidence, result = "PASS" })
            }, new JsonSerializerOptions { WriteIndented = true }));
            Log("RELATIVE_UIUX=PASS");
        }
        finally
        {
            if (disk == null) File.Delete(PlacerSettingsStore.DefaultPath); else File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk);
            store.Load();
            foreach (var source in sources) ItemSettings.Default.Templates.Remove(source);
            field.SetValue(vm, original); timeline.Items = items; timeline.SelectedItems = selection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            view.Width = width; view.Height = height; vm.SetLegacyWorkspace(mode); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
}
