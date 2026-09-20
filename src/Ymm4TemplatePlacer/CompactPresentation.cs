using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    private ActionCommand? openCurrentSetSettingsCommand, setSettingsShapeCommand;
    public ActionCommand OpenCurrentSetSettingsCommand => openCurrentSetSettingsCommand ??= new(_ => true, _ => Guard(() =>
    {
        if (selectedIntentSet is { } set && IsCurrentIntentSet(set)) OpenSettingsForSet(set);
        else IntentSettingsRequested?.Invoke(this, EventArgs.Empty);
    }));
    public ActionCommand SetSettingsShapeCommand => setSettingsShapeCommand ??= new(
        x => settingsAvailable && IntentSettings?.HasSelectedSet == true && x is IntentTileShape shape && Enum.IsDefined(shape),
        x => Guard(() =>
        {
            var session = IntentSettings ?? throw new InvalidOperationException("設定を開いてください。");
            var entries = session.IsGenericContext ? session.SelectedGenericSet?.Entries : session.SelectedPalette?.Entries;
            if (entries == null) return;
            foreach (var entry in entries) entry.Shape = (IntentTileShape)x!;
        }));
    public IReadOnlyList<int> ExpressionRowHeights { get; } = [32, 36, 48, 64, 80, 96];
    public int ExpressionRowHeight
    {
        get => settings.Presentation.ExpressionRowHeight;
        set
        {
            if (value == ExpressionRowHeight) return;
            Guard(() =>
            {
                if (!CanEditExpressionRowHeight) throw new InvalidOperationException("設定の入力を確定または戻してから、行の高さを変更してください。");
                var next = PlacerSettingsStore.Copy(settings);
                next.Presentation = next.Presentation with { ExpressionRowHeight = value };
                settingsStore.Save(next); settings = next;
                // Rows and the current Voice are deliberately not reconstructed by a presentation edit.
                ResetIntentSettings(); OnPropertyChanged(nameof(ExpressionRowHeight));
            });
            OnPropertyChanged(nameof(ExpressionRowHeight));
        }
    }
    public bool CanEditExpressionRowHeight => settingsAvailable && IntentSettings?.HasChanges != true;
    internal bool TrySetExpressionRowHeight(string text)
    {
        if (!int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value) ||
            value is < 32 or > 96)
        {
            HasError = true;
            Status = "行の高さは32～96の整数で指定してください。";
            return false;
        }
        ExpressionRowHeight = value;
        var accepted = ExpressionRowHeight == value;
        if (accepted && HasError && Status == "行の高さは32～96の整数で指定してください。")
        {
            HasError = false;
            Status = "";
        }
        return accepted;
    }
}
