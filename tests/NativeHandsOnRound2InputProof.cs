using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
    // OS input belongs only in the isolated proof build, never the distributable.
    private static class Round2Input
    {
        [StructLayout(LayoutKind.Sequential)] internal struct CursorPoint { public int X; public int Y; }
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out CursorPoint point);
        [DllImport("user32.dll")] internal static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] internal static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
        [DllImport("user32.dll")] internal static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    }
    private static async Task NativeRound2Click(Point screen, TimelinePointerOrigin? expected = null)
    {
        Assert(Round2Input.SetCursorPos((int)Math.Round(screen.X), (int)Math.Round(screen.Y)), "R2-A OS cursor moved to verified host hit point");
        await Task.Delay(100);
        Assert(Round2Input.GetCursorPos(out var actual) && Math.Abs(actual.X - screen.X) <= 1 && Math.Abs(actual.Y - screen.Y) <= 1,
            $"R2-A requested pointer ({screen.X:0},{screen.Y:0}) is physically reachable; actual=({actual.X},{actual.Y})");
        if (expected != null) Assert(TimelinePointerIntentClassifier.Classify(Mouse.DirectlyOver as DependencyObject) == expected,
            "R2-A actual pre-click hit route matches the freshly measured " + expected);
        Log($"R2-A input-before-down: over={Mouse.DirectlyOver?.GetType().FullName}; captured={Mouse.Captured?.GetType().FullName}; route={TimelinePointerIntentClassifier.Classify(Mouse.DirectlyOver as DependencyObject)}");
        Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        await Task.Delay(50); Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
        await Task.Delay(100); await Idle();
    }
    private static async Task VerifyHandsOnRound2Input(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R2-A pointer context and lifecycle";
        var vm = ViewModel!; var view = View!;
        var items = timeline.Items; var selection = timeline.SelectedItems; var frame = timeline.CurrentFrame;
        var legacy = vm.UseLegacyWorkspace; var visibility = view.Visibility;
        var host = Application.Current.Windows.Cast<Window>().Single(x => x.DataContext?.GetType().FullName == "YukkuriMovieMaker.ViewModels.MainViewModel");
        var width = host.Width; var height = host.Height; var windowState = host.WindowState;
        var left = host.Left; var top = host.Top;
        var voice = new VoiceItem(new Character { Name = "Round2 Input" }) { Frame = 10, Length = 60, Layer = 1 };
        var trace = new List<object>(); var genericTransitions = 0;
        var beforeHostSelectionObserved = false; var selectionAfterHostObserved = false;
        PropertyChangedEventHandler contextChanged = (_, e) =>
        {
            if (e.PropertyName == nameof(PlacerViewModel.PlacementContext))
            {
                if (vm.PlacementContext == PlacementContext.Generic) genericTransitions++;
                trace.Add(new { kind = "context", context = vm.PlacementContext.ToString(), selected = timeline.SelectedItems.Count, frame = timeline.CurrentFrame });
            }
        };
        PreProcessInputEventHandler beforeInput = (_, e) =>
        {
            if (e.StagingItem.Input is MouseButtonEventArgs mouse && mouse.RoutedEvent == Mouse.PreviewMouseDownEvent)
            {
                var origin = TimelinePointerIntentClassifier.Classify(mouse.MouseDevice.DirectlyOver as DependencyObject);
                if (origin == TimelinePointerOrigin.Item) beforeHostSelectionObserved |= vm.PlacementContext == PlacementContext.Generic;
                trace.Add(new { kind = "pointer-preview", origin = origin.ToString(), source = mouse.MouseDevice.DirectlyOver?.GetType().FullName,
                    context = vm.PlacementContext.ToString(), selected = timeline.SelectedItems.Count, frame = timeline.CurrentFrame });
            }
        };
        PropertyChangedEventHandler selectionChanged = (_, e) =>
        {
            if (e.PropertyName == nameof(Timeline.SelectedItems) && timeline.SelectedItems.Contains(voice))
                selectionAfterHostObserved |= vm.PlacementContext == PlacementContext.Selection;
        };
        try
        {
            vm.SetLegacyWorkspace(false); view.PaletteTab.IsSelected = true; view.Visibility = Visibility.Visible;
            // Hosted runner desktops may be smaller than 1280x900. Maximize into the real
            // working area instead of clicking logical coordinates outside the physical screen.
            host.WindowState = WindowState.Normal; host.Left = SystemParameters.WorkArea.Left; host.Top = SystemParameters.WorkArea.Top;
            host.WindowState = WindowState.Maximized; host.Activate(); await Task.Delay(150); await Idle();
            timeline.Items = [voice]; timeline.SelectedItems = []; timeline.CurrentFrame = 0;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); vm.ActivateIntentWorkspace(); await Idle();
            Assert(TimelinePointerIntentClassifier.IsPinnedHost && new PlacerToolPlugin().DefaultGroupName == YukkuriMovieMaker.Resources.Localization.Texts.ToolGroupUtilityName,
                "R2-A A1/A2 exact-host Utility group uses the shared host localization resource");
            Assert(view.PointerRouter.IsAttached, "R2-A visible common workspace attaches its one pointer router");
            var signature = Signature(timeline);
            vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer();
            vm.ObserveTimelinePointer(TimelinePointerOrigin.Item);
            Assert(vm.PlacementContext == PlacementContext.Generic, "R2-A pending Item intent does not consume pre-host selection");
            timeline.SelectedItems = [voice]; vm.EndTimelinePointer();
            Assert(vm.PlacementContext == PlacementContext.Selection, "R2-A post-host nonempty selection consumes Item intent");
            vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground);
            timeline.SelectedItems = [voice]; vm.EndTimelinePointer();
            Assert(vm.PlacementContext == PlacementContext.Generic && timeline.SelectedItems.Contains(voice), "R2-A background retains selection and suppresses same-gesture fallback");
            vm.ObserveTimelinePointer(TimelinePointerOrigin.Unknown); timeline.SelectedItems = [voice]; vm.EndTimelinePointer();
            Assert(vm.PlacementContext == PlacementContext.Generic, "R2-A unknown pointer keeps context even if host emits a selection notification");
            timeline.CurrentFrame += 3;
            Assert(vm.PlacementContext == PlacementContext.Generic, "R2-A programmatic CurrentFrame does not switch context");
            timeline.SelectedItems = [voice];
            Assert(vm.PlacementContext == PlacementContext.Selection, "R2-A independent non-pointer selection supplies the bounded selection fallback");
            Assert(Signature(timeline) == signature, "R2-A all synthetic context transitions are Timeline-item zero-write");

            var visuals = RelativeVisuals(host).OfType<FrameworkElement>().ToArray();
            var itemView = visuals.Single(x => x.GetType().FullName == TimelinePointerIntentClassifier.ItemViewName && x.IsVisible && x.ActualWidth > 5);
            var timelineView = visuals.First(x => x.GetType().FullName == TimelinePointerIntentClassifier.TimelineViewName && x.IsVisible);
            var rulerView = visuals.First(x => x.GetType().FullName == TimelinePointerIntentClassifier.ScaleViewName && x.IsVisible);
            async Task<Point> HitPoint(FrameworkElement area, TimelinePointerOrigin expected, bool preferRight)
            {
                var xs = preferRight ? new[] { .92, .82, .68, .5, .3 } : new[] { .5, .3, .7, .1, .9 };
                foreach (var x in xs) foreach (var y in new[] { .5, .65, .8, .3 })
                {
                    var point = area.TranslatePoint(new Point(area.ActualWidth * x, area.ActualHeight * y), host);
                    var hit = host.InputHitTest(point) as DependencyObject;
                    if (TimelinePointerIntentClassifier.Classify(hit) != expected) continue;
                    var screen = host.PointToScreen(point);
                    if (!Round2Input.SetCursorPos((int)Math.Round(screen.X), (int)Math.Round(screen.Y))) continue;
                    await Task.Delay(60); await Idle();
                    var actual = Mouse.DirectlyOver as DependencyObject;
                    if (TimelinePointerIntentClassifier.Classify(actual) == expected) return screen;
                    Log($"R2-A rejected occluded host point for {expected}: actual={actual?.GetType().FullName}; window={Window.GetWindow(actual ?? host)?.GetType().FullName}; x={screen.X:0}; y={screen.Y:0}");
                }
                throw new InvalidOperationException("No exact host hit route for " + expected);
            }
            Point rulerPoint;
            // Host InputHitTest alone ignores a floating Tool or popup covering that point.
            // Move without clicking through a bounded candidate grid, then require the real
            // device hit to match the exact route before any input can count as proof.
            vm.PropertyChanged += contextChanged; InputManager.Current.PreProcessInput += beforeInput;
            timeline.PropertyChanged += selectionChanged;
            await NativeRound2Click(await HitPoint(timelineView, TimelinePointerOrigin.TimelineBackground, true), TimelinePointerOrigin.TimelineBackground);
            Assert(vm.PlacementContext == PlacementContext.Generic && timeline.SelectedItems.Contains(voice), "R2-A A5 real background input chooses Generic without clearing the retained Voice");
            await NativeRound2Click(await HitPoint(itemView, TimelinePointerOrigin.Item, false), TimelinePointerOrigin.Item);
            Assert(beforeHostSelectionObserved && selectionAfterHostObserved && vm.PlacementContext == PlacementContext.Selection,
                "R2-A A3/A4 real already-selected Item click waits for host Selection and enters Selection context");
            await NativeRound2Click(await HitPoint(rulerView, TimelinePointerOrigin.Ruler, false), TimelinePointerOrigin.Ruler);
            Assert(vm.PlacementContext == PlacementContext.Generic, "R2-A A6 real ruler input chooses Generic");
            // The common Tool can change layout when its context changes. Never reuse
            // a screen point measured before that change; a stale hit must not count as proof.
            rulerPoint = await HitPoint(rulerView, TimelinePointerOrigin.Ruler, false);
            var rulerStart = timeline.CurrentFrame;
            Round2Input.SetCursorPos((int)rulerPoint.X, (int)rulerPoint.Y); Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            for (var i = 1; i <= 3; i++) { Round2Input.SetCursorPos((int)rulerPoint.X + i * 15, (int)rulerPoint.Y); await Task.Delay(60); }
            Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); await Task.Delay(100); await Idle();
            Assert(timeline.CurrentFrame != rulerStart && vm.PlacementContext == PlacementContext.Generic, "R2-A A7 real ruler drag changes time without context churn");
            var keyStart = timeline.CurrentFrame;
            Round2Input.keybd_event(0x27, 0, 0, UIntPtr.Zero); Round2Input.keybd_event(0x27, 0, 2, UIntPtr.Zero); await Task.Delay(150); await Idle();
            Assert(timeline.CurrentFrame != keyStart && vm.PlacementContext == PlacementContext.Generic, "R2-A A8 real Right-key CurrentFrame movement keeps Generic");
            var playbackStart = timeline.CurrentFrame;
            Round2Input.keybd_event(0x20, 0, 0, UIntPtr.Zero); Round2Input.keybd_event(0x20, 0, 2, UIntPtr.Zero); await Task.Delay(500);
            Round2Input.keybd_event(0x20, 0, 0, UIntPtr.Zero); Round2Input.keybd_event(0x20, 0, 2, UIntPtr.Zero); await Task.Delay(120); await Idle();
            Assert(timeline.CurrentFrame != playbackStart && vm.PlacementContext == PlacementContext.Generic, "R2-A A9 real playback changes time and never switches context");
            Assert(genericTransitions == 2,
                "R2-A ruler/keyboard/playback do not repeatedly emit context transitions");

            var forwarded = 0;
            using (var duplicateProbe = new TimelinePointerInputRouter(_ => forwarded++, () => { }))
            {
                duplicateProbe.Attach(); duplicateProbe.Attach(); await NativeRound2Click(await HitPoint(timelineView, TimelinePointerOrigin.TimelineBackground, true), TimelinePointerOrigin.TimelineBackground);
                Assert(forwarded == 1, "R2-A repeated Attach delivers one actual pointer event, not two");
                duplicateProbe.Detach(); duplicateProbe.Detach(); await NativeRound2Click(await HitPoint(timelineView, TimelinePointerOrigin.TimelineBackground, true), TimelinePointerOrigin.TimelineBackground);
                Assert(forwarded == 1, "R2-A repeated Detach leaves no global input handler");
            }
            for (var i = 0; i < 3; i++)
            {
                view.Visibility = Visibility.Collapsed; await Idle(); Assert(!view.PointerRouter.IsAttached, "R2-A hide detaches input router " + i);
                view.Visibility = Visibility.Visible; await Idle(); Assert(view.PointerRouter.IsAttached, "R2-A reopen attaches input router once " + i);
            }
            view.DataContext = null; await Idle();
            Assert(!view.PointerRouter.IsAttached && !vm.HasIntentTimeline, "R2-A DataContext removal detaches input and Timeline handlers");
            view.DataContext = vm; await Idle();
            Assert(view.PointerRouter.IsAttached && vm.HasIntentTimeline, "R2-A DataContext reattachment restores one input/Timeline route");
            vm.SetLegacyWorkspace(true); await Idle(); Assert(!view.PointerRouter.IsAttached, "R2-A legacy workspace detaches common input router");
            vm.SetLegacyWorkspace(false); await Idle(); Assert(view.PointerRouter.IsAttached, "R2-A returning to common workspace attaches input router");
            Assert(Signature(timeline) == signature, "R2-A actual host context input and lifecycle leave all Timeline items unchanged");
            File.WriteAllText(Path.Combine(output, "hands-on-round2-input.json"), JsonSerializer.Serialize(new {
                schema = "YMM4-Template-Placer-Round2-Input/1", host = "YMM4 4.55.1.1 Lite", result = "PASS", trace
            }, new JsonSerializerOptions { WriteIndented = true }));
            Log("HANDS_ON_ROUND2_A=PASS");
        }
        finally
        {
            File.WriteAllText(Path.Combine(output, "round2-context-input-trace.json"), JsonSerializer.Serialize(trace, new JsonSerializerOptions { WriteIndented = true }));
            Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            InputManager.Current.PreProcessInput -= beforeInput; vm.PropertyChanged -= contextChanged; timeline.PropertyChanged -= selectionChanged;
            vm.EndTimelinePointer(); timeline.Items = items; timeline.SelectedItems = selection; timeline.CurrentFrame = frame;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); view.Visibility = visibility;
            host.WindowState = WindowState.Normal; host.Width = width; host.Height = height; host.Left = left; host.Top = top; host.WindowState = windowState;
            vm.SetLegacyWorkspace(legacy); vm.Refresh();
        }
    }
}
