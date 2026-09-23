namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static void ShowTask(PlacerView view, string task)
    {
        var vm = (PlacerViewModel)view.DataContext;
        vm.ActivateIntentWorkspace();
        if (vm.IsAddingTemplate) throw new InvalidOperationException("Finish or cancel the explicit add task before switching test tasks.");
        if (task == "library") { vm.OpenTemplateManagementCommand.Execute(null); return; }
        vm.CloseTemplateManagementCommand.Execute(null);
        switch (task)
        {
            case "palette": view.PaletteTab.IsSelected = true; break;
            case "expression": view.ExpressionTab.IsSelected = true; break;
            case "selection": view.SelectionTab.IsSelected = true; break;
            default: throw new ArgumentOutOfRangeException(nameof(task));
        }
    }
    private static bool TaskIsVisible(PlacerView view, string task) => task switch
    {
        "library" => view.LibraryTaskSurface.IsVisible && !view.MainTabs.IsVisible,
        "palette" => view.MainTabs.IsVisible && view.PaletteTab.IsSelected,
        "expression" => view.MainTabs.IsVisible && view.ExpressionTab.IsSelected,
        "selection" => view.MainTabs.IsVisible && view.SelectionTab.IsSelected,
        _ => false
    };
}
