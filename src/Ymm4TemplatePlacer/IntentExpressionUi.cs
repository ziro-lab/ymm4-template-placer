using System.ComponentModel;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    private bool relativeExpressionBindingsInstalled;
    public void AttachRelativeExpressionBindings()
    {
        if (!relativeExpressionBindingsInstalled)
        {
            relativeExpressionBindingsInstalled = true;
            var legacy = AddExpressionTemplateCommand;
            AddExpressionTemplateCommand = new ActionCommand(x => IsTemplateExpressionSource && settingsAvailable && x is AssignmentRow,
                x => { if (UsesRelativeExpressions) Guard(() => OpenRelativeExpressionSettings((AssignmentRow)x!)); else legacy.Execute(x); });
            OnPropertyChanged(nameof(AddExpressionTemplateCommand));
            PropertyChanged += RelativeExpressionSettingsChanged;
        }
        MarkExpressionVocabularyDirty();
    }
    private void RelativeExpressionSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A fresh settings session is published after Save/Discard, not on each keystroke.
        if (e.PropertyName == nameof(IntentSettings) && UsesRelativeExpressions && IntentSettings?.HasChanges == false)
            MarkExpressionVocabularyDirty();
    }
    private void OpenRelativeExpressionSettings(AssignmentRow row)
    {
        if (!Rows.Contains(row) || !RequireTimeline().Items.Contains(row.Target.Voice) || row.Character != row.Target.Voice.CharacterName)
            throw new InvalidOperationException("「表情をまとめて」の音声が変わりました。メンテナンスから一覧を読み直してください。");
        BeginIntentSettings();
        var session = IntentSettings!;
        var palette = session.Palettes.FirstOrDefault(x => x.ExpressionCandidates && x.CharacterName == row.Character);
        if (palette == null)
        {
            session.Create([row.Target.Voice]); palette = session.SelectedPalette!;
            palette.Name = row.Character; palette.Intent = "表情"; palette.ExpressionCandidates = true;
            palette.Duration = IntentDuration.UntilRelated; palette.Neighbor = IntentNeighbor.NextSameTypeAndCharacter;
        }
        session.SelectedPalette = palette;
        IntentSettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
