using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class GenericLayerTargetPanel : UserControl
{
    public GenericLayerTargetPanel() => InitializeComponent();
    private void TargetKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PlacerViewModel root || e.IsRepeat) return;
        var command = e.Key == Key.Enter ? root.ApplyGenericLayerTargetCommand : e.Key == Key.Escape ? root.ResetGenericLayerTargetCommand : null;
        if (command?.CanExecute(null) != true) return;
        command.Execute(null); e.Handled = true;
    }
}
