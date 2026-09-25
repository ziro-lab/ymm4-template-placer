using System.Text.Json;
using System.Windows.Automation.Peers;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementQuickSettings(Timeline timeline, UndoRedoManager undo)
    {
        stage = "PLACEMENT_QUICK_SETTINGS P0/P1 shared Settings Draft and finite actions";
        var vm = ViewModel!;
        var view = View!;
        using var scope = new Round3Fixture(timeline, undo);

        var source = scope.AddTemplate("QuickPlacement/Source", new TextItem { Frame = 0, Length = 10, Layer = 2 });
        var character = new Character { Name = "Quick Placement" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20 };
        var target = new IntentTargetContext
        {
            ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
            CharacterName = character.Name
        };
        var relation = new IntentRelation
        {
            Anchor = IntentAnchor.SelectedStart,
            Alignment = IntentAlignment.StartAtAnchor,
            Duration = IntentDuration.TargetSpan,
            Fallback = IntentFallback.DoNotPlace,
            StartOffset = 3,
            EndOffset = -2,
            Layer = new RelativeLayerPolicy
            {
                Mode = LayerPlacementMode.RelativeToTarget,
                Direction = RelativeLayerDirection.Down,
                Offset = 2,
                Minimum = 0,
                Maximum = 40
            }
        };
        var set = new IntentPalette(Guid.NewGuid(), "Quick Set", "quick", target, relation, [new(source.Id)]);
        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.Library = [source];
        fixture.IntentPalettes = [set];
        fixture.Palettes = [];
        fixture.ExpressionBootstrapComplete = true;
        fixture.IntentPaletteRevision = 1;

        scope.Apply(fixture, [voice], [voice], 100);
        await Idle();

        var beforeTimeline = Signature(timeline);
        var beforeSettings = JsonSerializer.Serialize(scope.Current);
        var paletteSurface = view.RelativePaletteSurface;
        var quick = paletteSurface.PanelQuickSettingsSurface;

        try
        {
            paletteSurface.PanelQuickSettingsButton.IsChecked = true;
            await Idle();
            quick.SelectDefaultPage();
            await Idle();

            Assert(vm.HasPlacementQuickDraft &&
                ReferenceEquals(vm.PlacementQuickDraft, vm.IntentSettings?.SelectedPalette) &&
                vm.PlacementQuickDraft?.Id == set.Id,
                "PLACEMENT_QUICK_SETTINGS P0 quick surface reuses the exact existing IntentSettingsSession/IntentPaletteDraft");
            Assert(paletteSurface.PanelQuickSettingsPopup.IsOpen &&
                ReferenceEquals(quick.QuickSettingsTabs.SelectedItem, quick.PlacementQuickTab) &&
                quick.PlacementQuickTab.IsVisible &&
                quick.QuickBehaviorPreview.Diagram is { HasDiagram: true },
                "PLACEMENT_QUICK_SETTINGS P0 targeted Set opens the placement page with the shared read-only Preview");
            Assert(Signature(timeline) == beforeTimeline && JsonSerializer.Serialize(scope.Current) == beforeSettings,
                "PLACEMENT_QUICK_SETTINGS P0 opening the quick surface performs zero Timeline and Settings writes");

            new ButtonAutomationPeer(quick.QuickDurationTemplate).Invoke();
            new ButtonAutomationPeer(quick.QuickAlignCenter).Invoke();
            new ButtonAutomationPeer(quick.QuickLayerUpOne).Invoke();
            await Idle();

            var saved = scope.Current.IntentPalettes.Single(x => x.Id == set.Id).Relation;
            Assert(saved.Duration == IntentDuration.Template &&
                saved.Anchor == IntentAnchor.SelectedCenter &&
                saved.Alignment == IntentAlignment.CenterAtAnchor &&
                saved.Layer.Mode == LayerPlacementMode.RelativeToTarget &&
                saved.Layer.Direction == RelativeLayerDirection.Up &&
                saved.Layer.Offset == 1,
                "PLACEMENT_QUICK_SETTINGS P1 finite actions commit the documented existing placement fields");
            Assert(saved.Fallback == IntentFallback.DoNotPlace &&
                saved.StartOffset == 3 && saved.EndOffset == -2 &&
                saved.Layer.Minimum == 0 && saved.Layer.Maximum == 40,
                "PLACEMENT_QUICK_SETTINGS P1 finite actions preserve unrelated fallback, offsets and collision bounds");
            Assert(vm.PlacementQuickDraft?.BehaviorDescription.Duration == IntentDuration.Template &&
                vm.PlacementQuickDraft.Anchor == IntentAnchor.SelectedCenter &&
                vm.PlacementQuickDraft.Direction == RelativeLayerDirection.Up &&
                quick.QuickBehaviorPreview.Diagram is { HasDiagram: true } &&
                Signature(timeline) == beforeTimeline,
                "PLACEMENT_QUICK_SETTINGS P1 Preview and saved relation come from the same Draft with zero Timeline mutation");

            new ButtonAutomationPeer(quick.QuickLayerSpecified).Invoke();
            await Idle();
            var sourceLayerSaved = scope.Current.IntentPalettes.Single(x => x.Id == set.Id).Relation.Layer;
            Assert(sourceLayerSaved.Mode == LayerPlacementMode.Absolute && sourceLayerSaved.UseSourceLayer &&
                quick.QuickAbsoluteLayerBox.IsVisible && !quick.QuickLayerUpOne.IsVisible,
                "LAYER_UX P1 Quick Settings switches to specified-layer mode with blank=Source-layer semantics");

            quick.QuickAbsoluteLayerBox.Text = "15";
            quick.QuickAbsoluteLayerBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
            new ButtonAutomationPeer(quick.QuickLayerSearchDown).Invoke();
            await Idle();
            var explicitLayerSaved = scope.Current.IntentPalettes.Single(x => x.Id == set.Id).Relation.Layer;
            Assert(explicitLayerSaved.Mode == LayerPlacementMode.Absolute && !explicitLayerSaved.UseSourceLayer &&
                explicitLayerSaved.AbsoluteLayer == 15 && explicitLayerSaved.Direction == RelativeLayerDirection.Down,
                "LAYER_UX P1 Quick Settings saves explicit layer + one-direction collision escape through the shared Settings Draft");

            quick.QuickAbsoluteLayerBox.Text = "";
            quick.QuickAbsoluteLayerBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)!.UpdateSource();
            new ButtonAutomationPeer(quick.QuickLayerSearchUp).Invoke();
            await Idle();
            var blankLayerSaved = scope.Current.IntentPalettes.Single(x => x.Id == set.Id).Relation.Layer;
            Assert(blankLayerSaved.UseSourceLayer && blankLayerSaved.Direction == RelativeLayerDirection.Up,
                "LAYER_UX P1 clearing the quick specified-layer number restores Source-layer placement without a second mode");

            new ButtonAutomationPeer(quick.QuickLayerUpOne).Invoke();
            await Idle();

            var quickDraftBeforePresentationReset = vm.PlacementQuickDraft;
            Assert(vm.ShapeCurrentIntentSetCommand.CanExecute(IntentTileShape.Square),
                "PLACEMENT_QUICK_SETTINGS P1 existing presentation quick action is admitted after placement commit");
            vm.ShapeCurrentIntentSetCommand.Execute(IntentTileShape.Square);
            await Idle();
            Assert(vm.PlacementQuickDraft != null &&
                !ReferenceEquals(vm.PlacementQuickDraft, quickDraftBeforePresentationReset) &&
                ReferenceEquals(vm.PlacementQuickDraft, vm.IntentSettings?.SelectedPalette) &&
                vm.PlacementQuickDraft.Id == set.Id &&
                vm.PlacementQuickDraft.Duration == IntentDuration.Template &&
                vm.PlacementQuickDraft.Anchor == IntentAnchor.SelectedCenter &&
                vm.PlacementQuickDraft.Direction == RelativeLayerDirection.Up,
                "PLACEMENT_QUICK_SETTINGS P1 presentation-side Settings rebuild rebinds placement quick controls to the new authoritative Draft");

            var tile = vm.IntentTiles.Single();
            Assert(vm.ExecuteIntentTileCommand.CanExecute(tile),
                "PLACEMENT_QUICK_SETTINGS P1 successful protected commit admits the next placement");
            vm.ExecuteIntentTileCommand.Execute(tile);
            await Idle();
            var placed = timeline.Items.Except(new IItem[] { voice }).Single();
            Assert(placed.Frame == 118 && placed.Length == 5 && placed.Layer == 19,
                "PLACEMENT_QUICK_SETTINGS P1 next placement consumes the committed center/template/up-one relation including retained offsets");
            await undo.UndoAsync();
            await Idle();
            Assert(Signature(timeline) == beforeTimeline,
                "PLACEMENT_QUICK_SETTINGS P1 resulting placement remains one exact native Undo");

            paletteSurface.PanelQuickSettingsButton.IsChecked = false;
            await Idle();
            paletteSurface.PanelQuickSettingsButton.IsChecked = true;
            await Idle();
            quick.SelectDefaultPage();
            await Idle();

            // Enter the exact pre-dispatch pending state synchronously. A WPF
            // AutomationPeer Invoke is itself dispatcher-scheduled, which would race
            // this assertion with both the edit and its Background auto-commit.
            Assert(vm.ApplyPlacementQuickActionCommand.CanExecute(PlacementQuickAction.AlignEnd),
                "PLACEMENT_QUICK_SETTINGS P2 finite end-alignment action is admitted on the live quick Draft");
            vm.ApplyPlacementQuickActionCommand.Execute(PlacementQuickAction.AlignEnd);
            Assert(!vm.ExecuteIntentTileCommand.CanExecute(vm.IntentTiles.Single()),
                "PLACEMENT_QUICK_SETTINGS P2 pending quick edit blocks placement from using the older saved relation");

            // This is the same ordering used by the real owner-window outside-click
            // handler: settle the Settings session first, then let Popup close.
            vm.EndPanelQuickSettings();
            paletteSurface.PanelQuickSettingsButton.IsChecked = false;
            await Idle();

            var closedSaved = scope.Current.IntentPalettes.Single(x => x.Id == set.Id).Relation;
            Assert(closedSaved.Anchor == IntentAnchor.SelectedEnd &&
                closedSaved.Alignment == IntentAlignment.EndAtAnchor &&
                vm.ExecuteIntentTileCommand.CanExecute(vm.IntentTiles.Single()),
                "PLACEMENT_QUICK_SETTINGS P2 closing quick settings synchronously commits a valid finite edit before placement resumes");

            Log("PLACEMENT_QUICK_SETTINGS_P0=PASS");
            Log("PLACEMENT_QUICK_SETTINGS_P1=PASS");
            Log("PLACEMENT_QUICK_SETTINGS_P2=PASS");
        }
        finally
        {
            paletteSurface.PanelQuickSettingsButton.IsChecked = false;
            vm.EndPanelQuickSettings();
        }
    }
}
