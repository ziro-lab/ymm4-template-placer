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

            // A close immediately after a finite action must synchronously settle the
            // queued edit before an owner-window click could continue into placement.
            new ButtonAutomationPeer(quick.QuickAlignEnd).Invoke();
            Assert(!vm.ExecuteIntentTileCommand.CanExecute(vm.IntentTiles.Single()),
                "PLACEMENT_QUICK_SETTINGS P2 pending quick edit blocks placement from using the older saved relation");
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
