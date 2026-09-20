using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ymm4TemplatePlacer;
public partial class GenericLayerTargetPanel : UserControl
{
    private int wheelRemainder;
    private Guid? wheelSet;
    public GenericLayerTargetPanel()
    {
        InitializeComponent();
        GenericTargetBox.PreviewKeyDown += TargetKeyDown;
        GenericOccupiedPicker.PreviewKeyDown += TargetKeyDown;
        GenericTargetBox.PreviewMouseWheel += TargetWheel;
        GenericOccupiedPicker.DropDownClosed += (_, _) =>
        {
            if (IsLoaded && DataContext is PlacerViewModel root && root.ApplyGenericLayerTargetCommand?.CanExecute(null) == true)
                root.ApplyGenericLayerTargetCommand.Execute(null);
        };
        DataContextChanged += (_, _) => wheelRemainder = 0;
        Unloaded += (_, _) => wheelRemainder = 0;
        GenericTargetBox.MouseLeave += (_, _) => wheelRemainder = 0;
    }
    internal void FocusDirectNumberEntry()
    {
        GenericTargetBox.Focus();
        GenericTargetBox.CaretIndex = GenericTargetBox.Text?.Length ?? 0;
        GenericTargetBox.Select(0, 0);
    }
    private void TargetKeyDown(object sender, KeyEventArgs e)
    {
#if YMM4_PROOF
        NativeProof.TraceGenericLayerKey(e, DataContext);
#endif
        if (DataContext is not PlacerViewModel root || e.Key is not (Key.Enter or Key.Escape)) return;
        e.Handled = true;
        var command = e.Key == Key.Enter ? root.ApplyGenericLayerTargetCommand : root.ResetGenericLayerTargetCommand;
        if (command?.CanExecute(null) == true) command.Execute(null);
    }
    private void TargetWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None || !IsVisible || !GenericTargetBox.IsEnabled ||
            DataContext is not PlacerViewModel root || GenericTargetBox.DataContext is not GenericLayerTargetDraft draft ||
            !ReferenceEquals(root.GenericLayerTarget, draft)) return;
        // Field-local input only. Preserve partial high-resolution wheel deltas,
        // but never turn an incomplete number into a guessed saved target.
        e.Handled = true;
        if (wheelSet != draft.SetId) { wheelSet = draft.SetId; wheelRemainder = 0; }
        wheelRemainder += e.Delta;
        var steps = wheelRemainder / 120; wheelRemainder %= 120;
        if (steps != 0) root.StepGenericLayer(draft, steps);
    }
}
