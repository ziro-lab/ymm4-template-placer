using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class GenericLayerTargetPanel : UserControl
{
    public GenericLayerTargetPanel() => InitializeComponent();
    private void TargetKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PlacerViewModel root || e.Key is not (Key.Enter or Key.Escape)) return;
        // These keys belong to this numeric editor, not to the host's bubbling
        // Timeline commands. Invalid input stays editable; a held key never saves twice.
        e.Handled = true;
        if (e.IsRepeat) return;
        var command = e.Key == Key.Enter ? root.ApplyGenericLayerTargetCommand : root.ResetGenericLayerTargetCommand;
        if (command?.CanExecute(null) == true) command.Execute(null);
    }
}
