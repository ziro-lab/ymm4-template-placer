using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

// Only collects text. The root coordinator validates currency and performs the protected save.
public sealed class IntentTileAliasDialog : Window
{
    public TextBox AliasBox { get; }
    public Button AcceptButton { get; }
    public Button CancelButton { get; }
    public IntentTileAliasDialog(string alias)
    {
        Title = "タイルの表示名"; Width = 350; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false;
        SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
        SetResourceReference(ForegroundProperty, SystemColors.WindowTextBrushKey);
        var content = new StackPanel { Margin = new Thickness(16) };
        content.Children.Add(new TextBlock { Text = "このSetの表示名だけを変更します。元テンプレート名は変わりません。空欄なら短い名前に戻ります。", TextWrapping = TextWrapping.Wrap });
        AliasBox = new TextBox { Text = alias, MaxLength = 128, Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(6) };
        content.Children.Add(AliasBox);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        AcceptButton = new Button { Content = "保存", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0) };
        CancelButton = new Button { Content = "キャンセル", IsCancel = true, Padding = new Thickness(14, 5, 14, 5) };
        AcceptButton.Click += (_, _) => DialogResult = true;
        buttons.Children.Add(AcceptButton); buttons.Children.Add(CancelButton); content.Children.Add(buttons); Content = content;
        Loaded += (_, _) => { AliasBox.Focus(); AliasBox.SelectAll(); };
    }
}
