using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyIntentSetWheel(Timeline timeline, UndoRedoManager undo)
    {
        stage = "native Set tab wheel browsing";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!; var panel = view.RelativePaletteSurface;
        var sources = Enumerable.Range(1, 24).Select(i => scope.AddTemplate("SetWheel/" + i, new TextItem { Length = 15, Layer = 2 })).ToArray();
        var voice = new VoiceItem(new Character { Name = "Set Wheel" }) { Frame = 100, Length = 30, Layer = 20 };
        var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = "Set Wheel" };
        var fixture = PlacerSettingsStore.Copy(scope.Original); fixture.Library = [.. sources];
        fixture.IntentPalettes = Enumerable.Range(1, 9).Select(i => new IntentPalette(Guid.NewGuid(), "ホイール確認" + i, "演出", target,
            new(), sources.Select(x => new IntentEntry(x.Id)).ToList())).ToList();
        fixture.Palettes = []; fixture.ManualCharacterPaletteId = null; fixture.ManualStylePaletteId = null;
        fixture.Presentation = new(); fixture.ExpressionBootstrapComplete = true;
        scope.Apply(fixture, [voice], [voice]); vm.SelectedIntentSet = vm.IntentSets[2]; await Idle();
        var scroll = panel.IntentSetHeaderScroll!;
        var selected = vm.SelectedIntentSet; var signature = Signature(timeline);
        var saved = JsonSerializer.Serialize(scope.Current);
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var window = Window.GetWindow(view)!; Assert(window.Activate(), "SET_WHEEL physical proof owner is active");
        await Task.Delay(80); await Idle();
        var tab = (TabItem)panel.IntentSetSegments.ItemContainerGenerator.ContainerFromIndex(2);
        var step = tab.ActualWidth;
        Assert(scroll.ScrollableWidth > step && panel.PaletteTileScroll.ScrollableHeight > 0,
            "SET_WHEEL fixture independently overflows headers horizontally and tiles vertically");

        async Task Hover(FrameworkElement owner, Point? local = null)
        {
            var point = owner.PointToScreen(local ?? new Point(owner.ActualWidth / 2, owner.ActualHeight / 2));
            Assert(Round2Input.SetCursorPos((int)Math.Round(point.X), (int)Math.Round(point.Y)), "SET_WHEEL OS cursor reaches intended surface");
            await Task.Delay(70); await Idle();
            var hit = Mouse.DirectlyOver as DependencyObject;
            Assert(hit != null && (ReferenceEquals(hit, owner) || owner.IsAncestorOf(hit) ||
                !owner.IsEnabled && (ReferenceEquals(hit, panel.IntentSetRow) || panel.IntentSetRow.IsAncestorOf(hit))),
                "SET_WHEEL physical hit belongs to the intended header/arrow/content surface");
        }
        async Task Wheel(int delta)
        {
            Round2Input.mouse_event(0x0800, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
            await Task.Delay(110); await Idle();
        }
        void SelectionUnchanged(string reason) => Assert(ReferenceEquals(vm.SelectedIntentSet, selected) &&
            ReferenceEquals(panel.IntentSetSegments.SelectedItem, selected) && view.PaletteTab.IsSelected,
            "SET_WHEEL " + reason + " retains selected Set identity and MainTabs");

        scroll.ScrollToLeftEnd(); panel.PaletteTileScroll.ScrollToVerticalOffset(40); await Idle();
        var contentOffset = panel.PaletteTileScroll.VerticalOffset;
        await Hover(tab); await Wheel(-120);
        Assert(Math.Abs(scroll.HorizontalOffset - step) <= 1 && panel.PaletteTileScroll.VerticalOffset == contentOffset,
            "SET_WHEEL actual wheel down over selected header browses right by one header without vertical spill");
        SelectionUnchanged("header wheel down");
        await Wheel(120);
        Assert(scroll.HorizontalOffset < 1 && panel.PaletteTileScroll.VerticalOffset == contentOffset,
            "SET_WHEEL actual wheel up browses left and returns exactly to the boundary");
        SelectionUnchanged("header wheel up");

        // The physical pointer remains on the strip for this routed fine-delta
        // case; ordinary +/-120 input above and arrow/end cases below are OS input.
        var fine = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -30) { RoutedEvent = Mouse.PreviewMouseWheelEvent };
        panel.IntentSetRow.RaiseEvent(fine); await Idle();
        Assert(fine.Handled && Math.Abs(scroll.HorizontalOffset - step / 4) <= 1,
            "SET_WHEEL fractional delta is preserved rather than discarded or promoted to a whole notch");
        SelectionUnchanged("fractional wheel");

        scroll.ScrollToLeftEnd(); await Idle();
        await Hover(panel.IntentSetScrollRightButton); await Wheel(-120);
        Assert(Math.Abs(scroll.HorizontalOffset - step) <= 1,
            "SET_WHEEL wheel over right arrow uses the same bounded browsing route");
        await Hover(panel.IntentSetScrollLeftButton); await Wheel(120);
        Assert(scroll.HorizontalOffset < 1,
            "SET_WHEEL wheel over left arrow uses the same bounded browsing route");
        await Hover(panel.IntentSetScrollLeftButton); await Wheel(120);
        Assert(scroll.HorizontalOffset < 1 && panel.PaletteTileScroll.VerticalOffset == contentOffset,
            "SET_WHEEL disabled left arrow at the boundary consumes wheel without content spill");
        SelectionUnchanged("arrow and left-boundary wheel");

        scroll.ScrollToRightEnd(); await Idle();
        await Hover(panel.IntentSetScrollRightButton); await Wheel(-120);
        Assert(Math.Abs(scroll.HorizontalOffset - scroll.ScrollableWidth) <= 1 && panel.PaletteTileScroll.VerticalOffset == contentOffset,
            "SET_WHEEL disabled right arrow saturates without content spill");
        await Wheel(120);
        Assert(scroll.HorizontalOffset < scroll.ScrollableWidth,
            "SET_WHEEL wheel up over right arrow can browse away from the saturated end");
        SelectionUnchanged("right-boundary wheel");

        panel.PaletteTileScroll.ScrollToTop(); await Idle(); var headerOffset = scroll.HorizontalOffset;
        await Hover(panel.PaletteTileScroll, new Point(60, Math.Min(50, panel.PaletteTileScroll.ActualHeight / 2)));
        await Wheel(-120);
        Assert(panel.PaletteTileScroll.VerticalOffset > 0 && scroll.HorizontalOffset == headerOffset,
            "SET_WHEEL actual wheel over tiles remains vertical and never browses headers");
        SelectionUnchanged("content wheel");

        view.Width = 640; await Idle();
        Assert(scroll.ScrollableWidth < 1, "SET_WHEEL nine headers fit the wide non-overflow strip");
        await Hover(tab); await Wheel(-120);
        Assert(scroll.HorizontalOffset < 1, "SET_WHEEL non-overflow strip remains stationary under wheel");
        SelectionUnchanged("non-overflow wheel");
        Assert(Signature(timeline) == signature && JsonSerializer.Serialize(scope.Current) == saved &&
            (disk == null ? !File.Exists(PlacerSettingsStore.DefaultPath) : File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(disk)),
            "SET_WHEEL all wheel routes leave Timeline, saved model and settings bytes unchanged");
        Log("INTENT_SET_WHEEL=PASS");
    }
}
