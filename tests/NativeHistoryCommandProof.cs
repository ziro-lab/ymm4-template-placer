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

        Assert(panel.NativeUndoButton.Width >= 80 && panel.NativeRedoButton.Width >= 80 &&
            panel.NativeUndoButton.Height >= 46 && panel.NativeRedoButton.Height >= 46 &&
            panel.NativeUndoButton.Content?.ToString()?.Contains("戻す", StringComparison.Ordinal) == true &&
            panel.NativeRedoButton.Content?.ToString()?.Contains("やり直す", StringComparison.Ordinal) == true,
            "NATIVE_UNDO_REDO P0 persistent history controls are large, text-labeled primary actions instead of tiny icon buttons");

        Assert(panel.NativeUndoButton.ToolTip?.ToString()?.Contains("YMM4", StringComparison.Ordinal) == true &&
            panel.NativeRedoButton.ToolTip?.ToString()?.Contains("YMM4", StringComparison.Ordinal) == true,
            "NATIVE_UNDO_REDO P0 tooltips identify the commands as YMM4 history");

        Log("NATIVE_UNDO_REDO_P0=PASS");
    }
}
