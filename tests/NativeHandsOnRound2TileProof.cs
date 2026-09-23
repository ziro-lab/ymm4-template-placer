using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static Button[] Round2TileButtons() => RelativeVisuals(View!.RelativePaletteSurface.IntentTileItems).OfType<Button>().Where(x => x.CommandParameter is IntentTileChoice).ToArray();
    private static async Task<Point> Round2TilePoint(Button button)
    {
        button.BringIntoView(); await Idle();
        var point = button.PointToScreen(new Point(button.ActualWidth / 2, button.ActualHeight / 2));
        Assert(Round2Input.SetCursorPos((int)Math.Round(point.X), (int)Math.Round(point.Y)), "R2-D OS cursor reaches the tile candidate");
        await Task.Delay(90); await Idle();
        Assert(Round2Input.GetCursorPos(out var actual) && Math.Abs(actual.X - point.X) <= 1 && Math.Abs(actual.Y - point.Y) <= 1 &&
            Mouse.DirectlyOver is DependencyObject over && (ReferenceEquals(over, button) || button.IsAncestorOf(over) ||
                (!button.IsEnabled && VisualTreeHelper.GetParent(button) is Grid cell && (ReferenceEquals(over, cell) || cell.IsAncestorOf(over)))) ,
            "R2-D physical device hit is the intended live Button/cell, not an occluding window or stale coordinate");
        return point;
    }
    private static async Task Round2TileDrag(Button from, Button to, bool cancel = false, bool withinSameTile = false)
    {
        var end = await Round2TilePoint(to); var start = await Round2TilePoint(from);
        if (withinSameTile) end = new Point(start.X + 20, start.Y + 10);
        Round2Input.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        // The OS producer must remain independent of WPF's modal DoDragDrop loop.
        await Task.Run(() =>
        {
            try
            {
                Thread.Sleep(100);
                for (var i = 1; i <= 5; i++)
                {
                    Round2Input.SetCursorPos((int)Math.Round(start.X + (end.X - start.X) * i / 5), (int)Math.Round(start.Y + (end.Y - start.Y) * i / 5)); Thread.Sleep(70);
                }
                if (cancel) { Round2Input.keybd_event(0x1B, 0, 0, UIntPtr.Zero); Thread.Sleep(70); Round2Input.keybd_event(0x1B, 0, 2, UIntPtr.Zero); }
            }
            finally { Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); }
        });
        await Task.Delay(120); await Idle();
    }
    private static async Task<ContextMenu> Round2TileMenu(Button button)
    {
        await Round2TilePoint(button);
        Round2Input.mouse_event(0x0008, 0, 0, 0, UIntPtr.Zero); await Task.Delay(50);
        Round2Input.mouse_event(0x0010, 0, 0, 0, UIntPtr.Zero); await Task.Delay(100); await Idle();
        var menu = ((Grid)VisualTreeHelper.GetParent(button)).ContextMenu;
        Assert(menu?.IsOpen == true && menu.Items.OfType<MenuItem>().Count() == 4, "R2-D real right-click opens finite alias/color/shape/Set actions");
        return menu!;
    }
    private static async Task Round2MenuInvoke(MenuItem item)
    {
        Assert(item.IsEnabled && item.Command != null, "R2-D native menu action is wired and executable");
        var peer = new MenuItemAutomationPeer(item);
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
    }
    private static async Task Round2Rename(IntentTileChoice tile, string alias, bool accept)
    {
        Exception? error = null; var exercised = false;
        _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            IntentTileAliasDialog? dialog = null;
            try
            {
                dialog = Application.Current.Windows.OfType<IntentTileAliasDialog>().Single(); dialog.AliasBox.Text = alias;
                var peer = new ButtonAutomationPeer(accept ? dialog.AcceptButton : dialog.CancelButton);
                ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke(); exercised = true;
            }
            catch (Exception ex) { error = ex; if (dialog != null) dialog.DialogResult = false; }
        }));
        ViewModel!.RenameIntentTileCommand.Execute(tile); await Idle();
        if (error != null) throw new InvalidOperationException("R2-D alias dialog exercise failed", error);
        Assert(exercised, "R2-D native alias dialog is exercised through its Save/Cancel button");
    }
    private static async Task VerifyHandsOnRound2Tiles(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R2-D whole-tile gestures and additive appearance";
        var vm = ViewModel!; var view = View!; var surface = view.RelativePaletteSurface;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var items = timeline.Items; var selection = timeline.SelectedItems; var frame = timeline.CurrentFrame;
        var legacy = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var window = Window.GetWindow(view)!; var windowWidth = window.Width; var windowHeight = window.Height;
        var windowLeft = window.Left; var windowTop = window.Top; var windowState = window.WindowState;
        var voice = new VoiceItem(new Character { Name = "Round2 Tiles" }) { Frame = 100, Length = 30, Layer = 20 };
        var a = Template("R2D/A", [new TextItem { Length = 11, Layer = 7 }]);
        var b = Template("R2D/B", [new TextItem { Length = 13, Layer = 8 }]);
        ItemSettings.Default.Templates.Add(a); ItemSettings.Default.Templates.Add(b);
        var clickCount = 0;
        RoutedEventHandler clicks = (_, e) => { if (e.OriginalSource is Button { CommandParameter: IntentTileChoice }) clickCount++; };
        surface.AddHandler(Button.ClickEvent, clicks, true);
        try
        {
            var ra = TemplateResolver.Reference(a, a.Name, null); var rb = TemplateResolver.Reference(b, b.Name, null);
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))] };
            var set = new IntentPalette(Guid.NewGuid(), "タイル", "legacy", target, new(), [new(ra.Id), new(rb.Id)]);
            var generic = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "汎用", null, [ra.Id, rb.Id]);
            var fixture = PlacerSettingsStore.Copy(original); fixture.Library = [ra, rb]; fixture.IntentPalettes = [set]; fixture.Palettes = [generic];
            fixture.ManualStylePaletteId = generic.Id; fixture.ManualCharacterPaletteId = null; fixture.LegacyWorkspace = false;
            fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true;
            field.SetValue(vm, fixture); timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.CurrentFrame = 10; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(false); vm.ActivateIntentWorkspace(); vm.Refresh(); vm.ResetIntentSettings(); view.PaletteTab.IsSelected = true;
            window.WindowState = System.Windows.WindowState.Normal; window.Left = SystemParameters.WorkArea.Left + 8; window.Top = SystemParameters.WorkArea.Top + 8;
            window.Width = Math.Min(600, SystemParameters.WorkArea.Width - 16); window.Height = Math.Min(600, SystemParameters.WorkArea.Height - 16);
            view.Width = 360; view.Height = 360; window.Activate(); await Task.Delay(120); await Idle();
            var signature = Signature(timeline);
            Assert(Round2TileButtons().Length == 2 && !RelativeVisuals(surface).OfType<FrameworkElement>().Any(x => x.Name == "TileDragHandle"),
                "R2-D E1/E2 the real Button occupies each whole tile; the obsolete narrow handle is absent");
            var firstPoint = await Round2TilePoint(Round2TileButtons()[0]); await NativeRound2Click(firstPoint);
            Assert(clickCount == 1 && timeline.Items.Count == 2 && timeline.Items.Single(x => x != voice).Frame == voice.Frame,
                "R2-D E1 real short left-click places once through the existing targeted Button command");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == signature, "R2-D whole-tile click remains one native Undo");
            await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[1]);
            var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            Assert(clickCount == 1 && Signature(timeline) == signature && saved.IntentPalettes.Single().Entries.Select(x => x.LibraryEntryId).SequenceEqual(new[] { rb.Id, ra.Id }),
                "R2-D E2 actual whole-tile OS drag changes authoritative targeted order and never fires placement Click");
            var orderBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
            await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[0], withinSameTile: true);
            Assert(clickCount == 1 && Signature(timeline) == signature && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(orderBytes),
                "R2-D E3 a self-drop past the WPF threshold is neither placement nor a redundant save");
            await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[1], cancel: true);
            Assert(clickCount == 1 && Signature(timeline) == signature && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(orderBytes),
                "R2-D E3 Escape cancels the real modal drag without placement or order mutation");
            vm.IntentSettings!.Palettes.Single().Name = "未保存";
            await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[1]);
            Assert(clickCount == 1 && Signature(timeline) == signature && File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(orderBytes),
                "R2-D E9 a refused dirty-settings drag still suppresses placement and preserves the draft");
            vm.ResetIntentSettings();
            var menu = await Round2TileMenu(Round2TileButtons()[0]);
            Assert(clickCount == 1 && Signature(timeline) == signature, "R2-D E4 right-click never places a tile");
            var shapeMenu = menu.Items.OfType<MenuItem>().Single(x => x.Header?.ToString() == "形"); shapeMenu.IsSubmenuOpen = true; await Idle();
            Assert(shapeMenu.Items.OfType<MenuItem>().Select(x => x.Header?.ToString()).SequenceEqual(new[] { "角丸", "四角", "丸" }),
                "R2-D E5 only the three bounded shapes are offered");
            var stale = vm.IntentTiles[0]; await Round2MenuInvoke(shapeMenu.Items.OfType<MenuItem>().Single(x => x.Header?.ToString() == "丸")); menu.IsOpen = false; await Idle();
            menu = await Round2TileMenu(Round2TileButtons()[0]);
            var colorMenu = menu.Items.OfType<MenuItem>().Single(x => x.Header?.ToString() == "色"); colorMenu.IsSubmenuOpen = true; await Idle();
            await Round2MenuInvoke(colorMenu.Items.OfType<MenuItem>().Single(x => x.Header?.ToString() == "青")); menu.IsOpen = false; await Idle();
            Assert(vm.IntentTiles[0].Shape == IntentTileShape.Circle && vm.IntentTiles[0].Color == IntentTileColor.Blue && vm.IntentTiles[0].Radius.TopLeft > 40 && Signature(timeline) == signature,
                "R2-D E6/E8 menu shape/color persist independently of placement semantics and source identity");
            Assert(!vm.ShapeIntentTileCommand.CanExecute(new IntentTileShapeRequest(stale, IntentTileShape.Square)),
                "R2-D E9 a stale menu/drag token cannot edit a refreshed Set");
            await Round2Rename(vm.IntentTiles[0], "短い別名", true);
            Assert(vm.IntentTiles[0].Label == "短い別名" && b.Name == "R2D/B", "R2-D E6 alias changes only this membership label, never the live Template name");
            var aliasBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath); await Round2Rename(vm.IntentTiles[0], "破棄される名前", false);
            Assert(File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(aliasBytes), "R2-D native alias Cancel is zero-write");
            vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer(); await Idle();
            var genericTile = vm.IntentTiles[0]; vm.ChangeIntentTileAppearance(genericTile, x => x with { DisplayAlias = "汎用A", Color = IntentTileColor.Green, Shape = IntentTileShape.Square }); await Idle();
            await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[1]);
            saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load(); var persistedGeneric = saved.Palettes.Single();
            Assert(persistedGeneric.LibraryEntryIds.SequenceEqual(new[] { rb.Id, ra.Id }) && persistedGeneric.AppearanceFor(ra.Id).DisplayAlias == "汎用A" &&
                persistedGeneric.AppearanceFor(ra.Id).Shape == IntentTileShape.Square && persistedGeneric.Layer == generic.Layer && saved.IntentPalettes.Single().Relation == set.Relation &&
                saved.IntentPalettes.Single().Entries.Single(x => x.LibraryEntryId == rb.Id).DisplayAlias == "短い別名" && timeline.SelectedItems.Contains(voice) && Signature(timeline) == signature,
                "R2-D E7/E8 Generic OS drag preserves the membership-keyed optional appearance map and targeted data; original IDs remain the only order authority");
            var session = new IntentSettingsSession(saved, new[] { typeof(VoiceItem) }, [voice]);
            Assert(JsonSerializer.Serialize(session.Build()) == JsonSerializer.Serialize(saved), "R2-D targeted and Generic appearance survive staged Settings reconstruction without schema migration");
            var priorBytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
            RejectWithoutMutation(timeline, () => vm.ChangeIntentTileAppearance(vm.IntentTiles[0], x => x with { Shape = (IntentTileShape)999 }), "R2-D unknown shape fails before any settings/Timeline write");
            Assert(File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(priorBytes), "R2-D invalid appearance retains exact settings bytes");
            var node = JsonNode.Parse(JsonSerializer.Serialize(fixture))!;
            foreach (var palette in node["IntentPalettes"]!.AsArray()) foreach (var entry in palette!["Entries"]!.AsArray()) entry!.AsObject().Remove("Shape");
            foreach (var palette in node["Palettes"]!.AsArray()) palette!.AsObject().Remove("TileAppearance");
            var oldPath = Path.Combine(output, "round2-old-appearance.json"); File.WriteAllText(oldPath, node.ToJsonString()); var oldBytes = File.ReadAllBytes(oldPath);
            var old = new PlacerSettingsStore(oldPath).Load();
            Assert(old.IntentPalettes.All(x => x.Entries.All(e => e.Shape == IntentTileShape.Rounded)) && old.Palettes.All(x => x.TileAppearance == null) && File.ReadAllBytes(oldPath).SequenceEqual(oldBytes),
                "R2-D E10 old missing appearance fields default safely and load never rewrites bytes");
            vm.IntentSettings!.Palettes.Single().LayerOffset = "未確定"; var sameDraft = vm.IntentSettings!;
            vm.OpenTileSettingsCommand.Execute(vm.IntentTiles[0]); await Idle();
            Assert(ReferenceEquals(sameDraft, vm.IntentSettings) && sameDraft.HasChanges && sameDraft.IsGenericContext && sameDraft.SelectedGenericSet?.SelectedEntry != null &&
                File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(priorBytes) && Signature(timeline) == signature,
                "R2-D E4/E9 opening exact Set settings preserves an unrelated invalid draft and performs no save/placement");
            vm.ResetIntentSettings(); view.SelectionTab.IsSelected = true; view.Height = 440;
            var settingsPanel = view.RelativeSettingsSurface;
            // Phase 4 removes the first-level Source/Set-management disclosure. Retain the compact-layout gate against the visible surface.
            Assert(settingsPanel.SourceEditor.IsVisible,
                "R2-D/C bulk source registration stays discoverable without a first-level disclosure");
            settingsPanel.SettingsScroll.ScrollToTop(); await Idle();
            Assert(settingsPanel.PalettePicker.ActualWidth > 150 && settingsPanel.SettingsScroll.ViewportHeight > 140 &&
                settingsPanel.RollbackButton.TranslatePoint(new Point(0, settingsPanel.RollbackButton.ActualHeight), view).Y <= view.ActualHeight,
                "R2-D/C narrow Settings uses compact direct targets and a full-width Set picker, retaining a usable scrolling viewport and visible rollback");
            SaveNamedView(view, "v042-round2-settings-compact.png");
            view.PaletteTab.IsSelected = true; view.Height = 360; await Idle(); SaveNamedView(view, "v042-round2-tiles-360.png");
            Assert(clickCount == 1, "R2-D all tested drag/edit/context operations generated no extra placement click");
            File.WriteAllText(Path.Combine(output, "hands-on-round2-tiles.json"), JsonSerializer.Serialize(new {
                schema = "YMM4-Template-Placer-Round2-Tiles/1", host = "YMM4 4.55.1.1 Lite", result = "PASS", input = "OS mouse drag/right click; native menu/dialog controls", checks = Enumerable.Range(1, 10).Select(x => new { id = $"E{x}", result = "PASS" }) }));
            Log("HANDS_ON_ROUND2_D=PASS");
        }
        finally
        {
            Round2Input.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); surface.RemoveHandler(Button.ClickEvent, clicks);
            if (disk == null) File.Delete(PlacerSettingsStore.DefaultPath); else File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk);
            store.Load(); ItemSettings.Default.Templates.Remove(a); ItemSettings.Default.Templates.Remove(b);
            field.SetValue(vm, original); timeline.Items = items; timeline.SelectedItems = selection; timeline.CurrentFrame = frame; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            view.Width = width; view.Height = height; window.WindowState = System.Windows.WindowState.Normal; window.Width = windowWidth; window.Height = windowHeight; window.Left = windowLeft; window.Top = windowTop; window.WindowState = windowState;
            vm.SetLegacyWorkspace(legacy); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
}
