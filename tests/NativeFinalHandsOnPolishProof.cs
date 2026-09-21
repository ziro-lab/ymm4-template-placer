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

        // H2: reproduce the stale-event-source shape directly. The fallback says ComboBox,
        // while the current pointer coordinate is over ordinary Settings content.
        session = vm.IntentSettings!;
        session.SelectedItemContext = session.ItemContexts.Single(x => x.IsRealItemType && x.Key == voiceKey);
        await Idle();
        panel.SettingsScroll.ScrollToTop();
        panel.UpdateLayout();
        var root = panel.SettingsScroll;
        Assert(root.ScrollableHeight > 1, "FINAL H2 fixture has a genuinely scrollable Settings surface");

        var ordinaryPoint = panel.NewPaletteButton.TranslatePoint(new Point(
            Math.Min(2, Math.Max(0, panel.NewPaletteButton.ActualWidth - 1)),
            Math.Min(2, Math.Max(0, panel.NewPaletteButton.ActualHeight - 1))), root);
        var live = NestedWheelRouting.ResolveCurrentSource(root, ordinaryPoint, panel.PalettePicker);
        Assert(live != null && !IsDescendantOf(live, panel.PalettePicker),
            "FINAL H2: current hit-test overrides a stale ComboBox event source without mouse movement");

        var beforeOffset = root.VerticalOffset;
        var parentAccepted = NestedWheelRouting.TryScroll(root, live, -120, ModifierKeys.None);
        root.UpdateLayout();
        Assert(parentAccepted && root.VerticalOffset > beforeOffset,
            "FINAL H2: stale historical inner-control ownership cannot block current parent scrolling");

        root.ScrollToTop();
        root.UpdateLayout();
        var comboPoint = panel.PalettePicker.TranslatePoint(new Point(
            Math.Max(1, panel.PalettePicker.ActualWidth / 2),
            Math.Max(1, panel.PalettePicker.ActualHeight / 2)), root);
        var comboHit = NestedWheelRouting.ResolveCurrentSource(root, comboPoint, panel.NewPaletteButton);
        Assert(comboHit != null && IsDescendantOf(comboHit, panel.PalettePicker) &&
            !NestedWheelRouting.TryScroll(root, comboHit, -120, ModifierKeys.None),
            "FINAL H2: a ComboBox actually under the pointer still owns its wheel behavior");

        Log("FINAL_HANDS_ON_POLISH=PASS");
    }
}
