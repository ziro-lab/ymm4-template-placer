using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

public partial class PanelQuickSettingsPanel : UserControl
{
    public PanelQuickSettingsPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => SelectDefaultPage();
        DataContextChanged += (_, _) => { if (IsLoaded) SelectDefaultPage(); };
    }

    internal void SelectDefaultPage()
    {
        QuickSettingsTabs.SelectedItem = DataContext is PlacerViewModel { HasPlacementQuickDraft: true }
            ? PlacementQuickTab
            : PresentationQuickTab;
    }
}
