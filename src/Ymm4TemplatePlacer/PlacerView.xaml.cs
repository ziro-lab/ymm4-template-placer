using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

public partial class PlacerView : UserControl
{
    public PlacerView()
    {
        InitializeComponent();
#if YMM4_PROOF
        NativeProof.View = this;
#endif
    }
}
