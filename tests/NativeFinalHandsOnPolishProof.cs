using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static bool IsDescendantOf(DependencyObject? child, DependencyObject ancestor)
    {
        for (var current = child; current != null; current = TimelinePointerIntentClassifier.Parent(current))
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    private static async Task VerifyFinalHandsOnPolish(Timeline timeline, UndoRedoManager undo)
    {
        stage = "final Hands-on polish";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var view = View!;
        var panel = view.RelativeSettingsSurface;
        var character = new Character { Name = "Final Polish" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 30, Layer = 20 };
        var source = scope.AddTemplate("FinalPolish/source", new TextItem { Length = 12, Layer = 4 });
        var voiceKey = IntentSelectionContext.TypeKey(typeof(VoiceItem));
        var targeted = new IntentPalette(Guid.NewGuid(), "削除対象", "演出",
            new IntentTargetContext { ItemTypeKeys = [voiceKey] }, new IntentRelation(), [new IntentEntry(source.Id)]);
        var generic = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "汎用削除対象", null, [source.Id]);

        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.Library = [source];
        fixture.IntentPalettes = [targeted];
        fixture.Palettes = [generic];
        fixture.ManualStylePaletteId = generic.Id;
        fixture.ManualCharacterPaletteId = null;
        fixture.ExpressionBootstrapComplete = true;
        scope.Apply(fixture, [voice], [voice]);
        view.SelectionTab.IsSelected = true;
        await Idle();

        var session = vm.IntentSettings ?? throw new InvalidOperationException("Final polish Settings session missing.");
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsRealItemType && x.Key == voiceKey);
        await Idle();
        Assert(panel.DirectDeletePaletteButton.IsVisible &&
            ReferenceEquals(panel.DirectDeletePaletteButton.Command, vm.DeleteIntentPaletteCommand) &&
            panel.DirectDeletePaletteButton.IsEnabled,
            "FINAL H1: selected Item-owned Set exposes direct delete beside the Set picker");

        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsGeneric);
        await Idle();
        Assert(panel.DirectDeletePaletteButton.IsVisible &&
            ReferenceEquals(panel.DirectDeletePaletteButton.Command, vm.DeleteIntentPaletteCommand) &&
            panel.DirectDeletePaletteButton.IsEnabled,
            "FINAL H1: Generic Set uses the same directly visible protected delete command");

        // Exercise deletion semantics without driving the confirmation dialog: the UI button
        // above is the authoritative command, while the detached session proves the mutation
        // remains settings-only and never touches source templates or Timeline.
        var detached = new IntentSettingsSession(fixture, new[] { typeof(VoiceItem) });
        var beforeTimeline = Signature(timeline);
        var sourceTemplate = ItemSettings.Default.Templates.Single(x => source.Source.Matches(x));
        detached.SelectedItemContext = detached.ItemContexts.Single(x => x.IsRealItemType && x.Key == voiceKey);
        detached.RemoveSelected();
        detached.SelectedItemContext = detached.ItemContexts.Single(x => x.IsGeneric);
        detached.RemoveSelected();
        var removed = detached.Build();
        Assert(removed.IntentPalettes.All(x => x.Id != targeted.Id) &&
            removed.Palettes.All(x => x.Id != generic.Id) &&
            ItemSettings.Default.Templates.Contains(sourceTemplate) &&
            Signature(timeline) == beforeTimeline,
            "FINAL H1: Set deletion removes only Set configuration; source template and Timeline remain untouched");

        // H2: prove current-layout ownership first, then cross the boundary using
        // the physical OS cursor without waiting for WPF MouseMove/DirectlyOver refresh.
        session = vm.IntentSettings!;
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsRealItemType && x.Key == voiceKey);
        await Idle();
        panel.SettingsScroll.ScrollToTop();
        panel.UpdateLayout();
        var root = panel.SettingsScroll;
        Assert(root.ScrollableHeight > 1, "FINAL H2 fixture has a genuinely scrollable Settings surface");

        panel.RelationSummaryText.BringIntoView();
        await Idle();
        root.UpdateLayout();
        var ordinaryPoint = panel.RelationSummaryText.TranslatePoint(new Point(
            Math.Min(2, Math.Max(0, panel.RelationSummaryText.ActualWidth - 1)),
            Math.Min(2, Math.Max(0, panel.RelationSummaryText.ActualHeight - 1))), root);
        Assert(ordinaryPoint.X >= 0 && ordinaryPoint.Y >= 0 && ordinaryPoint.X <= root.ActualWidth && ordinaryPoint.Y <= root.ActualHeight,
            "FINAL H2 fixture brings ordinary Settings content into the current viewport");
        var live = NestedWheelRouting.ResolveCurrentSource(root, ordinaryPoint);
        Assert(live != null && !IsDescendantOf(live, panel.AnchorBox),
            "FINAL H2: live hit-test resolves ordinary Settings content independently of a historical event source");

        var beforeOffset = root.VerticalOffset;
        var parentAccepted = NestedWheelRouting.TryScroll(root, live, -120, ModifierKeys.None);
        root.UpdateLayout();
        Assert(parentAccepted && root.VerticalOffset > beforeOffset,
            "FINAL H2: ordinary current content scrolls the parent Settings viewer");

        panel.AnchorBox.BringIntoView();
        await Idle();
        root.UpdateLayout();
        var comboPoint = panel.AnchorBox.TranslatePoint(new Point(
            Math.Max(1, panel.AnchorBox.ActualWidth / 2),
            Math.Max(1, panel.AnchorBox.ActualHeight / 2)), root);
        Assert(comboPoint.X >= 0 && comboPoint.Y >= 0 && comboPoint.X <= root.ActualWidth && comboPoint.Y <= root.ActualHeight,
            "FINAL H2 fixture brings the inner ComboBox into the current Settings viewport");
        var comboHit = NestedWheelRouting.ResolveCurrentSource(root, comboPoint);
        Assert(comboHit != null && IsDescendantOf(comboHit, panel.AnchorBox) &&
            !NestedWheelRouting.TryScroll(root, comboHit, -120, ModifierKeys.None),
            "FINAL H2: a ComboBox actually under the pointer still owns its wheel behavior");

        // Find a visible ordinary point in the same current viewport.
        Point? boundaryTarget = null;
        DependencyObject? boundaryHit = null;
        for (var y = 8d; y < root.ActualHeight - 8 && boundaryTarget == null; y += 12)
            for (var x = 8d; x < root.ActualWidth - 8; x += 16)
            {
                var point = new Point(x, y);
                var hit = NestedWheelRouting.ResolveCurrentSource(root, point);
                if (hit == null || IsDescendantOf(hit, panel.AnchorBox)) continue;
                if (hit is ComboBox or System.Windows.Controls.Primitives.RangeBase) continue;
                var interactiveAncestor = false;
                for (var current = hit; current != null && !ReferenceEquals(current, root); current = TimelinePointerIntentClassifier.Parent(current))
                    if (current is ComboBox or System.Windows.Controls.Primitives.RangeBase) { interactiveAncestor = true; break; }
                if (!interactiveAncestor) { boundaryTarget = point; boundaryHit = hit; break; }
            }
        Assert(boundaryTarget.HasValue && boundaryHit != null,
            "FINAL H2 fixture finds ordinary parent-scroll content beside the inner controls");

        var comboScreen = root.PointToScreen(comboPoint);
        Assert(Round2Input.SetCursorPos((int)Math.Round(comboScreen.X), (int)Math.Round(comboScreen.Y)),
            "FINAL H2 OS cursor moved onto inner ComboBox");
        var physicalCombo = NestedWheelRouting.ResolveCurrentSource(root);
        Assert(physicalCombo != null && IsDescendantOf(physicalCombo, panel.AnchorBox),
            "FINAL H2 physical resolver sees the inner control while the wheel starts there");

        var targetScreen = root.PointToScreen(boundaryTarget!.Value);
        Assert(Round2Input.SetCursorPos((int)Math.Round(targetScreen.X), (int)Math.Round(targetScreen.Y)),
            "FINAL H2 OS cursor crossed from inner control to outer Settings content");
        // Intentionally no delay/Idle here: this models moving out while wheel events
        // continue before WPF mouse-over state has had a chance to settle.
        var crossed = NestedWheelRouting.ResolveCurrentSource(root);
        beforeOffset = root.VerticalOffset;
        var crossedAccepted = crossed != null && NestedWheelRouting.TryScroll(root, crossed, -120, ModifierKeys.None);
        root.UpdateLayout();
        Assert(crossed != null && !IsDescendantOf(crossed, panel.AnchorBox) && crossedAccepted && root.VerticalOffset > beforeOffset,
            "FINAL H2: boundary crossing immediately hands the next wheel to parent without waiting for MouseMove");

        var outsideScreen = root.PointToScreen(new Point(-8, -8));
        Assert(Round2Input.SetCursorPos((int)Math.Round(outsideScreen.X), (int)Math.Round(outsideScreen.Y)) &&
            NestedWheelRouting.ResolveCurrentSource(root) == null,
            "FINAL H2: pointer outside the root yields no stale-source fallback and leaves the event unhandled");

        Log("FINAL_HANDS_ON_POLISH=PASS");
    }
}
