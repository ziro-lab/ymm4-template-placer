using System.ComponentModel;
using System.IO;
using System.Reflection;
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
    private sealed class UnrelatedSeekSurface { public Task SeekAsync(int frame) => Task.CompletedTask; }
    private static async Task InvokePreview(object preview, string method)
    {
        var action = preview.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException("Known public Preview fixture method missing: " + method);
        if (action.Invoke(preview, null) is not Task task) throw new InvalidOperationException("Preview fixture method does not return Task.");
        await task;
    }
    private static async Task<int[]> ObservePlaybackStart(Timeline timeline, object preview)
    {
        var frames = new List<int>();
        PropertyChangedEventHandler changed = (_, e) => { if (e.PropertyName == nameof(Timeline.CurrentFrame) && frames.Count < 40) frames.Add(timeline.CurrentFrame); };
        timeline.PropertyChanged += changed;
        try
        {
            await InvokePreview(preview, "TogglePlayAsync");
            Log("R3-F public playback StartPosition=" + preview.GetType().GetProperty("StartPosition", BindingFlags.Instance | BindingFlags.Public)?.GetValue(preview));
            await Task.Delay(650);
        }
        finally { timeline.PropertyChanged -= changed; await InvokePreview(preview, "StopAsync"); }
        await Idle(); return frames.ToArray();
    }
    private static async Task Round3RowClick(AssignmentRow row)
    {
        var view = View!;
        view.VoiceGrid.ScrollIntoView(row); view.VoiceGrid.UpdateLayout(); await Idle();
        var container = view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow
            ?? throw new InvalidOperationException("Current Voice row was not realized.");
        container.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = UIElement.MouseLeftButtonUpEvent, Source = container });
        await ViewModel!.NavigationCompletion; await Idle();
    }
    private static async Task VerifyHandsOnRound3Navigation(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-F Preview playback seek, viewport and latest-wins navigation";
        await ViewModel!.NavigationCompletion;
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!;
        var character = new Character { Name = "Round3 Navigation" };
        var first = new VoiceItem(character) { Frame = 180, Length = 90, Layer = 20 };
        var middle = new VoiceItem(character) { Frame = 1200, Length = 90, Layer = 20 };
        var last = new VoiceItem(character) { Frame = 6000, Length = 90, Layer = 20 };
        var extent = new TextItem { Frame = 0, Length = 12000, Layer = 0 };
        var source = scope.AddTemplate("R3F/expression", new TachieFaceItem(character) { Length = 10, Layer = 4 });
        var settings = PlacerSettingsStore.Copy(scope.Original); settings.Library = [source]; settings.Palettes = [];
        settings.IntentPalettes = [new(Guid.NewGuid(), "表情", "表情", new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name }, new(), [new(source.Id)]) { ExpressionCandidates = true }];
        settings.ExpressionBootstrapComplete = true; settings.ManualCharacterPaletteId = null; settings.ManualStylePaletteId = null;
        settings.Presentation = new();
        // Playback-origin proof uses the same small, silent scene as the public Lab.
        // Long-range geometry is added only for the separate viewport checks.
        scope.Apply(settings, [first], [first], 0);
        view.ExpressionTab.IsSelected = true; await Idle();
        var before = Signature(timeline);
        var host = ExpressionNavigationHost.Resolve();
        Log($"R3-F resolved public surfaces: preview={host.PreviewOwner?.GetType().FullName}; seek={host.CanSeek}; viewport={host.ViewportOwner?.GetType().FullName}; follow={host.CanFollow}");
        Round3Assert(host.CanSeek && host.PreviewOwner?.GetType().FullName == ExpressionNavigationHost.PreviewName &&
            host.SeekAsync!.Method.IsPublic && host.SeekAsync.Method.GetParameters().Select(x => x.ParameterType).SequenceEqual(new[] { typeof(int) }),
            "F3", "bounded live Preview DataContext resolves the exact public SeekAsync(int) Task capability");
        Assert(host.CanFollow, "R3-F pinned host resolves public ContainFrameInViewport/ScrollFrame independently");
        var actualSeeks = new List<int>(); var containsCalls = new List<int>(); var scrolls = new List<int>();
        var live = host with
        {
            SeekAsync = async frame => { actualSeeks.Add(frame); await host.SeekAsync!(frame); },
            ContainFrameInViewport = frame => { containsCalls.Add(frame); return host.ContainFrameInViewport!(frame); },
            ScrollFrame = frame => { scrolls.Add(frame); host.ScrollFrame!(frame); }
        };
        vm.NavigationHostFactory = () => live;
        AssignmentRow Row(VoiceItem voice) => vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
        async Task Navigate(VoiceItem voice) { vm.NavigateExpressionRowCommand.Execute(Row(voice)); await vm.NavigationCompletion; await Idle(); }
        void Follow(ExpressionViewportFollow mode) => scope.Current.Presentation = scope.Current.Presentation with { ViewportFollow = mode };
        int[] baselinePlayback = [], synchronizedPlayback = [];
        TaskCompletionSource<bool>? firstGate = null, lastGate = null;
        try
        {
            await InvokePreview(host.PreviewOwner!, "StopAsync");
            await host.SeekAsync!(0); await Task.Delay(350); await Idle();
            timeline.CurrentFrame = first.Frame; timeline.SelectItem(first); await Task.Delay(350);
            var displayedOnly = timeline.CurrentFrame;
            baselinePlayback = await ObservePlaybackStart(timeline, host.PreviewOwner!);
            Log($"R3-F Timeline-only: displayed={displayedOnly}; playback={string.Join(',', baselinePlayback)}");
            Assert(displayedOnly == first.Frame && baselinePlayback.Length > 0 && Math.Abs(baselinePlayback[0] - displayedOnly) > 15,
                "R3-F actual Timeline-only navigation reproduces stale playback start, not just a visual playhead check");
            await host.SeekAsync!(0); await Task.Delay(350);
            await Round3RowClick(Row(first));
            Round3Assert(timeline.CurrentFrame == first.Frame && timeline.SelectedItems.Count == 1 && ReferenceEquals(timeline.SelectedItems[0], first),
                "F1", "the actual Voice-row event routes through the root and immediately selects the Voice/start without mutating items");
            Round3Assert(actualSeeks.LastOrDefault() == first.Frame, "F4", "product row navigation seeks Preview to the same current Voice.Frame target");
            await Task.Delay(350); synchronizedPlayback = await ObservePlaybackStart(timeline, host.PreviewOwner!);
            Log($"R3-F product navigation: target={first.Frame}; playback={string.Join(',', synchronizedPlayback)}");
            Round3Assert(synchronizedPlayback.Length > 0 && Math.Abs(synchronizedPlayback[0] - first.Frame) <= 15,
                "F5", "actual playback after product row navigation begins within 15 frames of the selected Voice");
            Assert(Signature(timeline) == before, "R3-F product playback-origin proof does not mutate the test Voice");
            timeline.Items = [first, middle, last, extent]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.Refresh(); await Idle(); before = Signature(timeline);
            view.VoiceGrid.ScrollIntoView(Row(first)); view.VoiceGrid.UpdateLayout(); await Idle();
            var container = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(Row(first));
            var combo = Descendant<ComboBox>(container)!; var seeksBefore = actualSeeks.Count;
            timeline.CurrentFrame = 17; timeline.SelectItem(extent);
            combo.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                { RoutedEvent = UIElement.MouseLeftButtonUpEvent, Source = combo }); await Idle();
            Round3Assert(timeline.CurrentFrame == 17 && ReferenceEquals(timeline.SelectedItem, extent) && actualSeeks.Count == seeksBefore,
                "F2", "the actual template ComboBox mouse event is not consumed as Voice-row navigation");

            Follow(ExpressionViewportFollow.Off); containsCalls.Clear(); scrolls.Clear(); await Navigate(first);
            Round3Assert(containsCalls.Count == 0 && scrolls.Count == 0, "F8", "Off performs neither viewport containment lookup nor ScrollFrame");
            var requested = timeline.CurrentFrame; host.ScrollFrame!(last.Frame); await Idle();
            Round3Assert(timeline.CurrentFrame == requested, "F11", "the real public ScrollFrame moves only the viewport and not CurrentFrame");
            Follow(ExpressionViewportFollow.WhenOutside);
            host.ScrollFrame!(first.Frame); await Idle(); Assert(host.ContainFrameInViewport!(first.Frame), "R3-F inside fixture uses public semantic containment");
            containsCalls.Clear(); scrolls.Clear(); await Navigate(first);
            var insideNoScroll = containsCalls.SequenceEqual(new[] { first.Frame }) && scrolls.Count == 0;
            Assert(!host.ContainFrameInViewport!(last.Frame), "R3-F outside fixture is semantically outside the live viewport");
            containsCalls.Clear(); scrolls.Clear(); await Navigate(last);
            Round3Assert(insideNoScroll && containsCalls.SequenceEqual(new[] { last.Frame }) && scrolls.SequenceEqual(new[] { last.Frame }) && host.ContainFrameInViewport!(last.Frame),
                "F9", "WhenOutside uses real ContainFrameInViewport and ScrollFrame exactly when outside, never when already contained");
            Follow(ExpressionViewportFollow.Always); containsCalls.Clear(); scrolls.Clear(); await Navigate(last);
            Round3Assert(scrolls.SequenceEqual(new[] { last.Frame }) && containsCalls.Count == 0,
                "F10", "Always calls the real ScrollFrame for the final target even when already contained");

            vm.NavigationHostFactory = () => live with { PreviewOwner = null, SeekAsync = null };
            seeksBefore = actualSeeks.Count; scrolls.Clear(); await Navigate(middle);
            Round3Assert(timeline.CurrentFrame == middle.Frame && ReferenceEquals(timeline.SelectedItem, middle) && actualSeeks.Count == seeksBefore && scrolls.SequenceEqual(new[] { middle.Frame }),
                "F6", "missing Preview seek still performs CurrentFrame/Selection and real viewport follow independently");
            Round3Assert(vm.Status.Contains("同期は利用できません", StringComparison.Ordinal) && vm.Status.Length < 180 && !vm.HasError,
                "F7", "missing Preview capability gives a concise partial-capability notice without blocking navigation");
            vm.NavigationHostFactory = () => live with { ContainFrameInViewport = null, ScrollFrame = null };
            seeksBefore = actualSeeks.Count; await Navigate(first);
            Round3Assert(timeline.CurrentFrame == first.Frame && ReferenceEquals(timeline.SelectedItem, first) && actualSeeks.Count == seeksBefore + 1 && actualSeeks[^1] == first.Frame,
                "F12", "missing viewport methods still perform CurrentFrame/Selection and the actual public Preview seek");

            firstGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lastGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var inFlight = 0; var peak = 0; var calls = new List<int>(); var finalScrolls = new List<int>();
            async Task HeldSeek(int frame)
            {
                var call = calls.Count; calls.Add(frame); inFlight++; peak = Math.Max(peak, inFlight);
                try
                {
                    if (call == 0) { await firstGate.Task; throw new InvalidOperationException("Old seek failure must not replace the newest row status."); }
                    await lastGate.Task;
                }
                finally { inFlight--; }
            }
            vm.NavigationHostFactory = () => new(new object(), HeldSeek, _ => false, frame => finalScrolls.Add(frame));
            vm.NavigateExpressionRowCommand.Execute(Row(first));
            vm.NavigateExpressionRowCommand.Execute(Row(middle));
            vm.NavigateExpressionRowCommand.Execute(Row(last));
            var latestStatus = vm.Status;
            Assert(vm.NavigationWorkerActive && calls.SequenceEqual(new[] { first.Frame }) && timeline.CurrentFrame == last.Frame && ReferenceEquals(timeline.SelectedItem, last),
                "R3-F rapid clicks immediately select the newest row while exactly one seek remains in flight");
            firstGate.SetResult(true); await Idle();
            var staleIgnored = vm.Status == latestStatus && !vm.HasError && finalScrolls.Count == 0;
            Assert(calls.SequenceEqual(new[] { first.Frame, last.Frame }), "R3-F intermediate pending target is discarded before invoking the next seek");
            lastGate.SetResult(true); await vm.NavigationCompletion; await Idle();
            Round3Assert(peak == 1 && inFlight == 0 && !vm.NavigationWorkerActive && calls.SequenceEqual(new[] { first.Frame, last.Frame }) &&
                finalScrolls.SequenceEqual(new[] { last.Frame }) && timeline.CurrentFrame == last.Frame && ReferenceEquals(timeline.SelectedItem, last),
                "F13", "root navigation is single-worker/latest-wins: only first and final seek, only final viewport, last-click final state");
            Round3Assert(staleIgnored && vm.Status == latestStatus && !vm.HasError,
                "F14", "stale async seek failure/completion does not overwrite newer row status or viewport");
            vm.NavigationHostFactory = () => live;
            await Navigate(last);
            Round3Assert(!ExpressionNavigationHost.FromKnownInstances(new UnrelatedSeekSurface(), host.ViewportOwner).CanSeek &&
                ExpressionNavigationHost.FromKnownInstances(host.PreviewOwner, null).CanSeek &&
                ExpressionNavigationHost.FromKnownInstances(null, host.ViewportOwner).CanFollow &&
                host.SeekAsync!.Method.Name == "SeekAsync" && host.ScrollFrame!.Method.Name == "ScrollFrame",
                "F15", "adapter binds only known exact public capabilities and rejects unrelated same-signature objects; no focus/input/private-state workaround");
            Assert(Signature(timeline) == before, "R3-F all navigation, playback and capability tests preserve every Timeline item and association");
            File.WriteAllText(Path.Combine(output, "hands-on-round3-playback-observation.json"), JsonSerializer.Serialize(new
            {
                schema = "YMM4-Template-Placer-Round3-Playback/1", result = "PASS", target = first.Frame,
                timeline_only = baselinePlayback, product_navigation = synchronizedPlayback,
                route = "PreviewViewModel.SeekAsync(int)", viewport = "ContainFrameInViewport(int)/ScrollFrame(int)"
            }, new JsonSerializerOptions { WriteIndented = true }));
            SaveNamedView(view, "v042-round3-navigation-360.png");
            Round3Phase("F", "hands-on-round3-navigation.json"); Log("HANDS_ON_ROUND3_F=PASS");
        }
        finally
        {
            firstGate?.TrySetResult(true); lastGate?.TrySetResult(true);
            await vm.NavigationCompletion; vm.NavigationHostFactory = null;
            await InvokePreview(host.PreviewOwner!, "StopAsync");
        }
    }
}
