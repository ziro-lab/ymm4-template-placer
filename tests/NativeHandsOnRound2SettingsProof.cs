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
    private static async Task VerifyHandsOnRound2Settings(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R2-C Set-first staged Settings and finite sentence editor";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var items = timeline.Items; var selected = timeline.SelectedItems;
        var legacy = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var character = new Character { Name = "Round2 Settings" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20 };
        var source = Template("R2C/汎用", [new TextItem { Length = 17, Layer = 8 }]);
        var bundle = Template("R2C/複合", [new TextItem { Length = 15, Layer = 7 }, new TextItem { Length = 15, Layer = 8 }]);
        ItemSettings.Default.Templates.Add(source); ItemSettings.Default.Templates.Add(bundle);
        try
        {
            var reference = TemplateResolver.Reference(source, source.Name, null);
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))] };
            var first = new IntentPalette(Guid.NewGuid(), "強調", "  legacy/purpose  ", target, new(), [new(reference.Id)]);
            var second = first with { Id = Guid.NewGuid(), Name = "テロップ", Intent = "different/purpose" };
            var style = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "汎用SE", null, [reference.Id]);
            var fixture = PlacerSettingsStore.Copy(original); fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true;
            fixture.Library = [reference]; fixture.IntentPalettes = [first, second]; fixture.Palettes = [style];
            fixture.ManualStylePaletteId = style.Id; fixture.ManualCharacterPaletteId = null; fixture.LegacyWorkspace = false;
            field.SetValue(vm, fixture); timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(false); vm.ActivateIntentWorkspace(); vm.ResetIntentSettings(); vm.OpenIntentSettingsCommand.Execute(null); await Idle();
            var panel = view.RelativeSettingsSurface; var session = vm.IntentSettings!; var signature = Signature(timeline);
            var before = JsonSerializer.Serialize(fixture);
            Assert(panel.SettingsTargetButtons.IsVisible && session.CommonItemContexts.Any(x => x.IsGeneric) &&
                new[] { "ボイス", "テキスト", "画像", "図形" }.All(name => session.CommonItemContexts.Any(x => x.Label == name)),
                "R2-C C1/C8 direct target buttons include Generic and common Japanese Item names without requiring an existing source of each type");
            Assert(panel.FindName("SettingsIntentPicker") == null && panel.FindName("IntentNameBox") == null &&
                session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(new[] { first.Id, second.Id }),
                "R2-C C2/C3 targeted Settings shows all matching Set names across preserved Intent categories");
            Assert(!session.HasChanges && !panel.TargetAdvanced.IsExpanded && panel.FindName("TargetTypeChoices") == null && Signature(timeline) == signature,
                "R2-C C6/C9 opening and target navigation are clean and Timeline zero-write; normal Settings has one Item owner and no multi-type matrix");
            var draft = session.SelectedPalette!;
            Assert(draft.SentenceAnchors.Single(x => x.Value == IntentAnchor.PairBoundary).Available == false &&
                draft.SentenceNeighbors.All(x => x.Value != IntentNeighbor.None),
                "R2-C D2/D4 invalid single-target boundary and required-neighbor None choices are not selectable");
            var anchorPresentation = draft.SentenceAnchors.ToDictionary(x => x.Value);
            Assert(anchorPresentation[IntentAnchor.SelectionRangeStart].Name == "対象範囲の開始" &&
                anchorPresentation[IntentAnchor.SelectionRangeEnd].Name == "対象範囲の終了" &&
                anchorPresentation[IntentAnchor.PairBoundary].Name == "2件の間の区切り線" &&
                anchorPresentation[IntentAnchor.SelectionRangeStart].StartsGroup &&
                anchorPresentation[IntentAnchor.PairBoundary].StartsGroup &&
                anchorPresentation[IntentAnchor.RelatedStart].StartsGroup,
                "BEHAVIOR_PREVIEW UX anchor display language uses target-range/separator wording with bounded visual groups");
            panel.AnchorBox.IsDropDownOpen = true; await Idle();
            var rangeItem = panel.AnchorBox.Items.Cast<IntentSentenceOption<IntentAnchor>>().Single(x => x.Value == IntentAnchor.SelectionRangeStart);
            var rangeContainer = (ComboBoxItem?)panel.AnchorBox.ItemContainerGenerator.ContainerFromItem(rangeItem);
            Assert(rangeContainer != null && rangeContainer.BorderThickness.Top == 1 && rangeContainer.Margin.Top >= 5,
                "BEHAVIOR_PREVIEW UX AnchorBox renders a visible category divider before target-range anchors");
            panel.AnchorBox.IsDropDownOpen = false; await Idle();

            // Hands-on regression: changing placement position from the real ComboBoxes
            // must not re-enter WPF binding/Preview rendering or terminate the host.
            var previewSignature = Signature(timeline);
            var behaviorNotifications = 0;
            draft.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IntentPaletteDraft.BehaviorDescription)) behaviorNotifications++; };
            var beforeSameValue = behaviorNotifications;
            draft.Anchor = draft.Anchor;
            draft.Direction = draft.Direction;
            Assert(behaviorNotifications == beforeSameValue,
                "BEHAVIOR_PREVIEW crash guard same-value ComboBox feedback is idempotent");

            foreach (var anchor in new[] { IntentAnchor.SelectedStart, IntentAnchor.SelectedCenter, IntentAnchor.SelectedEnd })
            {
                panel.AnchorBox.IsDropDownOpen = true; await Idle();
                panel.AnchorBox.SelectedValue = anchor;
                panel.AnchorBox.IsDropDownOpen = false; await Idle();
                Assert(draft.Anchor == anchor && panel.BehaviorPreview.Diagram is { HasDiagram: true } &&
                    Signature(timeline) == previewSignature,
                    $"BEHAVIOR_PREVIEW crash guard live AnchorBox change survives: {anchor}");
            }
            foreach (var alignment in Enum.GetValues<IntentAlignment>())
            {
                panel.AlignmentBox.IsDropDownOpen = true; await Idle();
                panel.AlignmentBox.SelectedValue = alignment;
                panel.AlignmentBox.IsDropDownOpen = false; await Idle();
                Assert(draft.Alignment == alignment && panel.BehaviorPreview.Diagram is { HasDiagram: true } &&
                    Signature(timeline) == previewSignature,
                    $"BEHAVIOR_PREVIEW crash guard live AlignmentBox change survives: {alignment}");
            }
            foreach (var direction in Enum.GetValues<RelativeLayerDirection>())
            {
                panel.DirectionBox.IsDropDownOpen = true; await Idle();
                panel.DirectionBox.SelectedValue = direction;
                panel.DirectionBox.IsDropDownOpen = false; await Idle();
                Assert(draft.Direction == direction && panel.BehaviorPreview.Diagram is { HasDiagram: true } &&
                    Signature(timeline) == previewSignature,
                    $"BEHAVIOR_PREVIEW crash guard live DirectionBox change survives: {direction}");
            }
            panel.AnchorBox.SelectedValue = IntentAnchor.SelectedStart;
            panel.AlignmentBox.SelectedValue = IntentAlignment.StartAtAnchor;
            panel.DirectionBox.SelectedValue = RelativeLayerDirection.Up;
            await Idle();

            var oldRelation = first.Relation;
            panel.DurationBox.SelectedValue = IntentDuration.UntilRelated; await Idle();
            Assert(draft.Duration == IntentDuration.UntilRelated && draft.Neighbor != IntentNeighbor.None && draft.Alignment == IntentAlignment.StartAtAnchor &&
                panel.RelationSummaryText.Text.Contains("まで", StringComparison.Ordinal) && Signature(timeline) == signature,
                "R2-C D1/D3 sentence selector updates the existing finite fields and live summary, without Timeline mutation");
            Assert(panel.BehaviorPreview.IsVisible &&
                panel.BehaviorPreview.Diagram is { HasDiagram: true } targetedPreview &&
                targetedPreview.Blocks.Any(x => x.Kind == PreviewBlockKind.Placed) &&
                Signature(timeline) == signature,
                "BEHAVIOR_PREVIEW v2 compact Settings updates diagram from the live Draft without Timeline writes");
            double Top(FrameworkElement element) => element.TranslatePoint(new Point(0, 0), panel).Y;
            Assert(Top(panel.SetNameBox) < Top(panel.TargetEditor) &&
                Top(panel.TargetEditor) < Top(panel.BehaviorPreviewCard) &&
                Top(panel.BehaviorPreviewCard) < Top(panel.RelationEditor),
                "BEHAVIOR_PREVIEW UX targeted Settings reads Set name -> target -> Preview -> placement controls");
            draft.FixedDuration = "not an integer"; draft.Duration = IntentDuration.TargetSpan;
            var hidden = session.Build().IntentPalettes.Single(x => x.Id == first.Id);
            Assert(hidden.Relation == (oldRelation with { Neighbor = draft.Neighbor }) && hidden.Intent == first.Intent && draft.FixedDuration == "not an integer",
                "R2-C C4/D6/D10 unused invalid numeric text does not change saved values; compatibility Intent is retained exactly");
            Assert(!panel.RelationAdvanced.IsExpanded && panel.RelationAdvanced.Header?.ToString() == "細かく調整" &&
                !panel.FixedDurationPanel.IsVisible && !panel.BoundaryTolerancePanel.IsVisible,
                "R2-C D5 numeric length/boundary/offset/gap/layer controls live under collapsed fine tuning");
            await InvokeSelectionButton(panel.RollbackButton); session = vm.IntentSettings!;
            var genericButton = RelativeVisuals(panel.SettingsTargetButtons).OfType<Button>().Single(x => x.CommandParameter is IntentSettingsItemContext { IsGeneric: true });
            await InvokeSelectionButton(genericButton);
            Assert(session.IsGenericContext && session.SelectedPalette == null && session.SelectedGenericSet?.Id == style.Id &&
                panel.GenericPalettePicker.IsVisible && !panel.PalettePicker.IsVisible && !session.HasChanges,
                "R2-C C1/C9 native Generic target selects staged Style Sets on the same Settings surface without a write");
            Assert(panel.GenericSettingsSurface.FindName("GenericBehaviorPreview") == null && Signature(timeline) == signature,
                "BEHAVIOR_PREVIEW v2 Generic Settings does not force a redundant diagram");
            var generic = session.SelectedGenericSet!; generic.UseTemplateLayer = false; generic.Minimum = "4"; generic.Maximum = "12"; generic.Preferred = "8";
            var built = session.Build(); var genericModel = built.Palettes.Single(x => x.Id == style.Id);
            Assert(genericModel.Kind == PaletteKind.Style && genericModel.Layer == new LayerPolicy { UseTemplateLayer = false, Minimum = 4, Maximum = 12, Preferred = 8 } &&
                genericModel.LibraryEntryIds.SequenceEqual(style.LibraryEntryIds) && panel.GenericSettingsSurface.GenericSentenceText.Text.Contains("テンプレートの長さ", StringComparison.Ordinal),
                "R2-C D7 Generic edits only existing QuickDrop CurrentFrame/intrinsic-duration/layer-policy semantics");
            generic.Minimum = "bad"; generic.UseTemplateLayer = true;
            Assert(session.Build().Palettes.Single(x => x.Id == style.Id).Layer == style.Layer && generic.Minimum == "bad",
                "R2-C D10 Generic unused range text is retained as a draft but never coerced or saved over its valid seed");
            await InvokeSelectionButton(panel.NewPaletteButton);
            var created = session.SelectedGenericSet!;
            Assert(created.Id != style.Id && created.Entries.Count == 0 && session.GenericSets.Count == 2,
                "R2-C C5 new Generic Set uses Style Palette identity and requires no Intent name");
            session.Sources.Single(x => ReferenceEquals(x.Source, source)).Selected = true;
            session.Sources.Single(x => ReferenceEquals(x.Source, bundle)).Selected = true;
            var batchBefore = JsonSerializer.Serialize(session.Build());
            RejectWithoutMutation(timeline, () => session.AddSelectedSources(), "R2-C unsupported Generic bundle rejects the complete mixed source batch");
            Assert(JsonSerializer.Serialize(session.Build()) == batchBefore && session.Sources.Count(x => x.Selected) == 2,
                "R2-C Generic source failure preserves both selected inputs and all draft membership/library bytes");
            session.Sources.Single(x => ReferenceEquals(x.Source, bundle)).Selected = false;
            Assert(panel.SourceEditor.IsVisible,
                "R2-C bulk source registration remains directly visible after first-level disclosure removal");
            await Idle();
            await InvokeSelectionButton(panel.AddSourcesButton);
            Assert(created.Entries.Single().LibraryEntryId == reference.Id && session.Build().Library.Count == 1,
                "R2-C Generic staged bulk add reuses the exact existing Library reference without another source store");
            session.Duplicate(); var copy = session.SelectedGenericSet!;
            Assert(copy.Id != created.Id && copy.Entries.Single().LibraryEntryId == reference.Id && !ReferenceEquals(copy.Entries[0], created.Entries[0]),
                "R2-C Generic duplication keeps independent staged entries and shared exact source identity");
            session.RemoveSelected(); session.SelectedGenericSet = created; created.Name = "保存した汎用セット";
            Assert(JsonSerializer.Serialize(fixture) == before && Signature(timeline) == signature,
                "R2-C C9 targeted/Generic edits preserve the immutable opening snapshot and Timeline while pending edits remain in the draft");
            await Idle(); // Valid edits persist through the real root auto-commit, without a Save click.
            var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            Assert(!vm.HasError && saved.Palettes.Any(x => x.Id == created.Id && x.Name == "保存した汎用セット") &&
                saved.IntentPalettes.Single(x => x.Id == first.Id).Intent == first.Intent && !vm.IntentSettings!.HasChanges,
                "R2-C C4/C10 automatic persistence atomically commits Generic and targeted Settings while preserving compatibility Intent");
            var persistedBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
            var reloaded = new IntentSettingsSession(saved, new[] { typeof(VoiceItem) }, [voice]);
            Assert(JsonSerializer.Serialize(reloaded.Build()) == JsonSerializer.Serialize(saved) &&
                File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(persistedBytes),
                "R2-C D9 saved/reloaded sentence state is reconstructed from finite models and does not rewrite on load");
            view.Width = 360; view.Height = 440; await Idle();
            Assert(panel.RollbackButton.IsVisible && panel.RollbackButton.TranslatePoint(new Point(0, panel.RollbackButton.ActualHeight), panel).Y <= panel.ActualHeight + 1,
                "R2-C narrow Set-first Settings keeps the protected rollback action reachable");
            SaveNamedView(view, "v042-round2-settings-360.png");
            File.WriteAllText(Path.Combine(output, "hands-on-round2-settings.json"), JsonSerializer.Serialize(new {
                schema = "YMM4-Template-Placer-Round2-Settings/1", result = "PASS", host = "YMM4 4.55.1.1 Lite", source = "finite-model", generic = "Style Palette / QuickDrop" }));
            Log("HANDS_ON_ROUND2_C=PASS");
        }
        finally
        {
            if (disk == null) File.Delete(PlacerSettingsStore.DefaultPath); else File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk);
            store.Load(); ItemSettings.Default.Templates.Remove(source); ItemSettings.Default.Templates.Remove(bundle);
            field.SetValue(vm, original); timeline.Items = items; timeline.SelectedItems = selected; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(legacy); vm.Refresh(); vm.ResetIntentSettings(); view.Width = width; view.Height = height;
        }
    }
}
