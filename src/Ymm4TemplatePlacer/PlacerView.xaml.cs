using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

public partial class PlacerView : UserControl
{
    public PlacerView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => RefreshExpressionDetail();
        VoiceGrid.SelectionChanged += (_, _) => RefreshExpressionDetail();
#if YMM4_PROOF
        NativeProof.View = this;
#endif
    }
    private void RefreshExpressionDetail() => ExpressionRowDetail.Visibility =
        ActualWidth < 520 && VoiceGrid.SelectedItem is AssignmentRow ? Visibility.Visible : Visibility.Collapsed;
}
