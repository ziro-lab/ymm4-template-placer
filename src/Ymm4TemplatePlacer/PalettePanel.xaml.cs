using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace Ymm4TemplatePlacer;
public partial class PalettePanel : UserControl
{
    public QuickDropControls DropSurface { get; } = new();
    public PalettePanel()
    {
        InitializeComponent(); DropControls.Content = DropSurface;
        PaletteEditor.Expanded += (_, _) => DropSurface.LayerEditor.IsExpanded = false;
        DropSurface.LayerEditor.Expanded += (_, _) => PaletteEditor.IsExpanded = false;
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new EventSetter(Control.MouseDoubleClickEvent, new MouseButtonEventHandler(OnPaletteEntryDoubleClick)));
        PaletteList.ItemContainerStyle = style;
    }
    private void OnPaletteEntryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || sender is not ListBoxItem container || container.DataContext is not PaletteEntryView entry || DataContext is not PlacerViewModel vm) return;
        vm.SelectedPaletteEntry = entry;
        if (vm.QuickDropCommand.CanExecute(null)) vm.QuickDropCommand.Execute(null);
        e.Handled = true;
    }
}
