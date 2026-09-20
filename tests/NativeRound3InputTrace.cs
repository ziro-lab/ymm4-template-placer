using System.Windows.Input;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    public static void TraceGenericLayerKey(KeyEventArgs e, object? context)
    {
        if (e.Key is Key.Enter or Key.Escape)
            Log($"R3-D numeric key route: key={e.Key}; handledOnArrival={e.Handled}; repeat={e.IsRepeat}; context={context?.GetType().FullName}; canApply={(context as PlacerViewModel)?.ApplyGenericLayerTargetCommand.CanExecute(null)}");
    }
}
