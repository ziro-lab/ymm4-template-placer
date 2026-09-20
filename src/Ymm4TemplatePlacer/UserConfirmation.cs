using System.Windows;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
#if YMM4_PROOF
    internal Func<string, bool>? ConfirmationOverride { get; set; }
#endif

    private bool ConfirmUserAction(string message)
    {
#if YMM4_PROOF
        if (ConfirmationOverride != null) return ConfirmationOverride(message);
#endif
        return MessageBox.Show(message, Title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
    }

    private void DeleteCurrentPaletteFromUi()
    {
        var palette = CurrentPalette ?? throw new InvalidOperationException("パレットを選んでください。");
        var message = $"「{palette.Name}」を削除します。\nこのパレットには{palette.LibraryEntryIds.Count}件のテンプレートがあります。\n元テンプレート、テンプレート管理の登録、タイムラインは削除されません。";
        if (!ConfirmUserAction(message)) return;
        DeleteCurrentPalette();
    }

    private void UnregisterLibraryFromUi()
    {
        var entry = SelectedLibraryEntry?.Entry ?? throw new InvalidOperationException("登録解除するテンプレートを選んでください。");
        var affected = settings.Palettes.Count(x => x.LibraryEntryIds.Contains(entry.Id));
        var message = $"「{entry.DisplayName}」を登録解除します。\nこの登録は{affected}個のパレットからも外れます。\n元のYMM4テンプレートとタイムラインは削除されません。";
        if (!ConfirmUserAction(message)) return;
        UnregisterLibrary();
    }
}
