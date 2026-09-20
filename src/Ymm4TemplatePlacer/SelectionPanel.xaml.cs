using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ymm4TemplatePlacer;
public partial class SelectionPanel : UserControl
{
    public SelectionPanel() => InitializeComponent();
}
public sealed class PlacementPurposeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        SelectionProfile.TargetCompanion => "対象全体に合わせる",
        SelectionProfile.PointEmphasis => "一点を強調",
        SelectionProfile.SelectionRange => "選択範囲を覆う",
        SelectionProfile.Boundary => "2つの境界に置く",
        _ => ""
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
