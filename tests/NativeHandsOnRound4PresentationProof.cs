using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnRound4Presentation(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R4-B compact header, global presentation and common Voice row height";
        var vm = ViewModel!; var view = View!;
        await vm.NavigationCompletion;
        using var scope = new Round3Fixture(timeline, undo);
        var a = scope.AddTemplate("R4B/first", new TextItem { Length = 11, Layer = 2 });
        var b = scope.AddTemplate("R4B/second", new TextItem { Length = 17, Layer = 2 });
        var character = new Character { Name = "Round4 Height" };
        var longSerif = string.Concat(Enumerable.Repeat("長いセリフを文字サイズを縮めずに複数行で確認します。", 7));
        var first = new VoiceItem(character) { Frame = 200, Length = 30, Layer = 20, Serif = longSerif };
        var last = new VoiceItem(character) { Frame = 250, Length = 30, Layer = 20, Serif = "次の行" };
        var layer = new LayerPolicy { UseTemplateLayer = false, Minimum = 4, Maximum = 12, Preferred = 8, SearchMode = LayerSearchMode.DoNotPlace };
        var left = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "R4B A", null, [a.Id, b.Id]) { Layer = layer };
        var right = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "R4B B", null, [b.Id]) { Layer = layer };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [a, b]; settings.Palettes = [left, right]; settings.IntentPalettes = [];
        settings.ManualCharacterPaletteId = null; settings.ManualStylePaletteId = left.Id;
        settings.ExpressionBootstrapComplete = true;
        settings.Presentation = new() { LayoutMode = PaletteLayoutMode.Fixed, FixedColumns = 4, ShortcutsEnabled = true,
            PositionShortcuts = [new(0, Key.F20)] };
        scope.Apply(settings, [first, last], [], 100);
        vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer(); await Idle();
        var palette = view.RelativePaletteSurface;
        var before = Signature(timeline);
        Round4Assert(!RelativeVisuals(palette).OfType<FrameworkElement>().Any(x => x.Name == "IntentSetSettingsButton"),
            "B1", "normal placement has no permanent Set-wide button");
        var titleY = palette.IntentContextTitleText.TranslatePoint(new Point(), palette).Y;
        var layerY = palette.GenericLayerSurface.TranslatePoint(new Point(), palette).Y;
        Round4Assert(palette.GenericLayerSurface.IsVisible && Math.Abs(titleY - layerY) < 2,
            "B2", "inline Generic layer editor shares the context title row at the real narrow width");
        Round4Assert(palette.GenericLayerSurface.GenericTargetBox.IsVisible && palette.GenericLayerSurface.GenericOccupiedPicker.IsVisible &&
            palette.GenericLayerSurface.ActualHeight <= 48 && palette.IntentContextHeader.ActualHeight >= 48,
            "B3", "layer target and escape direction stay visible beside the two-line header while large history controls fill the header height");
        palette.UpdateLayout();
        var undoCenterBefore = palette.NativeUndoButton.TranslatePoint(
            new Point(palette.NativeUndoButton.ActualWidth / 2, palette.NativeUndoButton.ActualHeight / 2), palette).X;
        vm.ObserveTimelinePointer(TimelinePointerOrigin.Item); timeline.SelectedItems = [first]; vm.EndTimelinePointer(); await Idle(); palette.UpdateLayout();
        var undoCenterAfter = palette.NativeUndoButton.TranslatePoint(
            new Point(palette.NativeUndoButton.ActualWidth / 2, palette.NativeUndoButton.ActualHeight / 2), palette).X;
        Round4Assert(!palette.GenericLayerSurface.IsVisible && Math.Abs(undoCenterBefore - undoCenterAfter) < 0.5,
            "B3H", "Undo/Redo stay at the exact same horizontal center when the right-side Generic layer controls disappear");
        timeline.SelectedItems = []; vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer(); await Idle();
        try
        {
            Window.GetWindow(view)!.Activate();
            var box = palette.GenericLayerSurface.GenericTargetBox;
            var statusBeforeWheel = vm.Status;
            await NativeRound2Click(box.PointToScreen(new Point(box.ActualWidth / 2, box.ActualHeight / 2))); await Idle();
            Round2Input.mouse_event(0x0800, 0, 0, 120, UIntPtr.Zero); await Task.Delay(100); await Idle();
            var incremented = scope.Current.Palettes.Single(x => x.Id == left.Id).Layer.Preferred == 9;
            Round2Input.mouse_event(0x0800, 0, 0, unchecked((uint)-120), UIntPtr.Zero); await Task.Delay(100); await Idle();
            Round4Assert(incremented && scope.Current.Palettes.Single(x => x.Id == left.Id).Layer.Preferred == 8 && Signature(timeline) == before,
                "B4", "real wheel over inline layer number applies +1/-1 through the protected target path without Timeline mutation");
            Assert(vm.Status == statusBeforeWheel,
                "UI corrective C1 routine wheel success does not replace bottom Status with per-notch confirmation");
            var staleDraft = vm.GenericLayerTarget!;
            vm.StepGenericLayer(staleDraft, int.MaxValue); await Idle();
            Assert(scope.Current.Palettes.Single(x => x.Id == left.Id).Layer.Preferred == 12,
                "UI U1 wheel clamps at saved upper bound instead of overflowing or changing the range");
            vm.StepGenericLayer(vm.GenericLayerTarget!, int.MinValue); await Idle();
            Assert(scope.Current.Palettes.Single(x => x.Id == left.Id).Layer.Preferred == 4,
                "UI U1 wheel clamps at saved lower bound");
            var unchanged = JsonSerializer.Serialize(scope.Current);
            vm.StepGenericLayer(staleDraft, 1); await Idle();
            Assert(vm.HasError && JsonSerializer.Serialize(scope.Current) == unchanged,
                "UI U1 stale wheel request cannot write through a replaced draft");
            vm.GenericLayerTarget!.Target = "not a number";
            vm.StepGenericLayer(vm.GenericLayerTarget, 1); await Idle();
            Assert(vm.HasError && vm.GenericLayerTarget.Target == "not a number" && JsonSerializer.Serialize(scope.Current) == unchanged,
                "UI U1 wheel preserves incomplete input instead of guessing a numeric target");
            vm.ResetGenericLayerTargetCommand.Execute(null);
            var editor = palette.GenericLayerSurface;
            editor.GenericTargetBox.Text = "9"; await Idle();
            var blocked = !vm.ExecuteIntentTileCommand.CanExecute(vm.IntentTiles[0]);
            Keyboard.Focus(editor.GenericTargetBox); await Round3PressKey(Key.Enter); await Idle();
            var applied = scope.Current.Palettes.Single(x => x.Id == left.Id).Layer.Preferred == 9;
            editor.GenericTargetBox.Text = "10"; await Idle(); Keyboard.Focus(editor.GenericTargetBox); await Round3PressKey(Key.Escape); await Idle();
            Round4Assert(blocked && applied && vm.GenericLayerTarget is { Target: "9", HasChanges: false } && Signature(timeline) == before,
                "B5", "inline Enter saves the existing numeric draft and Escape restores it; dirty input cannot execute an old target");
            Round4Assert(Enumerable.Range(1, 11).All(x => round3Checks.ContainsKey("D" + x)),
                "B6", "all eleven retained native Generic layer planner safety checks passed on this run");

            await NativeRound2Click(palette.PanelQuickSettingsButton.PointToScreen(new Point(palette.PanelQuickSettingsButton.ActualWidth / 2,
                palette.PanelQuickSettingsButton.ActualHeight / 2))); await Idle();
            Round4Assert(view.PaletteTab.IsSelected && palette.PanelQuickSettingsPopup.IsOpen &&
                vm.PanelQuickPresentation != null && vm.SelectedIntentSet?.Id == left.Id,
                "B7", "bottom quick settings opens locally for the exact current Generic Set without switching tabs");
            var quick = palette.PanelQuickSettingsSurface;
            var shapes = new[] { quick.QuickShapeRounded, quick.QuickShapeSquare, quick.QuickShapeCircle };
            var all = shapes.All(x => ReferenceEquals(x.Command, vm.ShapeCurrentIntentSetCommand));
            foreach (var button in shapes)
            {
                var point = button.PointToScreen(new Point(button.ActualWidth / 2, button.ActualHeight / 2));
                await NativeRound2Click(point); await Idle();
                var expected = (IntentTileShape)button.CommandParameter;
                all &= scope.Current.Palettes.Single(x => x.Id == left.Id).LibraryEntryIds.All(id =>
                    scope.Current.Palettes.Single(x => x.Id == left.Id).AppearanceFor(id).Shape == expected);
                if (!palette.PanelQuickSettingsPopup.IsOpen) break;
            }
            Log($"UI U2 B8 trace: buttons={shapes.Length}; all={all}; popup={palette.PanelQuickSettingsPopup.IsOpen}; currentSet={vm.SelectedIntentSet?.Id}; expectedSet={left.Id}; shapes={string.Join(",", scope.Current.Palettes.Single(x => x.Id == left.Id).LibraryEntryIds.Select(id => scope.Current.Palettes.Single(x => x.Id == left.Id).AppearanceFor(id).Shape))}");
            Round4Assert(all && palette.PanelQuickSettingsPopup.IsOpen &&
                scope.Current.Palettes.Single(x => x.Id == left.Id).LibraryEntryIds.All(id =>
                    scope.Current.Palettes.Single(x => x.Id == left.Id).AppearanceFor(id).Shape == IntentTileShape.Circle),
                "B8", "quick settings Set-wide shape writes current membership shapes through the protected store and stays open");

            var layoutPicker = quick.QuickLayoutPicker;
            await NativeRound2Click(layoutPicker.PointToScreen(new Point(layoutPicker.ActualWidth / 2, layoutPicker.ActualHeight / 2))); await Idle();
            var nestedInteractionStayedOpen = palette.PanelQuickSettingsPopup.IsOpen && layoutPicker.IsDropDownOpen;
            layoutPicker.IsDropDownOpen = false; await Idle();
            Assert(nestedInteractionStayedOpen && palette.PanelQuickSettingsPopup.IsOpen,
                "UI Round2 F8 internal quick-settings ComboBox interaction stays open");

            var outsideTarget = palette.IntentContextTitleText;
            var outsidePoint = outsideTarget.PointToScreen(new Point(Math.Min(8, Math.Max(1, outsideTarget.ActualWidth / 2)), outsideTarget.ActualHeight / 2));
            Assert(Round2Input.SetCursorPos((int)Math.Round(outsidePoint.X), (int)Math.Round(outsidePoint.Y)),
                "UI Round2 F8 OS cursor moved to an owner-window point outside quick settings");
            await Task.Delay(80); await Idle();
            var outsideHit = Mouse.DirectlyOver as DependencyObject;
            var popupRoot = palette.PanelQuickSettingsPopup.Child as FrameworkElement;
            Assert(outsideHit != null &&
                (ReferenceEquals(outsideHit, outsideTarget) || outsideTarget.IsAncestorOf(outsideHit)) &&
                (popupRoot == null || (!ReferenceEquals(outsideHit, popupRoot) && !popupRoot.IsAncestorOf(outsideHit))),
                "UI Round2 F8 physical pointer reaches the intended owner-window outside target, not Popup content");

            var outsideDelivered = false;
            var outsideHandledOnArrival = true;
            MouseButtonEventHandler outsideProbe = (_, e) => { outsideDelivered = true; outsideHandledOnArrival = e.Handled; };
            outsideTarget.AddHandler(Mouse.PreviewMouseDownEvent, outsideProbe, true);
            try
            {
                await NativeRound2Click(outsidePoint); await Idle();
            }
            finally { outsideTarget.RemoveHandler(Mouse.PreviewMouseDownEvent, outsideProbe); }
            Assert(outsideDelivered && !outsideHandledOnArrival && !palette.PanelQuickSettingsPopup.IsOpen &&
                palette.PanelQuickSettingsButton.IsChecked != true,
                "UI Round2 F8 owner-window outside click closes quick settings without swallowing the YMM4 click");

            await NativeRound2Click(palette.PanelQuickSettingsButton.PointToScreen(new Point(palette.PanelQuickSettingsButton.ActualWidth / 2,
                palette.PanelQuickSettingsButton.ActualHeight / 2))); await Idle();
            Assert(palette.PanelQuickSettingsPopup.IsOpen,
                "UI Round2 F8 quick settings can reopen normally after light-dismiss");

            var ownerWindow = Window.GetWindow(view)!;
            var popupProbe = new Window { Width = 120, Height = 80, ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow };
            var popupClosedOnDeactivate = false;
            var ownerDeactivationCount = 0;
            EventHandler ownerDeactivated = (_, _) => ownerDeactivationCount++;
            ownerWindow.Deactivated += ownerDeactivated;
            try
            {
                var ownerActivated = ownerWindow.Activate();
                await Task.Delay(80); await Idle();
                var ownerWasActive = ownerWindow.IsActive;
                popupProbe.Show();
                var probeActivated = popupProbe.Activate();
                await Task.Delay(120); await Idle();
                Log($"UI corrective C4 activation trace: ownerActivate={ownerActivated}; ownerBefore={ownerWasActive}; probeActivate={probeActivated}; probeActive={popupProbe.IsActive}; ownerAfter={ownerWindow.IsActive}; ownerDeactivated={ownerDeactivationCount}; popup={palette.PanelQuickSettingsPopup.IsOpen}; checked={palette.PanelQuickSettingsButton.IsChecked}");
                Assert(ownerActivated && ownerWasActive && probeActivated && popupProbe.IsActive && ownerDeactivationCount > 0,
                    "UI corrective C4 proof actually transfers WPF activation away from the owner Window");
                popupClosedOnDeactivate = !palette.PanelQuickSettingsPopup.IsOpen && palette.PanelQuickSettingsButton.IsChecked != true;
            }
            finally
            {
                ownerWindow.Deactivated -= ownerDeactivated;
                popupProbe.Close(); ownerWindow.Activate(); await Idle();
            }
            Assert(popupClosedOnDeactivate && !palette.PanelQuickSettingsPopup.IsOpen,
                "UI corrective C4 owner Window deactivation closes quick settings and activation does not reopen it");
            palette.PanelQuickSettingsButton.IsChecked = false; await Idle();
            vm.ChangeIntentTileAppearance(vm.IntentTiles[0], x => x with { Shape = IntentTileShape.Square }); await Idle();
            Round4Assert(vm.IntentTiles[0].Shape == IntentTileShape.Square && vm.IntentTiles[1].Shape == IntentTileShape.Circle,
                "B9", "one tile still overrides its own shape after a Set-wide change");
            vm.BeginIntentSettings(); view.SelectionTab.IsSelected = true; await Idle();
            var settingsSurface = view.RelativeSettingsSurface;
            var sourceSettingsY = settingsSurface.SourceEditor.TranslatePoint(new Point(), settingsSurface.SettingsScroll).Y;
            var presentationSettingsY = settingsSurface.PresentationSettingsSurface.TranslatePoint(new Point(), settingsSurface.SettingsScroll).Y;
            Round4Assert(settingsSurface.SettingsScroll.IsAncestorOf(settingsSurface.PresentationSettingsSurface) &&
                ((Expander)settingsSurface.PresentationSettingsSurface.Content).Header?.ToString() == "全体の表示・操作" &&
                presentationSettingsY > sourceSettingsY,
                "B10", "full Settings keeps global presentation separate from Set editing but lower in the shared outer scroll");
            view.PaletteTab.IsSelected = true; await Idle();

            view.Width = 720; await Idle(); palette.UpdateLayout();
            var tilePanel = RelativeVisuals(palette.IntentTileItems).OfType<PaletteTilePanel>().Single();
            var wideCell = (FrameworkElement)VisualTreeHelper.GetChild(tilePanel, 0);
            var wideResponsive = tilePanel.ColumnCount == 4 && tilePanel.CellSize > 104 &&
                Math.Abs(wideCell.ActualWidth - tilePanel.CellSize) < 1 &&
                Math.Abs(wideCell.ActualHeight - tilePanel.CellSize) < 1;
            view.Width = 360; await Idle(); palette.UpdateLayout();
            tilePanel = RelativeVisuals(palette.IntentTileItems).OfType<PaletteTilePanel>().Single();
            Assert(wideResponsive && tilePanel.ColumnCount == 4 && Math.Abs(tilePanel.CellSize - 104) < 1,
                "UI corrective C3 Fixed keeps four slots, grows square cells on wide viewport and retains 104-DIP minimum when narrow");

            var presentation = JsonSerializer.Serialize(scope.Current.Presentation);
            vm.SelectedIntentSet = vm.IntentSets.Single(x => x.Id == right.Id); await Idle();
            Round4Assert(vm.PaletteLayout == PaletteLayoutMode.Fixed, "B11", "Set switching retains the global Auto/Fixed mode");
            Round4Assert(vm.PaletteFixedColumns == 4, "B12", "Set switching retains the global fixed column count");
            Round4Assert(JsonSerializer.Serialize(scope.Current.Presentation) == presentation,
                "B13", "Set switching does not rewrite global shortcut/presentation metadata");
            var originals = timeline.Items.ToArray();
            var admitted = vm.TryExecutePositionShortcut(Key.F20, ModifierKeys.None); await Idle();
            var added = timeline.Items.Except(originals).ToArray();
            var executed = admitted && !vm.HasError && added.Length == 1 && added[0].Length == 17 && added[0].Frame == 100;
            await undo.UndoAsync(); await Idle();
            vm.SelectedIntentSet = vm.IntentSets.Single(x => x.Id == left.Id); await Idle();
            admitted = vm.TryExecutePositionShortcut(Key.F20, ModifierKeys.None); await Idle();
            added = timeline.Items.Except(originals).ToArray();
            executed &= admitted && added.Length == 1 && added[0].Length == 11;
            await undo.UndoAsync(); await Idle();
            Round4Assert(executed && Signature(timeline) == before, "B14", "the same slot key executes each Set's current first tile and each action is one native Undo");
            Round4Assert(new[] { typeof(PaletteDefinition), typeof(IntentPalette) }.All(t =>
                t.GetProperty("FixedColumns") == null && t.GetProperty("PositionShortcuts") == null),
                "B15", "neither Set backend stores duplicated column or shortcut metadata");
            var node = JsonSerializer.SerializeToNode(scope.Current)!.AsObject(); node["Presentation"]!.AsObject().Remove("ExpressionRowHeight");
            var path = Path.Combine(output, "round4-row-height-old-settings.json"); File.WriteAllText(path, node.ToJsonString());
            var bytes = File.ReadAllBytes(path); var loaded = new PlacerSettingsStore(path).Load();
            Round4Assert(loaded.Presentation.ExpressionRowHeight == 36 && File.ReadAllBytes(path).SequenceEqual(bytes),
                "B16", "old settings missing row height load 36 without rewriting their bytes");
            bool Rejected(int height)
            {
                try { (loaded.Presentation with { ExpressionRowHeight = height }).Validate(); return false; }
                catch (InvalidDataException) { return true; }
            }
            Round4Assert(Rejected(31) && Rejected(97),
                "B17", "the store rejects common row heights outside 32-96");
            view.ExpressionTab.IsSelected = true; await Idle();
            var rows = vm.Rows.ToArray(); view.VoiceGrid.ScrollIntoView(rows[0]); view.VoiceGrid.UpdateLayout(); await Idle();
            var container = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(rows[0]);
            var serif = RelativeVisuals(container).OfType<TextBlock>().First(x => x.Text == longSerif);
            var fontSize = serif.FontSize;
            var beforeDragBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
            var rowPoint = container.TranslatePoint(new Point(20, container.ActualHeight - 1), view.VoiceGrid);
            var startScreen = view.VoiceGrid.PointToScreen(rowPoint);
            var targetScreen = view.VoiceGrid.PointToScreen(new Point(rowPoint.X, rowPoint.Y + 40));
            Assert(Round2Input.SetCursorPos((int)Math.Round(startScreen.X), (int)Math.Round(startScreen.Y)),
                "UI corrective C2 OS cursor moved to Voice row resize boundary");
            var held = false;
            double previewHeight;
            try
            {
                Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); held = true; await Task.Delay(60);
                Assert(Round2Input.SetCursorPos((int)Math.Round(targetScreen.X), (int)Math.Round(targetScreen.Y)),
                    "UI corrective C2 OS cursor dragged the captured Voice row boundary");
                await Task.Delay(100); await Idle(); view.VoiceGrid.UpdateLayout();
                previewHeight = view.VoiceGrid.RowHeight;
                Assert(previewHeight > 36 && previewHeight <= 96 &&
                    File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(beforeDragBytes) &&
                    new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.ExpressionRowHeight == 36,
                    "UI corrective C2 row-boundary DragDelta previews one common height with zero settings writes");
            }
            finally
            {
                if (held) Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
            }
            await Task.Delay(100); await Idle(); view.VoiceGrid.UpdateLayout();
            var durableAfterDrag = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.ExpressionRowHeight;
            var dragAccepted = durableAfterDrag == (int)Math.Round(previewHeight) &&
                RelativeVisuals(view.VoiceGrid).OfType<DataGridRow>().All(x => Math.Abs(x.ActualHeight - durableAfterDrag) < 1) &&
                vm.Rows.SequenceEqual(rows);

            view.ExpressionRowHeightBox.Text = "80"; Keyboard.Focus(view.ExpressionRowHeightBox); await Round3PressKey(Key.Enter); await Idle(); view.VoiceGrid.UpdateLayout();
            var numericAccepted = view.VoiceGrid.RowHeight == 80 &&
                new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.ExpressionRowHeight == 80;
            view.ExpressionRowHeightBox.Text = "70"; Keyboard.Focus(view.ExpressionRowHeightBox); await Round3PressKey(Key.Escape); await Idle();
            var escapeRestored = view.ExpressionRowHeightBox.Text == "80";
            view.ExpressionRowHeightBox.Text = "97"; Keyboard.Focus(view.ExpressionRowHeightBox); await Round3PressKey(Key.Enter); await Idle();
            var invalidRejected = view.ExpressionRowHeightBox.Text == "80" &&
                new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Presentation.ExpressionRowHeight == 80;
            view.ExpressionRowHeightBox.Text = "80"; Keyboard.Focus(view.ExpressionRowHeightBox); await Round3PressKey(Key.Enter); await Idle();

            Round4Assert(dragAccepted && numericAccepted && escapeRestored && invalidRejected && Signature(timeline) == before,
                "B18", "any Voice row boundary previews/resizes the one common height; numeric Enter applies, Esc restores and invalid input never persists");
            Round4Assert(serif.FontSize == fontSize, "B19", "row-height adjustment never shrinks the Serif font");
            Round4Assert(serif.TextWrapping == TextWrapping.Wrap && serif.TextTrimming == TextTrimming.CharacterEllipsis &&
                serif.ActualHeight > fontSize * 2 && view.VoiceGrid.RowHeight == 80 && Signature(timeline) == before,
                "B20", "larger bounded rows show multiple Serif lines without per-row auto-height or Timeline edits");
            SaveNamedView(view, "v042-round4-b-expression-height-360.png");
            view.PaletteTab.IsSelected = true; await Idle();
            SaveNamedView(view, "v042-round4-b-compact-palette-360.png");
            Round4Phase("B");
        }
        finally { vm.CloseExpressionTrialSession(); await vm.NavigationCompletion; }
    }
}
