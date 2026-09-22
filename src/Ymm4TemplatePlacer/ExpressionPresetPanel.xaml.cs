using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ymm4TemplatePlacer;

public partial class ExpressionPresetPanel : UserControl
{
    public ExpressionPresetPanel() => InitializeComponent();
}

public sealed class ExpressionRangeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ExpressionPreset preset) return "配置範囲";
        var range = preset.Duration == ExpressionDuration.VoiceSpan ? "音声と同じ" : "次の同キャラの音声まで";
        return "配置範囲: " + range + (preset.StartOffset != 0 || preset.EndOffset != 0 ? "（位置調整あり）" : "");
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ExpressionLayerModeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ExpressionLayerMode mode &&
        Enum.TryParse<ExpressionLayerMode>(parameter?.ToString(), out var expected) &&
        mode == expected ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
