using System.Windows.Controls;
using System.Windows.Input;
namespace Ymm4TemplatePlacer;
public partial class IntentSettingsPanel : UserControl
{
    public IntentSettingsPanel()
    {
        InitializeComponent();
        NestedWheelRouting.SetEnabled(SettingsScroll, true);
        PreviewMouseWheel += OnPanelPreviewMouseWheel;
        Loaded += (_, _) => (DataContext as PlacerViewModel)?.BeginIntentSettings();
        DataContextChanged += (_, _) => { if (IsLoaded) (DataContext as PlacerViewModel)?.BeginIntentSettings(); };
    }

    private void OnPanelPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        if (NestedWheelRouting.TryScrollFromHost(this, SettingsScroll, e.Delta, Keyboard.Modifiers)) e.Handled = true;
    }
}
