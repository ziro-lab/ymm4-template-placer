using System.Collections.ObjectModel;
using System.ComponentModel;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private bool refreshingPresets, loadingPresetDraft;
    private ExpressionPreset? loadedExpressionPreset;
    public PresetDraft ExpressionDraft { get; } = new();
    public ObservableCollection<ExpressionPreset> ExpressionPresets { get; } = [];
    public IReadOnlyList<ExpressionDurationChoice> ExpressionDurations { get; } =
        [new(ExpressionDuration.VoiceSpan, "Voiceと同じ"), new(ExpressionDuration.NextSameCharacter, "次の同Character Voiceまで")];
    private ExpressionPreset CurrentExpressionPreset => settings.ExpressionPresets.Single(x => x.Id == settings.CurrentExpressionPresetId);
    public ExpressionPreset? SelectedExpressionPreset
    {
        get => CurrentExpressionPreset;
        set
        {
            if (refreshingPresets || value == null || value.Id == settings.CurrentExpressionPresetId) return;
            Guard(() =>
            {
                if (ExpressionPresetDirty) throw new InvalidOperationException("編集中のPresetを保存するか［編集を戻す］を押してから切り替えてください。");
                EditSettings(next => next.CurrentExpressionPresetId = value.Id);
            });
            OnPropertyChanged(nameof(SelectedExpressionPreset));
        }
    }
    public bool ExpressionPresetDirty => !ExpressionDraft.Matches(CurrentExpressionPreset);
    public string ExpressionPresetSummary => CurrentExpressionPreset.Describe();
    public string ExpressionPresetNotice => ExpressionPresetDirty ? "Presetは未保存です。保存するまで配置しません。" :
        "配置・Excel読込後の配置には、このPresetを使います。時間の単位はframeです。";
    public ActionCommand SaveExpressionPresetCommand { get; private set; } = null!;
    public ActionCommand CopyExpressionPresetCommand { get; private set; } = null!;
    public ActionCommand DeleteExpressionPresetCommand { get; private set; } = null!;
    public ActionCommand RevertExpressionPresetCommand { get; private set; } = null!;

    partial void InitializePresets()
    {
        SaveExpressionPresetCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(SaveExpressionPreset));
        CopyExpressionPresetCommand = new ActionCommand(_ => settingsAvailable && !ExpressionPresetDirty, _ => Guard(CopyExpressionPreset));
        DeleteExpressionPresetCommand = new ActionCommand(_ => settingsAvailable && settings.ExpressionPresets.Count > 1 && !ExpressionPresetDirty, _ => Guard(DeleteExpressionPreset));
        RevertExpressionPresetCommand = new ActionCommand(_ => true, _ => LoadExpressionDraft());
        ExpressionDraft.PropertyChanged += ExpressionDraftChanged;
        RefreshPresets();
    }
    partial void RefreshPresets()
    {
        refreshingPresets = true;
        try
        {
            ExpressionPresets.Clear();
            foreach (var preset in settings.ExpressionPresets) ExpressionPresets.Add(preset);
            if (loadedExpressionPreset != CurrentExpressionPreset) LoadExpressionDraft();
            OnPropertyChanged(nameof(SelectedExpressionPreset));
            OnPropertyChanged(nameof(ExpressionPresetSummary));
            foreach (var row in Rows) row.PreferPalette(settings);
            ExpressionDraftChanged(null, new PropertyChangedEventArgs(null));
        }
        finally { refreshingPresets = false; }
    }
    private void LoadExpressionDraft()
    {
        loadingPresetDraft = true;
        try { loadedExpressionPreset = CurrentExpressionPreset; ExpressionDraft.Load(CurrentExpressionPreset); }
        finally { loadingPresetDraft = false; }
        ExpressionDraftChanged(null, new PropertyChangedEventArgs(null));
    }
    private void ExpressionDraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (loadingPresetDraft) return;
        OnPropertyChanged(nameof(ExpressionPresetDirty)); OnPropertyChanged(nameof(ExpressionPresetNotice));
        UpdateCommands();
        SaveExpressionPresetCommand?.RaiseCanExecuteChanged(); CopyExpressionPresetCommand?.RaiseCanExecuteChanged();
        DeleteExpressionPresetCommand?.RaiseCanExecuteChanged(); RevertExpressionPresetCommand?.RaiseCanExecuteChanged();
    }
    private ExpressionPreset RequireExpressionPreset()
    {
        if (!settingsAvailable) throw new InvalidOperationException(LibraryNotice);
        if (ExpressionPresetDirty) throw new InvalidOperationException("編集中の表情Presetを保存してから配置してください。");
        return CurrentExpressionPreset;
    }
    public void SaveExpressionPreset()
    {
        var preset = ExpressionDraft.Read(CurrentExpressionPreset.Id);
        EditSettings(next => next.ExpressionPresets[next.ExpressionPresets.FindIndex(x => x.Id == preset.Id)] = preset);
        LoadExpressionDraft();
        HasError = false; Status = $"表情Preset「{preset.Name}」を保存しました。Timelineは変更していません。";
    }
    public void CopyExpressionPreset()
    {
        var current = RequireExpressionPreset();
        var stem = current.Name.Length > 100 ? current.Name[..100] : current.Name;
        var number = 1; string name;
        do { name = $"{stem}（コピー {number++}）"; } while (settings.ExpressionPresets.Any(x => x.Name == name));
        var copy = current with { Id = Guid.NewGuid(), Name = name };
        EditSettings(next => { next.ExpressionPresets.Add(copy); next.CurrentExpressionPresetId = copy.Id; });
        HasError = false; Status = "Presetを複製しました。名前や配置条件を編集して保存してください。";
    }
    public void DeleteExpressionPreset()
    {
        var id = RequireExpressionPreset().Id;
        if (settings.ExpressionPresets.Count <= 1) throw new InvalidOperationException("最後の表情Presetは削除できません。");
        EditSettings(next => { next.ExpressionPresets.RemoveAll(x => x.Id == id); next.CurrentExpressionPresetId = next.ExpressionPresets[0].Id; });
        HasError = false; Status = "Presetを削除しました。既存のTimeline Itemは変更していません。";
    }
    partial void DisposeV04() => ExpressionDraft.PropertyChanged -= ExpressionDraftChanged;
}
