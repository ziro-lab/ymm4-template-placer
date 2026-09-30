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
    private static async Task VerifyIntentSetOrdering(Timeline timeline, UndoRedoManager undo)
    {
        stage = "native Set tab ordering";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!; var panel = view.RelativePaletteSurface;
        var source = scope.AddTemplate("SetOrder/演出", new TextItem { Length = 15, Layer = 2 });
        var voice = new VoiceItem(new Character { Name = "Set Order" }) { Frame = 100, Length = 30, Layer = 20 };
        var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = "Set Order" };
        IntentPalette Set(string name, string? character = "Set Order") => new(Guid.NewGuid(), name, "演出",
            target with { CharacterName = character }, new(), [new(source.Id) { Color = IntentTileColor.Blue }]);
        var a = Set("一番"); var b = Set("二番"); var c = Set("三番"); var d = Set("四番", null);
        var hidden1 = Set("別キャラA", "Other"); var hidden2 = Set("別キャラB", "Other");
        var fixture = PlacerSettingsStore.Copy(scope.Original); fixture.Library = [source]; fixture.IntentPalettes = [a, hidden1, b, c, hidden2, d];
        fixture.Palettes = []; fixture.ManualCharacterPaletteId = null; fixture.ManualStylePaletteId = null; fixture.ExpressionBootstrapComplete = true;
        scope.Apply(fixture, [voice], [voice]); await Idle();
        vm.SelectedIntentSet = vm.IntentSets.Single(x => x.Id == c.Id); await Idle();
        var signature = Signature(timeline); var selectedId = vm.SelectedIntentSet!.Id;
        var originalMetadata = fixture.IntentPalettes.ToDictionary(x => x.Id, x => JsonSerializer.Serialize(x));
        var staleSource = vm.IntentSets[0]; var staleTarget = vm.IntentSets[3];
        vm.ReorderIntentSet(new(staleSource, staleTarget, true)); await Idle();
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.IntentPalettes.Select(x => x.Id).SequenceEqual(new[] { b.Id, hidden1.Id, c.Id, d.Id, hidden2.Id, a.Id }) &&
            vm.IntentSets.Select(x => x.Id).SequenceEqual(new[] { b.Id, c.Id, d.Id, a.Id }) && vm.SelectedIntentSet?.Id == selectedId &&
            saved.IntentPalettes.All(x => JsonSerializer.Serialize(x) == originalMetadata[x.Id]) && Signature(timeline) == signature,
            "SET_ORDER visible reorder fills original visible slots, preserves hidden positions/all metadata/selected identity and writes no Timeline");
        Assert(!vm.ReorderIntentSetCommand.CanExecute(new IntentSetReorderRequest(staleSource, staleTarget)),
            "SET_ORDER stale drag references cannot reorder refreshed or changed contexts");
        var noOp = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        vm.ReorderIntentSet(new(vm.IntentSets[0], vm.IntentSets[0]));
        Assert(File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(noOp), "SET_ORDER self drop is no-op without a redundant save");
        var openingOrder = saved.IntentPalettes.Select(x => x.Id).ToArray();
        vm.IntentSettings!.OpenPaletteForEditing(vm.IntentSettings.Palettes.Single(x => x.Id == c.Id));
        Assert(vm.IntentSettings.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(new[] { b.Id, c.Id, d.Id, a.Id }),
            "SET_ORDER Settings shows the same persisted relative order as placement tabs");
        vm.IntentSettings.MoveOwned(1); vm.SaveIntentSettings();
        Assert(!vm.ReorderIntentSetCommand.CanExecute(new IntentSetReorderRequest(vm.IntentSets[0], vm.IntentSets[3], true)),
            "SET_ORDER direct reorder cannot reset an active committed Settings rollback snapshot");
        vm.RollbackSessionSettings(); await Idle();
        Assert(scope.Current.IntentPalettes.Select(x => x.Id).SequenceEqual(openingOrder) &&
            new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().IntentPalettes.Select(x => x.Id).SequenceEqual(openingOrder),
            "SET_ORDER subsequent Settings reorder and protected session rollback restore the drag-saved opening order");
        vm.IntentSettings!.SelectedPalette!.MinimumCount = "入力途中";
        var invalid = new IntentSetReorderRequest(vm.IntentSets[0], vm.IntentSets[3], true);
        Assert(!vm.ReorderIntentSetCommand.CanExecute(invalid), "SET_ORDER incomplete pending draft blocks direct reorder");
        var refused = false; try { vm.ReorderIntentSet(invalid); } catch (InvalidOperationException) { refused = true; }
        Assert(refused && vm.IntentSettings.SelectedPalette.MinimumCount == "入力途中" && Signature(timeline) == signature,
            "SET_ORDER rejected reorder retains the exact invalid draft and Timeline");
        vm.ResetIntentSettings();
        var field = typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)field.GetValue(vm)!;
        var disk = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        var external = disk.Concat(new byte[] { 10 }).ToArray(); File.WriteAllBytes(PlacerSettingsStore.DefaultPath, external);
        var beforeFailure = JsonSerializer.Serialize(scope.Current); refused = false;
        try { vm.ReorderIntentSet(new(vm.IntentSets[0], vm.IntentSets[3], true)); } catch (InvalidOperationException) { refused = true; }
        Assert(refused && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(external) &&
            JsonSerializer.Serialize(scope.Current) == beforeFailure && Signature(timeline) == signature,
            "SET_ORDER protected external-modification failure leaves disk/live order/Timeline unchanged");
        File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk); store.Load();
        await VerifySetTabPointerOrdering(timeline, panel, vm, view, signature);

        var manyVisible = Enumerable.Range(1, 9).Select(i => Set("多数" + i)).ToArray();
        var manyHidden = Enumerable.Range(1, 3).Select(i => Set("隠れ" + i, "Other")).ToArray();
        var many = PlacerSettingsStore.Copy(scope.Current);
        many.IntentPalettes = [manyVisible[0], manyHidden[0], manyVisible[1], manyVisible[2], manyVisible[3], manyHidden[1],
            manyVisible[4], manyVisible[5], manyVisible[6], manyHidden[2], manyVisible[7], manyVisible[8]];
        scope.Apply(many, [voice], [voice]); vm.SelectedIntentSet = vm.IntentSets[4]; await Idle();
        var manySelection = vm.SelectedIntentSet!.Id;
        await VerifySetOverflowDrag(timeline, panel, vm, signature);
        vm.ReorderIntentSet(new(vm.IntentSets[8], vm.IntentSets[0])); await Idle();
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(vm.IntentSets.Select(x => x.Id).SequenceEqual(manyVisible.Skip(8).Concat(manyVisible.Take(8)).Select(x => x.Id)) &&
            saved.IntentPalettes[1].Id == manyHidden[0].Id && saved.IntentPalettes[5].Id == manyHidden[1].Id && saved.IntentPalettes[9].Id == manyHidden[2].Id &&
            vm.SelectedIntentSet?.Id == manySelection,
            "SET_ORDER nine-applicable/twelve-stored reorder preserves hidden slots and selected identity across overflow");

        var generic = Enumerable.Range(1, 3).Select(i => new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "汎用" + i, null, [source.Id])).ToArray();
        var nonStyle = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Character, "隠れた既存セット", "Other", [source.Id]);
        var gfixture = PlacerSettingsStore.Copy(scope.Current); gfixture.Palettes = [generic[0], nonStyle, generic[1], generic[2]];
        gfixture.ManualStylePaletteId = generic[0].Id; gfixture.ManualCharacterPaletteId = nonStyle.Id;
        scope.Apply(gfixture, [voice], [voice]); vm.ObserveTimelinePointer(TimelinePointerOrigin.Ruler); vm.EndTimelinePointer(); await Idle();
        vm.SelectedIntentSet = vm.IntentSets[1]; var genericSelected = vm.SelectedIntentSet.Id;
        vm.ReorderIntentSet(new(vm.IntentSets[0], vm.IntentSets[2], true)); await Idle();
        saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(saved.Palettes.Select(x => x.Id).SequenceEqual(new[] { generic[1].Id, nonStyle.Id, generic[2].Id, generic[0].Id }) &&
            vm.SelectedIntentSet?.Id == genericSelected && saved.IntentPalettes.Select(x => x.Id).SequenceEqual(gfixture.IntentPalettes.Select(x => x.Id)),
            "SET_ORDER Generic reorder shares protected persistence and preserves non-Style slots/targeted order/selection");
        Assert(Signature(timeline) == signature, "SET_ORDER all direct and native-pointer reorder operations are Timeline zero-write");
        Log("INTENT_SET_ORDERING=PASS");
    }
    private static async Task VerifySetOverflowDrag(Timeline timeline, IntentPalettePanel panel, PlacerViewModel vm, string signature)
    {
        var scroll = panel.IntentSetHeaderScroll!; scroll.ScrollToLeftEnd(); await Idle();
        var tab = (TabItem)panel.IntentSetSegments.ItemContainerGenerator.ContainerFromIndex(0);
        var start = tab.PointToScreen(new Point(tab.ActualWidth / 2, tab.ActualHeight / 2));
        var edge = scroll.PointToScreen(new Point(scroll.ActualWidth - 3, scroll.ActualHeight / 2));
        var outside = panel.IntentTileItems.PointToScreen(new Point(180, 110));
        var stable = File.ReadAllBytes(PlacerSettingsStore.DefaultPath); var selected = vm.SelectedIntentSet!.Id;
        var visitedOffset = 0d; var markerObserved = false;
        System.Windows.DragEventHandler observe = (_, _) => { visitedOffset = Math.Max(visitedOffset, scroll.HorizontalOffset);
            markerObserved |= panel.IntentSetInsertionMarker.Visibility == Visibility.Visible; };
        panel.IntentSetRow.AddHandler(DragDrop.DragOverEvent, observe, true);
        try
        {
            Assert(Round2Input.SetCursorPos((int)Math.Round(start.X), (int)Math.Round(start.Y)), "SET_ORDER overflow source is physically reached");
            await Task.Delay(80); await Idle();
            Assert(Mouse.DirectlyOver is DependencyObject over && (ReferenceEquals(over, tab) || tab.IsAncestorOf(over)),
                "SET_ORDER overflow physical hit is the native source tab");
            Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            await Task.Run(() => { try { Thread.Sleep(100);
                for (var i = 1; i <= 5; i++) { Round2Input.SetCursorPos((int)Math.Round(start.X + (edge.X - start.X) * i / 5), (int)Math.Round(edge.Y)); Thread.Sleep(70); }
                for (var i = 0; i < 8; i++) { Round2Input.SetCursorPos((int)Math.Round(edge.X) - i % 2, (int)Math.Round(edge.Y)); Thread.Sleep(70); }
                Round2Input.SetCursorPos((int)Math.Round(outside.X), (int)Math.Round(outside.Y)); Thread.Sleep(70);
            } finally { Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); } });
            await Task.Delay(100); await Idle();
            Assert(visitedOffset > 0 && markerObserved && panel.IntentSetInsertionMarker.Visibility == Visibility.Collapsed &&
                File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(stable) && vm.SelectedIntentSet?.Id == selected && Signature(timeline) == signature,
                "SET_ORDER real overflow drag exposes later targets/insert feedback and outside cancellation retains order/selection/Timeline");
        }
        finally { panel.IntentSetRow.RemoveHandler(DragDrop.DragOverEvent, observe); Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); }
    }
    private static async Task VerifySetTabPointerOrdering(Timeline timeline, IntentPalettePanel panel, PlacerViewModel vm, PlacerView view, string signature)
    {
        view.PaletteTab.IsSelected = true; await Idle();
        var window = Window.GetWindow(view)!; window.Activate(); await Task.Delay(100); await Idle();
        TabItem Tab(int index) => (TabItem)panel.IntentSetSegments.ItemContainerGenerator.ContainerFromIndex(index);
        async Task<Point> PointAt(TabItem tab, double fraction = 0.5)
        {
            var point = tab.PointToScreen(new Point(tab.ActualWidth * fraction, tab.ActualHeight / 2));
            Assert(Round2Input.SetCursorPos((int)Math.Round(point.X), (int)Math.Round(point.Y)), "SET_ORDER OS cursor reaches native header");
            await Task.Delay(70); await Idle();
            Assert(Mouse.DirectlyOver is DependencyObject over && (ReferenceEquals(over, tab) || tab.IsAncestorOf(over)),
                "SET_ORDER physical hit is the actual native TabItem, not an occluding window");
            return point;
        }
        async Task Drag(TabItem from, TabItem to, bool cancel = false, bool outside = false)
        {
            var end = outside ? panel.IntentTileItems.PointToScreen(new Point(180, 110)) : await PointAt(to, 0.75);
            var start = await PointAt(from);
            Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
            await Task.Run(() => { try { Thread.Sleep(100); for (var i = 1; i <= 5; i++) {
                Round2Input.SetCursorPos((int)Math.Round(start.X + (end.X - start.X) * i / 5), (int)Math.Round(start.Y + (end.Y - start.Y) * i / 5)); Thread.Sleep(70); }
                if (cancel) { Round2Input.keybd_event(0x1B, 0, 0, UIntPtr.Zero); Thread.Sleep(70); Round2Input.keybd_event(0x1B, 0, 2, UIntPtr.Zero); }
            } finally { Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); } });
            await Task.Delay(100); await Idle();
        }
        var clickPoint = await PointAt(Tab(1)); await NativeRound2Click(clickPoint);
        Assert(vm.SelectedIntentSet?.Id == vm.IntentSets[1].Id && view.PaletteTab.IsSelected && Signature(timeline) == signature,
            "SET_ORDER real short header click selects once without placement or main-mode switch");
        var selected = vm.SelectedIntentSet!.Id; var ids = vm.IntentSets.Select(x => x.Id).ToArray();
        await Drag(Tab(0), Tab(3));
        Assert(vm.IntentSets.Select(x => x.Id).SequenceEqual(new[] { ids[1], ids[2], ids[3], ids[0] }) &&
            vm.SelectedIntentSet?.Id == selected && view.PaletteTab.IsSelected && Signature(timeline) == signature,
            "SET_ORDER actual unselected-header OS drag persists insertion while retaining selection and never places or switches MainTabs");
        var stable = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        await Drag(Tab(0), Tab(3), cancel: true);
        Assert(File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(stable) && vm.SelectedIntentSet?.Id == selected,
            "SET_ORDER Escape cancels native modal drag without selection or save");
        await Drag(Tab(0), Tab(3), outside: true);
        Assert(File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(stable) && vm.SelectedIntentSet?.Id == selected && Signature(timeline) == signature,
            "SET_ORDER outside-header drop cancels without tile placement, selection or save");
        vm.IntentSettings!.Palettes[0].MinimumCount = "入力途中";
        await Drag(Tab(0), Tab(3));
        Assert(File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(stable) && vm.SelectedIntentSet?.Id == selected && Signature(timeline) == signature,
            "SET_ORDER denied dirty-draft header drag remains zero-write and does not become a short click");
        vm.ResetIntentSettings();
    }
}
