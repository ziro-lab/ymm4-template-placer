using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    public PaletteLayoutMode PaletteLayout => settings.Presentation.LayoutMode;
    public int PaletteFixedColumns => settings.Presentation.FixedColumns;
    internal bool TryExecutePositionShortcut(Key key, ModifierKeys modifiers)
    {
        var presentation = settings.Presentation;
        if (activeTask != "palette" || !HasIntentTimeline || !ReferenceEquals(intentTimeline, timeline) || UseLegacyWorkspace ||
            !presentation.ShortcutsEnabled || presentation.LayoutMode != PaletteLayoutMode.Fixed) return false;
        var binding = presentation.PositionShortcuts.SingleOrDefault(x => x.Key == key && x.Modifiers == modifiers);
        if (binding == null || binding.SlotIndex < 0 || binding.SlotIndex >= IntentTiles.Count) return false;
        // Resolve the current position now, never retain a tile identity in input metadata.
        var tile = IntentTiles[binding.SlotIndex];
        if (!tile.Available || ExecuteIntentTileCommand?.CanExecute(tile) != true) return false;
        ExecuteIntentTileCommand.Execute(tile); return true;
    }
}
// Same bounded application-input lifetime as the context pointer router. No OS hook,
// no registered global hotkey, no planner here. The View owns attach/detach; the root
// resolves position and executes the exact existing mouse command.
internal sealed class PaletteShortcutInputRouter(Func<Key, ModifierKeys, bool> execute) : IDisposable
{
    private InputManager? manager;
    public bool IsAttached => manager != null;
    internal static bool IsEditableOrMenuSource(DependencyObject? source)
    {
        var depth = 0;
        for (var current = source; current != null && depth++ < 64; current = TimelinePointerIntentClassifier.Parent(current))
        {
            if (current is TextBoxBase or PasswordBox or ComboBox or MenuBase or MenuItem or HwndHost ||
                current is DataGridCell { IsEditing: true } or DataGridRow { IsEditing: true }) return true;
            // Custom editor shells may own a text service without deriving TextBoxBase.
            var name = current.GetType().Name;
            if (name.Contains("Editor", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Input", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
    internal static bool AdmitInput(DependencyObject? focused, bool isRepeat, bool modal, bool menuMode)
    {
        if (focused == null || isRepeat || modal || menuMode || IsEditableOrMenuSource(focused) || IsEditableOrMenuSource(Mouse.Captured as DependencyObject)) return false;
        var window = Window.GetWindow(focused);
        var context = window?.DataContext?.GetType();
        return window is { IsActive: true, IsEnabled: true } && context is { FullName: "YukkuriMovieMaker.ViewModels.MainViewModel" } && context.Assembly == typeof(Timeline).Assembly;
    }
    public void Attach()
    {
        if (manager != null) return;
        manager = InputManager.Current; manager.PreProcessInput += BeforeInput;
    }
    public void Detach()
    {
        if (manager == null) return;
        manager.PreProcessInput -= BeforeInput; manager = null;
    }
    private void BeforeInput(object sender, PreProcessInputEventArgs e)
    {
        if (e.StagingItem.Input is not KeyEventArgs key || key.RoutedEvent != Keyboard.PreviewKeyDownEvent || key.Handled || manager == null) return;
        var value = key.Key == Key.System ? key.SystemKey : key.Key;
        if (ProcessKey(value, key.KeyboardDevice.Modifiers, key.KeyboardDevice.FocusedElement as DependencyObject,
            key.IsRepeat, ComponentDispatcher.IsThreadModal, manager.IsInMenuMode)) key.Handled = true;
    }
    internal bool ProcessKey(Key key, ModifierKeys modifiers, DependencyObject? focused, bool repeat, bool modal, bool menu) =>
        IsAttached && AdmitInput(focused, repeat, modal, menu) && execute(key, modifiers);
    public void Dispose() => Detach();
}
