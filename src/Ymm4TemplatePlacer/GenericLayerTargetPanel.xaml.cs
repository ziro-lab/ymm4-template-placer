using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class GenericLayerTargetPanel : UserControl
{
    public GenericLayerTargetPanel()
    {
        InitializeComponent();
        // Listen only on our own numeric editor. Some host/text class handlers mark
        // Enter handled before instance handlers; this does not install a global hook.
        GenericTargetBox.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(TargetKeyDown), true);
    }
    private void TargetKeyDown(object sender, KeyEventArgs e)
    {
#if YMM4_PROOF
        NativeProof.TraceGenericLayerKey(e, DataContext);
#endif
        if (DataContext is not PlacerViewModel root || e.Key is not (Key.Enter or Key.Escape)) return;
        e.Handled = true;
        if (e.IsRepeat) return;
        var command = e.Key == Key.Enter ? root.ApplyGenericLayerTargetCommand : root.ResetGenericLayerTargetCommand;
        if (command?.CanExecute(null) == true) command.Execute(null);
    }
}
