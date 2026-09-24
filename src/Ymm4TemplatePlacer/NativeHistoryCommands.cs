using System.Windows;
using System.Windows.Input;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private UndoRedoManager? nativeHistoryManager;
    private EventHandler? nativeHistoryChangedHandler;
    private EventHandler? nativeHistoryRequeryHandler;

    public ActionCommand NativeUndoCommand { get; private set; } = null!;
    public ActionCommand NativeRedoCommand { get; private set; } = null!;

    private void InitializeNativeHistoryCommands()
    {
        NativeUndoCommand = new ActionCommand(
            _ => CanExecuteNativeHistory(CommandType.Undo),
            _ => Guard(() => ExecuteNativeHistory(CommandType.Undo)));
        NativeRedoCommand = new ActionCommand(
            _ => CanExecuteNativeHistory(CommandType.Redo),
            _ => Guard(() => ExecuteNativeHistory(CommandType.Redo)));

        nativeHistoryChangedHandler = (_, _) => RaiseNativeHistoryCommandState();
        nativeHistoryRequeryHandler = (_, _) => RaiseNativeHistoryCommandState();
        CommandManager.RequerySuggested += nativeHistoryRequeryHandler;
    }

    private void BindNativeHistoryCommands(UndoRedoManager? manager)
    {
        if (ReferenceEquals(nativeHistoryManager, manager))
        {
            RaiseNativeHistoryCommandState();
            return;
        }

        if (nativeHistoryManager != null && nativeHistoryChangedHandler != null)
        {
            nativeHistoryManager.Recorded -= nativeHistoryChangedHandler;
            nativeHistoryManager.Undoed -= nativeHistoryChangedHandler;
            nativeHistoryManager.Redoed -= nativeHistoryChangedHandler;
        }

        nativeHistoryManager = manager;

        if (nativeHistoryManager != null && nativeHistoryChangedHandler != null)
        {
            nativeHistoryManager.Recorded += nativeHistoryChangedHandler;
            nativeHistoryManager.Undoed += nativeHistoryChangedHandler;
            nativeHistoryManager.Redoed += nativeHistoryChangedHandler;
        }

        RaiseNativeHistoryCommandState();
    }

    private bool CanExecuteNativeHistory(CommandType type)
    {
        if (nativeHistoryManager == null)
            return false;

        try
        {
            return CommandSettings.Default[type] is RoutedCommand command
                && Application.Current?.MainWindow is { } target
                && command.CanExecute(null, target);
        }
        catch
        {
            return false;
        }
    }

    private void ExecuteNativeHistory(CommandType type)
    {
        if (CommandSettings.Default[type] is not RoutedCommand command ||
            Application.Current?.MainWindow is not { } target ||
            !command.CanExecute(null, target))
            throw new InvalidOperationException(type == CommandType.Undo
                ? "現在は戻せる操作がありません。"
                : "現在はやり直せる操作がありません。");

        // Execute YMM4's configured standard command against the YMM4 main window.
        // Template Placer deliberately owns no separate history stack.
        command.Execute(null, target);
        RaiseNativeHistoryCommandState();
    }

    private void RaiseNativeHistoryCommandState()
    {
        NativeUndoCommand?.RaiseCanExecuteChanged();
        NativeRedoCommand?.RaiseCanExecuteChanged();
    }

    private void DisposeNativeHistoryCommands()
    {
        BindNativeHistoryCommands(null);
        if (nativeHistoryRequeryHandler != null)
            CommandManager.RequerySuggested -= nativeHistoryRequeryHandler;
        nativeHistoryRequeryHandler = null;
        nativeHistoryChangedHandler = null;
    }
}
