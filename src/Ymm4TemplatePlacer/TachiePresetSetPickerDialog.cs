using System.Windows;
using System.Windows.Controls;

namespace Ymm4TemplatePlacer;

internal sealed record TachiePresetRegistrationTarget(Guid Id, string Label);

internal sealed class TachiePresetSetPickerDialog : Window
{
    private readonly ComboBox picker;
    public Guid? SelectedPaletteId =>
        picker.SelectedItem is TachiePresetRegistrationTarget target ? target.Id : null;

    public TachiePresetSetPickerDialog(IReadOnlyList<TachiePresetRegistrationTarget> targets)
    {
        if (targets.Count < 2)
            throw new ArgumentException("登録先Setの選択肢は2件以上必要です。", nameof(targets));

        Title = "表情プリセットの登録先";
        Width = 380;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        var root = new StackPanel { Margin = new Thickness(14) };
        root.Children.Add(new TextBlock
        {
            Text = "この表情プリセットを追加するSetを選んでください。",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        picker = new ComboBox
        {
            ItemsSource = targets,
            DisplayMemberPath = nameof(TachiePresetRegistrationTarget.Label),
            SelectedIndex = 0,
            MinWidth = 320,
            Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(picker);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var cancel = new Button
        {
            Content = "キャンセル",
            IsCancel = true,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var accept = new Button
        {
            Content = "登録",
            IsDefault = true,
            Padding = new Thickness(14, 5, 14, 5)
        };
        accept.Click += (_, _) => { DialogResult = true; Close(); };
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        root.Children.Add(buttons);
        Content = root;
    }
}
