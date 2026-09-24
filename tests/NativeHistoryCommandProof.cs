using System.Windows.Input;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyNativeHistoryCommandSurface()
    {
        stage = "NATIVE_UNDO_REDO P0 standard command surface";

        var vm = ViewModel ?? throw new InvalidOperationException("Native ViewModel is not attached.");
        var view = View ?? throw new InvalidOperationException("Native View is not attached.");
        var panel = view.RelativePaletteSurface;

        Assert(CommandSettings.Default[CommandType.Undo] is RoutedCommand &&
            CommandSettings.Default[CommandType.Redo] is RoutedCommand,
            "NATIVE_UNDO_REDO P0 YMM4 configured Undo/Redo resolve as routed standard commands");

        Assert(ReferenceEquals(panel.NativeUndoButton.Command, vm.NativeUndoCommand) &&
            ReferenceEquals(panel.NativeRedoButton.Command, vm.NativeRedoCommand),
            "NATIVE_UNDO_REDO P0 placement header buttons use the product native-history bridge");

        Assert(panel.NativeUndoButton.Width == 30 && panel.NativeRedoButton.Width == 30 &&
            panel.NativeUndoButton.Height == 26 && panel.NativeRedoButton.Height == 26,
            "NATIVE_UNDO_REDO P0 controls stay compact and do not require a taller placement header");

        Assert(panel.NativeUndoButton.ToolTip?.ToString()?.Contains("YMM4", StringComparison.Ordinal) == true &&
            panel.NativeRedoButton.ToolTip?.ToString()?.Contains("YMM4", StringComparison.Ordinal) == true,
            "NATIVE_UNDO_REDO P0 tooltips identify the commands as YMM4 history");

        Log("NATIVE_UNDO_REDO_P0=PASS");
    }
}
