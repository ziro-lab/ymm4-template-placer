using System.Windows;
using System.Windows.Media;

namespace Ymm4TemplatePlacer;

public static class IntentTileAppearance
{
    public static string Label(IntentEntry entry, LibraryEntry? source)
    {
        if (!string.IsNullOrWhiteSpace(entry.DisplayAlias)) return entry.DisplayAlias.Trim();
        var name = source?.Source.Name ?? "参照切れ";
        return ShortName(name);
    }
    public static string ShortName(string name) =>
        name.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? name;
    public static IReadOnlyList<string> Distinguish(IReadOnlyList<string> labels)
    {
        var counts = labels.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var reserved = labels.ToHashSet(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>(labels.Count);
        foreach (var stem in labels)
        {
            var label = stem;
            if (counts[stem] > 1)
            {
                var number = 1;
                do { label = $"{stem} ({number++})"; } while (reserved.Contains(label) || used.Contains(label));
            }
            used.Add(label); result.Add(label);
        }
        return result;
    }
    public static string ShapeName(IntentTileShape shape) => shape switch
    {
        IntentTileShape.Square => "四角", IntentTileShape.Circle => "丸", _ => "角丸"
    };
    public static CornerRadius Radius(IntentTileShape shape) => new(shape switch
    {
        IntentTileShape.Square => 0, IntentTileShape.Circle => 52, _ => 8
    });
    public static string ColorName(IntentTileColor color) => color switch
    {
        IntentTileColor.Rose => "ピンク", IntentTileColor.Amber => "黄", IntentTileColor.Green => "緑",
        IntentTileColor.Blue => "青", IntentTileColor.Violet => "紫", _ => "標準（色なし）"
    };
    // Finite color is represented on the face and its full border. Text remains a
    // system foreground; tint is blended against the current system background.
    public static Brush Face(IntentTileColor color) => FaceForTheme(color, SystemParameters.HighContrast, SystemColors.ControlColor);
    internal static Brush FaceForTheme(IntentTileColor color, bool highContrast, Color background)
    {
        if (highContrast || color == IntentTileColor.Neutral) return SystemColors.ControlBrush;
        if (Accent(color) is not SolidColorBrush accent) return SystemColors.ControlBrush;
        static byte Tint(byte background, byte color) => (byte)((background * 4 + color) / 5);
        var brush = new SolidColorBrush(Color.FromRgb(Tint(background.R, accent.Color.R), Tint(background.G, accent.Color.G), Tint(background.B, accent.Color.B)));
        brush.Freeze(); return brush;
    }
    public static Brush Accent(IntentTileColor color)
    {
        if (SystemParameters.HighContrast) return SystemColors.ControlTextBrush;
        var value = color switch
        {
            IntentTileColor.Rose => 0xC04B72, IntentTileColor.Amber => 0xA87817,
            IntentTileColor.Green => 0x39805A, IntentTileColor.Blue => 0x397BB0,
            IntentTileColor.Violet => 0x8057AD, _ => -1
        };
        if (value < 0) return SystemColors.ControlDarkBrush;
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value));
        brush.Freeze(); return brush;
    }
}
