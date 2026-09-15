using System.Windows.Controls;
namespace Ymm4TemplatePlacer;
public partial class IntentSettingsPanel : UserControl
{
    public IntentSettingsPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => (DataContext as PlacerViewModel)?.BeginIntentSettings();
        DataContextChanged += (_, _) => { if (IsLoaded) (DataContext as PlacerViewModel)?.BeginIntentSettings(); };
    }
}
