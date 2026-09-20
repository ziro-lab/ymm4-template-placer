using System.Windows.Controls;

namespace Ymm4TemplatePlacer;
public partial class PlacerView
{
    // Opening the chooser is a Voice operation. Forward only; the root owns
    // CurrentFrame, selection, the async seek worker and viewport policy.
    private void ExpressionChoiceOpened(object? sender, EventArgs e)
    {
        if (sender is ComboBox { DataContext: AssignmentRow row } &&
            observedViewModel is { } vm && vm.NavigateExpressionRowCommand.CanExecute(row))
            vm.NavigateExpressionRowCommand.Execute(row);
    }
}
