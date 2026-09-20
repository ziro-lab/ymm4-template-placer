using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private sealed class Round3RichTextEditor : Control { }
    public sealed class Round3EditValue { public string Text { get; set; } = "edit"; }
    private static async Task<Window> Round3FocusMain()
    {
        var host = Application.Current.Windows.Cast<Window>().Single(x => x.DataContext?.GetType().FullName == "YukkuriMovieMaker.ViewModels.MainViewModel");
        host.Activate(); Keyboard.Focus(host); await Idle();
        Assert(!View!.IsKeyboardFocusWithin && PaletteShortcutInputRouter.AdmitInput(Keyboard.FocusedElement as DependencyObject, false, false, false),
            "R3-B actual keyboard focus is inside the live YMM4 main window, outside the Plugin and text editors");
        return host;
    }
    private static async Task Round3PressKey(Key key, int repeats = 0)
    {
        var vk = checked((byte)KeyInterop.VirtualKeyFromKey(key));
        try
        {
            Round2Input.keybd_event(vk, 0, 0, UIntPtr.Zero); await Task.Delay(70);
            for (var i = 0; i < repeats; i++) { Round2Input.keybd_event(vk, 0, 0, UIntPtr.Zero); await Task.Delay(50); }
        }
        finally { Round2Input.keybd_event(vk, 0, 2, UIntPtr.Zero); }
        await Task.Delay(100); await Idle();
    }
    private static async Task VerifyHandsOnRound3Shortcuts(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-B fixed grid, position ownership and bounded native keys";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!; var surface = view.RelativePaletteSurface;
        var entries = Enumerable.Range(0, 5).Select(i => scope.AddTemplate("R3B/" + i, new TextItem { Length = 11 + i, Layer = 7 + i })).ToArray();
        var palette = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "位置", null, entries.Select(x => x.Id).ToList())
            { Layer = new() { UseTemplateLayer = false, Preferred = 7, Minimum = 4, Maximum = 40 } };
        var settings = PlacerSettingsStore.Copy(scope.Original); settings.Library = [.. entries]; settings.Palettes = [palette]; settings.IntentPalettes = [];
        settings.ExpressionBootstrapComplete = true; settings.ManualStylePaletteId = palette.Id; settings.ManualCharacterPaletteId = null;
        settings.Presentation = new();
        scope.Apply(settings, [], [], 100); await Idle();
        var panel = RelativeVisuals(surface.IntentTileItems).OfType<PaletteTilePanel>().Single();
        Round3Assert(panel.LayoutMode == PaletteLayoutMode.Auto && panel.ColumnCount == Math.Max(1, (int)(surface.PaletteTileScroll.ViewportWidth / 104)),
            "B1", "old/default Auto layout retains responsive 104-DIP cells");
        Round3Assert(!scope.Current.Presentation.ShortcutsEnabled && scope.Current.Presentation.PositionShortcuts.Count == 0 && !vm.TryExecutePositionShortcut(Key.F8, ModifierKeys.None),
            "B6", "fresh settings contain no enabled or inferred shortcuts");
        vm.BeginIntentSettings(); var draft = vm.IntentSettings!.Presentation;
        draft.LayoutMode = PaletteLayoutMode.Fixed; draft.FixedColumns = "4"; draft.ShortcutsEnabled = true;
        foreach (var (slot, key) in new[] { (0, "F8"), (4, "F9"), (200, "F10") })
        {
            draft.AddShortcutCommand.Execute(null); draft.Shortcuts.Last().Position = (slot + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            draft.Shortcuts.Last().Gesture = key;
        }
        vm.SaveIntentSettings(); await Idle();
        var fixedFour = panel.ColumnCount == 4;
        foreach (var width in new[] { 360d, 540d, 360d }) { view.Width = width; await Idle(); fixedFour &= panel.ColumnCount == 4; }
        var buttons = Round2TileButtons();
        var first = buttons[0].TranslatePoint(new Point(), panel); var fifth = buttons[4].TranslatePoint(new Point(), panel);
        Round3Assert(fixedFour && Math.Abs(first.X - fifth.X) < 0.1 && Math.Abs(fifth.Y - first.Y - 104) < 0.1,
            "B2", "four columns and row-major slot 4 geometry remain invariant across Tool resize");
        Round3Assert(surface.PaletteTileScroll.ExtentWidth >= 416 && surface.PaletteTileScroll.ViewportWidth < 416 && panel.ColumnCount == 4,
            "B3", "narrow Tool scrolls horizontally instead of changing fixed column count");
        var metadata = JsonSerializer.Serialize(scope.Current.Presentation.PositionShortcuts);
        using (var document = JsonDocument.Parse(metadata))
            Round3Assert(document.RootElement.EnumerateArray().All(x => x.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "Key", "Modifiers", "SlotIndex" })),
                "B4", "serialized shortcut metadata contains only SlotIndex/Key/Modifiers");
        var host = await Round3FocusMain(); var before = Signature(timeline);
        await Round3PressKey(Key.F8);
        Round3Assert(timeline.Items.Count == 1 && timeline.Items.Single().Length == 11 && timeline.Items.Single().Frame == 100,
            "B7", "real OS key executes slot 0 with focus in YMM4, not in the Plugin");
        var keyboardGeometry = timeline.Items.Select(x => (x.GetType(), x.Frame, x.Length, x.Layer)).ToArray();
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "R3-B keyboard placement is one native Undo");
        Window.GetWindow(view)!.Activate(); await Idle();
        await NativeRound2Click(await Round2TilePoint(Round2TileButtons()[0]));
        var sameGeometry = timeline.Items.Select(x => (x.GetType(), x.Frame, x.Length, x.Layer)).SequenceEqual(keyboardGeometry);
        await undo.UndoAsync(); await Idle();
        Round3Assert(sameGeometry && Signature(timeline) == before && Round2TileButtons()[0].Command == vm.ExecuteIntentTileCommand,
            "B14", "real keyboard and mouse use the same authoritative placement geometry/command/native Undo route");
        await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[1]);
        await Round3FocusMain(); await Round3PressKey(Key.F8);
        Round3Assert(timeline.Items.Count == 1 && timeline.Items.Single().Length == 12 && metadata == JsonSerializer.Serialize(scope.Current.Presentation.PositionShortcuts),
            "B5", "native drag changes the tile at slot 0, while its global key mapping remains byte-equivalent");
        await undo.UndoAsync(); await Idle();
        var repeatsObserved = 0; var downsObserved = 0; var upsObserved = 0;
        ProcessInputEventHandler observe = (_, e) =>
        {
            if (e.StagingItem.Input is not KeyEventArgs { Key: Key.F8 } key) return;
            if (key.RoutedEvent == Keyboard.PreviewKeyDownEvent) { downsObserved++; if (key.IsRepeat) repeatsObserved++; }
            if (key.RoutedEvent == Keyboard.PreviewKeyUpEvent) upsObserved++;
        };
        InputManager.Current.PostProcessInput += observe;
        try { await Round3PressKey(Key.F8, 3); }
        finally { InputManager.Current.PostProcessInput -= observe; }
        Log($"R3-B held-key native stream: down={downsObserved}, up={upsObserved}, repeat={repeatsObserved}, placed={timeline.Items.Count}");
        Round3Assert(downsObserved >= 4 && upsObserved == 1 && repeatsObserved > 0 && timeline.Items.Count == 1, "B12", "actual repeated key-down events produce exactly one placement, despite multiple free layers");
        await undo.UndoAsync(); await Idle();
        var focused = Keyboard.FocusedElement as DependencyObject;
        bool Route(Key key, bool repeat = false, bool modal = false, bool menu = false) => view.ShortcutRouter.ProcessKey(key, ModifierKeys.None, focused, repeat, modal, menu);
        var visible = view.Visibility; view.Visibility = Visibility.Collapsed; await Idle();
        Round3Assert(!view.ShortcutRouter.IsAttached && !Route(Key.F8) && Signature(timeline) == before, "B8", "hidden Tool detaches input and cannot steal or execute a key");
        view.Visibility = visible; view.PaletteTab.IsSelected = true; await Idle(); await Round3FocusMain();
        view.SelectionTab.IsSelected = true; await Idle();
        Round3Assert(!view.ShortcutRouter.IsAttached && !Route(Key.F8), "B9", "Settings is not a shortcut placement surface");
        view.PaletteTab.IsSelected = true; await Idle();
        var presentation = scope.Current.Presentation;
        scope.Current.Presentation = presentation with { LayoutMode = PaletteLayoutMode.Auto };
        Round3Assert(!Route(Key.F8) && Signature(timeline) == before, "B10", "Auto mode leaves even explicitly configured position keys unhandled");
        scope.Current.Presentation = presentation;
        var excluded = new DependencyObject[] { new TextBox(), new RichTextBox(), new PasswordBox(), new ComboBox(), new Round3RichTextEditor(), new MenuItem() };
        var excludes = excluded.All(PaletteShortcutInputRouter.IsEditableOrMenuSource) && !Route(Key.F8, modal: true) && !Route(Key.F8, menu: true);
        var grid = new DataGrid { ItemsSource = new[] { new Round3EditValue() }, AutoGenerateColumns = true, CanUserAddRows = false };
        var editing = new Window { Owner = host, Content = grid, Width = 260, Height = 180, Title = "R3 isolated editing fixture" };
        try
        {
            editing.Show(); await Idle(); grid.SelectedIndex = 0; grid.CurrentCell = new DataGridCellInfo(grid.Items[0], grid.Columns[0]);
            grid.BeginEdit(); await Idle();
            excludes &= RelativeVisuals(grid).OfType<DataGridCell>().Any(x => x.IsEditing) &&
                PaletteShortcutInputRouter.IsEditableOrMenuSource(Keyboard.FocusedElement as DependencyObject);
        }
        finally { editing.Close(); }
        var modalObserved = false;
        var modalWindow = new Window { Owner = host, Width = 200, Height = 120, Content = new Button { Content = "modal" } };
        _ = Application.Current.Dispatcher.BeginInvoke(new Action(() => { modalObserved = ComponentDispatcher.IsThreadModal && !Route(Key.F8, modal: ComponentDispatcher.IsThreadModal); modalWindow.Close(); }));
        modalWindow.ShowDialog();
        Window.GetWindow(view)!.Activate(); await Idle();
        var menu = await Round2TileMenu(Round2TileButtons()[0]);
        excludes &= InputManager.Current.IsInMenuMode && !Route(Key.F8, menu: InputManager.Current.IsInMenuMode); menu.IsOpen = false; await Idle();
        Round3Assert(excludes && modalObserved && Signature(timeline) == before, "B11", "live DataGrid editing, real modal/menu modes and standard/custom text controls are excluded");
        await Round3FocusMain(); focused = Keyboard.FocusedElement as DependencyObject;
        var missingLeavesUnhandled = !Route(Key.F10) && !Route(Key.F11);
        var unavailableTemplate = scope.Templates.Single(x => x.Name == "R3B/1");
        YukkuriMovieMaker.Settings.ItemSettings.Default.Templates.Remove(unavailableTemplate); vm.RefreshIntentWorkspace(); await Idle();
        missingLeavesUnhandled &= !Route(Key.F8) && !vm.IntentTiles[0].Available;
        YukkuriMovieMaker.Settings.ItemSettings.Default.Templates.Add(unavailableTemplate); vm.RefreshIntentWorkspace(); await Idle();
        Round3Assert(missingLeavesUnhandled && Signature(timeline) == before, "B13", "missing, unassigned and unavailable slots return unhandled with zero mutation");
        // A rejected preflight has exactly the same zero-write semantics for mouse and keys.
        timeline.Items = [new TextItem { Frame = 100, Length = 100, Layer = 7 }]; undo.Record();
        var current = scope.Current; var oldPalette = current.Palettes.Single();
        current.Palettes[0] = oldPalette with { Layer = new() { UseTemplateLayer = false, Preferred = 7, Minimum = 7, Maximum = 7 } }; vm.RefreshIntentWorkspace();
        var blocked = Signature(timeline); Route(Key.F8);
        Assert(vm.HasError && Signature(timeline) == blocked, "R3-B shortcut preflight failure is zero-write");
        vm.ExecuteIntentTileCommand.Execute(vm.IntentTiles[0]); Assert(vm.HasError && Signature(timeline) == blocked, "R3-B mouse preflight fails identically");
        current.Palettes[0] = oldPalette; timeline.Items = []; undo.Record(); vm.Refresh(); vm.ResetIntentSettings();
        for (var i = 0; i < 3; i++)
        {
            view.Visibility = Visibility.Collapsed; view.Visibility = visible;
            view.DataContext = null; view.DataContext = vm; await Idle();
        }
        view.PaletteTab.IsSelected = true; await Idle(); await Round3FocusMain(); await Round3PressKey(Key.F8);
        Round3Assert(view.ShortcutRouter.IsAttached && timeline.Items.Count == 1 && timeline.Items.Single().Length == 12,
            "B15", "hide/reopen/DataContext transitions leave exactly one active key handler");
        await undo.UndoAsync(); await Idle();
        Round3Phase("B", "hands-on-round3-shortcuts.json"); Log("HANDS_ON_ROUND3_B=PASS");
    }
}
