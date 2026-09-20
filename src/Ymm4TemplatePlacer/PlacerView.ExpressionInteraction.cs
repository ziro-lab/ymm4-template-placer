using System.Windows.Controls;

namespace Ymm4TemplatePlacer;
public partial class PlacerView
{
    // The grid's selected row is local presentation state. All host navigation
    // still goes upward through exactly the same authoritative root command.
    private void ExpressionChoiceOpened(object? sender, EventArgs e)
    {
        if (sender is not ComboBox { DataContext: AssignmentRow row } ||
            observedViewModel is not { } vm || !vm.NavigateExpressionRowCommand.CanExecute(row)) return;
        VoiceGrid.SelectedItem = row;
        vm.NavigateExpressionRowCommand.Execute(row);
    }
}
