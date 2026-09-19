using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;

public enum PaletteLayoutMode { Auto, Fixed }
public enum ExpressionViewportFollow { Off, WhenOutside, Always }
// This is a global position binding. No Template/LibraryEntry/IntentEntry identity belongs here.
public sealed record PositionShortcut(int SlotIndex, Key Key, ModifierKeys Modifiers = ModifierKeys.None)
{
    internal static bool SupportedKey(Key key) => key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9 or
        >= Key.NumPad0 and <= Key.Divide or >= Key.F1 and <= Key.F24 or Key.Space or Key.Home or Key.End or Key.PageUp or Key.PageDown;
    internal void Validate()
    {
        if (SlotIndex is < 0 or >= 256 || !SupportedKey(Key) || (Modifiers & ~(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) != 0)
            throw new InvalidDataException("位置ショートカットの位置・キーが不正です。");
    }
    [JsonIgnore] public string Gesture => (Modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "") +
        (Modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "") + (Modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "") +
        (Key is >= Key.D0 and <= Key.D9 ? ((int)Key - (int)Key.D0).ToString(System.Globalization.CultureInfo.InvariantCulture) : Key.ToString());
    internal static PositionShortcut Parse(int slot, string gesture)
    {
        var parts = gesture.Trim().Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 4) throw new InvalidOperationException("キーは A、Ctrl+1、Shift+F1 のように入力してください。");
        var modifiers = ModifierKeys.None;
        foreach (var part in parts[..^1])
        {
            var modifier = part.ToUpperInvariant() switch { "CTRL" => ModifierKeys.Control, "ALT" => ModifierKeys.Alt, "SHIFT" => ModifierKeys.Shift,
                _ => throw new InvalidOperationException("修飾キーは Ctrl・Alt・Shift を指定してください。") };
            if ((modifiers & modifier) != 0) throw new InvalidOperationException("修飾キーが重複しています。");
            modifiers |= modifier;
        }
        var text = parts[^1];
        if (text.Length == 1 && text[0] is >= '0' and <= '9') text = "D" + text;
        if (!Enum.GetNames<Key>().Contains(text, StringComparer.OrdinalIgnoreCase) || !Enum.TryParse<Key>(text, true, out var key) || !SupportedKey(key))
            throw new InvalidOperationException("英字・数字・F1～F24などのキーを指定してください。");
        var result = new PositionShortcut(slot, key, modifiers); result.Validate(); return result;
    }
}
public sealed record PalettePresentationSettings
{
    public PaletteLayoutMode LayoutMode { get; init; } = PaletteLayoutMode.Auto;
    public int FixedColumns { get; init; } = 4;
    public bool ShortcutsEnabled { get; init; }
    public List<PositionShortcut> PositionShortcuts { get; init; } = [];
    public ExpressionViewportFollow ViewportFollow { get; init; } = ExpressionViewportFollow.WhenOutside;
    public void Validate()
    {
        if (!Enum.IsDefined(LayoutMode) || FixedColumns is < 1 or > 16 || !Enum.IsDefined(ViewportFollow) ||
            PositionShortcuts == null || PositionShortcuts.Count > 64 || PositionShortcuts.Any(x => x == null))
            throw new InvalidDataException("配置パレットの表示・操作設定が不正です。");
        foreach (var binding in PositionShortcuts) binding.Validate();
        if (PositionShortcuts.Select(x => x.SlotIndex).Distinct().Count() != PositionShortcuts.Count ||
            PositionShortcuts.Select(x => (x.Key, x.Modifiers)).Distinct().Count() != PositionShortcuts.Count)
            throw new InvalidDataException("ショートカットの位置またはキーが重複しています。");
    }
}
public sealed partial class PlacerSettings
{
    public PalettePresentationSettings Presentation { get; set; } = new();
}
