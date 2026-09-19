using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class GenericLayerTargetPanel : UserControl
{
    public GenericLayerTargetPanel()
    {
        InitializeComponent();
        GenericTargetBox.PreviewKeyDown += TargetKeyDown;
    }
    private void TargetKeyDown(object sender, KeyEventArgs e)
    {
#if YMM4_PROOF
        NativeProof.TraceGenericLayerKey(e, DataContext);
#endif
        if (DataContext is not PlacerViewModel root || e.Key is not (Key.Enter or Key.Escape)) return;
        // Editor-local commit/reset, never placement. The native host can report
        // IsRepeat on the first delivered Return. Successful apply/reset creates a
        // clean draft, so command admission itself prevents repeated saves.
        e.Handled = true;
        var command = e.Key == Key.Enter ? root.ApplyGenericLayerTargetCommand : root.ResetGenericLayerTargetCommand;
        if (command?.CanExecute(null) == true) command.Execute(null);
    }
}
