using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task<bool> Round4DragScrollThumb(ScrollViewer viewer)
    {
        viewer.ScrollToVerticalOffset(viewer.ScrollableHeight / 2); viewer.UpdateLayout(); await Idle();
        var bar = viewer.Template.FindName("PART_VerticalScrollBar", viewer) as ScrollBar
            ?? throw new InvalidOperationException("R4-A vertical scrollbar fixture missing");
        bar.ApplyTemplate(); bar.UpdateLayout();
        var thumb = Descendant<Thumb>(bar) ?? throw new InvalidOperationException("R4-A scrollbar thumb fixture missing");
        thumb.BringIntoView(); await Idle();
        var point = thumb.PointToScreen(new Point(thumb.ActualWidth / 2, thumb.ActualHeight / 2));
        Round2Input.SetCursorPos((int)Math.Round(point.X), (int)Math.Round(point.Y)); await Task.Delay(80); await Idle();
        var hit = Mouse.DirectlyOver as DependencyObject;
        Log($"R4-A scrollbar hit: point={point} viewer={viewer.ActualWidth}x{viewer.ActualHeight} thumb={thumb.ActualWidth}x{thumb.ActualHeight} hit={hit?.GetType().FullName}");
        Assert(hit != null && (ReferenceEquals(hit, thumb) || thumb.IsAncestorOf(hit)), "R4-A actual pointer reaches the scrollbar thumb before dragging");
        var before = viewer.VerticalOffset;
        Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); await Task.Delay(50);
        try
        {
            Round2Input.SetCursorPos((int)Math.Round(point.X), (int)Math.Round(point.Y + 20));
            await Task.Delay(100);
        }
        finally { Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); }
        await Idle();
        return Math.Abs(viewer.VerticalOffset - before) > 0.01;
    }
    private static async Task VerifyHandsOnRound4Navigation(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R4-A dropdown activation, common content seek and nested Settings wheel";
        var vm = ViewModel!; var view = View!;
        await vm.NavigationCompletion;
        using var scope = new Round3Fixture(timeline, undo);
        var character = new Character { Name = "Round4 Navigation" };
        var first = new VoiceItem(character) { Frame = 180, Length = 40, Layer = 20 };
        var last = new VoiceItem(character) { Frame = 450, Length = 40, Layer = 20 };
        var manual = new TextItem { Frame = 600, Length = 30, Layer = 70, Remark = "R4-A untouched manual" };
        var a = scope.AddTemplate("R4A/expression-a", new TachieFaceItem(character) { Length = 10, Layer = 4, Remark = "A" });
        var b = scope.AddTemplate("R4A/expression-b", new TachieFaceItem(character) { Length = 15, Layer = 4, Remark = "B" });
        for (var i = 0; i < 40; i++) scope.AddTemplate("R4A/scroll/" + i, new TextItem { Length = 10 });
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [a, b]; settings.Palettes = []; settings.ManualCharacterPaletteId = null; settings.ManualStylePaletteId = null;
        settings.IntentPalettes = [new(Guid.NewGuid(), "R4A expressions", "expression", new()
        { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name }, new(), [new(a.Id), new(b.Id)])
        { ExpressionCandidates = true }];
        settings.ExpressionBootstrapComplete = true; settings.Presentation = new();
        scope.Apply(settings, [first, last, manual], [manual], 0);
        view.ExpressionTab.IsSelected = true; await Idle();
        var host = ExpressionNavigationHost.Resolve();
        Assert(host.CanSeek, "R4-A resolves the actual pinned Preview surface before the interaction test");
        var oldFactory = vm.NavigationHostFactory;
        var seeks = new List<int>();
        vm.NavigationHostFactory = () => host with { SeekAsync = async frame => { seeks.Add(frame); await host.SeekAsync!(frame); } };
        ComboBox? box = null;
        async Task<ComboBox> For(VoiceItem voice)
        {
            var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            view.VoiceGrid.ScrollIntoView(row); view.VoiceGrid.UpdateLayout(); await Idle();
            var cell = view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow
                ?? throw new InvalidOperationException("R4-A Voice row not realized");
            return Descendant<ComboBox>(cell) ?? throw new InvalidOperationException("R4-A expression selector not realized");
        }
        try
        {
            var before = Signature(timeline);
            box = await For(first);
            var screen = box.PointToScreen(new Point(Math.Max(1, box.ActualWidth - 10), box.ActualHeight / 2));
            Round2Input.SetCursorPos((int)Math.Round(screen.X), (int)Math.Round(screen.Y)); await Task.Delay(80); await Idle();
            var hit = Mouse.DirectlyOver as DependencyObject;
            Assert(hit != null && (ReferenceEquals(hit, box) || box.IsAncestorOf(hit)), "R4-A verifies the live ComboBox hit before native mouse input");
            await NativeRound2Click(screen); await vm.NavigationCompletion; await Idle();
            Round4Assert(timeline.CurrentFrame == first.Frame && ReferenceEquals(timeline.SelectedItem, first) && seeks.SequenceEqual(new[] { first.Frame }),
                "A1", "native dropdown-open activates the Voice using the same root navigation/real Preview adapter");
            Round4Assert(seeks.Count == 1, "A2", "one dropdown-open does not also deliver a duplicate row-click navigation");
            Round4Assert(box.IsDropDownOpen && box.IsEnabled, "A3", "the expression ComboBox remains open and enabled after real Preview seek");
            box.IsDropDownOpen = false; await Idle();
            Round4Assert(Signature(timeline) == before && ReferenceEquals(timeline.SelectedItem, first),
                "A4", "cancelling an opened chooser leaves the Voice selected and changes no expression/item content");

            box = await For(last); Window.GetWindow(view)!.Activate(); box.Focus(); Keyboard.Focus(box); await Idle();
            await Round3PressKey(Key.F4); await vm.NavigationCompletion; await Idle();
            Round4Assert(box.IsDropDownOpen && timeline.CurrentFrame == last.Frame && ReferenceEquals(timeline.SelectedItem, last) &&
                seeks.SequenceEqual(new[] { first.Frame, last.Frame }), "A5", "native F4 opening uses the same activation semantics as the mouse");
            box.IsDropDownOpen = false; await Idle();
            var rowLast = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, last));
            var calls = seeks.Count;
            rowLast.SelectedChoice = rowLast.Choices.Single(x => x.Template?.IntentSource?.Entry.LibraryEntryId == a.Id);
            await vm.NavigationCompletion; await Idle();
            var firstApplyStatus = vm.Status;
            var beforeReopen = seeks.Count;
            box.IsDropDownOpen = true; await vm.NavigationCompletion; await Idle();
            Assert(box.IsDropDownOpen && seeks.Count == beforeReopen && vm.Status == firstApplyStatus,
                "R4-A re-opening the active Voice chooser is idempotent and does not end the native trial");
            box.IsDropDownOpen = false;
            rowLast.SelectedChoice = rowLast.Choices.Single(x => x.Template?.IntentSource?.Entry.LibraryEntryId == b.Id);
            await vm.NavigationCompletion; await Idle();
            Round4Assert(!vm.HasError && seeks.Count == calls + 2 && seeks.TakeLast(2).All(x => x == last.Frame) &&
                vm.Status.Contains("即時反映", StringComparison.Ordinal) && firstApplyStatus.Contains("即時反映", StringComparison.Ordinal),
                "A6", "successful same-row content changes requeue the common seek and retain the placement result status");
            Round4Assert(vm.NavigationHostFactory != null && vm.NavigationCompletion.IsCompleted && timeline.CurrentFrame == last.Frame,
                "A7", "dropdown activation and content refresh both use the installed root adapter, not a View-side seek path");
            vm.CloseExpressionTrialSession(); await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == before, "R4-A two immediate choices plus content seeks remain one native trial Undo");
            await undo.RedoAsync(); await Idle();
            Assert(ManagedIntentExpressionReader.Read(timeline, last).Bundle?.Descriptor.Entry == b.Id && timeline.Items.Contains(manual),
                "R4-A native Redo restores the final exact expression and preserves the manual item");

            vm.BeginIntentSettings(); view.SelectionTab.IsSelected = true; await Idle();
            var surface = view.RelativeSettingsSurface;
            // The preceding R3-G proof deliberately expanded the presentation editor.
            // Restore a focused SourceList viewport instead of aiming at a clipped thumb.
            ((Expander)surface.PresentationSettingsSurface.Content).IsExpanded = false;
            surface.SourceEditor.IsExpanded = true;
            surface.SettingsScroll.ScrollToBottom(); view.UpdateLayout(); await Idle();
            var root = surface.SettingsScroll;
            var inner = Descendant<ScrollViewer>(surface.SourceList) ?? throw new InvalidOperationException("R4-A inner source ScrollViewer missing");
            Assert(NestedWheelRouting.GetEnabled(root) && inner.ScrollableHeight > 0 && root.ScrollableHeight > 0,
                "R4-A actual Settings surface has the enabled production route and two nonempty scroll ranges");
            MouseWheelEventArgs Wheel(UIElement source, int delta)
            {
                var e = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta)
                    { RoutedEvent = UIElement.PreviewMouseWheelEvent, Source = source };
                source.RaiseEvent(e); return e;
            }
            inner.ScrollToVerticalOffset(inner.ScrollableHeight / 2); root.ScrollToVerticalOffset(root.ScrollableHeight / 2); await Idle();
            var innerBefore = inner.VerticalOffset; var outerBefore = root.VerticalOffset;
            var wheel = Wheel(inner, -120); await Idle();
            Round4Assert(wheel.Handled && inner.VerticalOffset > innerBefore && Math.Abs(root.VerticalOffset - outerBefore) < 0.01,
                "A8", "real WPF preview-wheel routing scrolls the nested source list while it can move");
            inner.ScrollToBottom(); root.ScrollToVerticalOffset(root.ScrollableHeight / 2); await Idle(); outerBefore = root.VerticalOffset;
            wheel = Wheel(inner, -120); await Idle();
            var down = wheel.Handled && root.VerticalOffset > outerBefore;
            inner.ScrollToTop(); root.ScrollToVerticalOffset(root.ScrollableHeight / 2); await Idle(); outerBefore = root.VerticalOffset;
            wheel = Wheel(inner, 120); await Idle();
            Round4Assert(down && wheel.Handled && root.VerticalOffset < outerBefore, "A9", "both nested-list edges hand further wheel input to outer Settings");
            root.ScrollToVerticalOffset(root.ScrollableHeight / 2); await Idle(); outerBefore = root.VerticalOffset; innerBefore = inner.VerticalOffset;
            wheel = Wheel((UIElement)root.Content, -120); await Idle();
            Round4Assert(wheel.Handled && root.VerticalOffset > outerBefore && inner.VerticalOffset == innerBefore,
                "A10", "wheel over outer content outside the source list scrolls only outer Settings");
            var outerDrag = await Round4DragScrollThumb(root);
            surface.SourceList.BringIntoView(); await Idle();
            SaveNamedView(view, "v042-round4-a-before-inner-thumb.png");
            var innerDrag = await Round4DragScrollThumb(inner);
            Round4Assert(outerDrag && innerDrag, "A11", "native mouse thumb drags still move both outer and inner scrollbar ranges");
            Round4Assert(!NestedWheelRouting.TryScroll(root, inner, -120, ModifierKeys.Control) &&
                !NestedWheelRouting.TryScroll(root, new TextBlock(), -120, ModifierKeys.None) &&
                !NestedWheelRouting.TryScroll(root, inner, 0, ModifierKeys.None),
                "A12", "modified, out-of-scope and zero wheel input are not swallowed");
            SaveNamedView(view, "v042-round4-a-settings-wheel.png");
            Round4Phase("A");
        }
        finally
        {
            if (box != null) box.IsDropDownOpen = false;
            vm.CloseExpressionTrialSession(); await vm.NavigationCompletion; vm.NavigationHostFactory = oldFactory;
        }
    }
}
