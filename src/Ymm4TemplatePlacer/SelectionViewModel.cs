using System.Collections.ObjectModel;
using System.ComponentModel;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private bool refreshingSelection, loadingSelectionDraft;
    private SelectionPreset? loadedSelectionPreset;
    private LibraryEntryView? selectionTemplate;
    private string selectionPreview = "配置予定は［予定を確認］で表示します。";
    public SelectionPresetDraft SelectionDraft { get; } = new();
    public ObservableCollection<SelectionProfileChoice> SelectionProfiles { get; } = [];
    public ObservableCollection<SelectionPreset> SelectionPresets { get; } = [];
    public ObservableCollection<LibraryEntryView> SelectionTemplates { get; } = [];
    public IReadOnlyList<AnchorChoice> SelectionAnchors { get; } =
        [new(0, "開始"), new(25, "25%"), new(50, "50%"), new(75, "75%"), new(100, "終了（末尾の次frame）")];
    private SelectionPreset CurrentSelectionPreset => settings.SelectionPresets.Single(x => x.Id == settings.CurrentSelectionPresetId);
    public SelectionProfileChoice? SelectedSelectionProfile
    {
        get => SelectionProfiles.FirstOrDefault(x => x.Value == CurrentSelectionPreset.Profile);
        set
        {
            if (refreshingSelection || value == null || value.Value == CurrentSelectionPreset.Profile) return;
            Guard(() => { RequireCleanSelectionDraft(); EditSettings(next => next.CurrentSelectionPresetId = next.SelectionPresets.First(x => x.Profile == value.Value).Id); });
            OnPropertyChanged(nameof(SelectedSelectionProfile));
        }
    }
    public SelectionPreset? SelectedSelectionPreset
    {
        get => CurrentSelectionPreset;
        set
        {
            if (refreshingSelection || value == null || value.Id == settings.CurrentSelectionPresetId) return;
            Guard(() => { RequireCleanSelectionDraft(); EditSettings(next => next.CurrentSelectionPresetId = value.Id); });
            OnPropertyChanged(nameof(SelectedSelectionPreset));
        }
    }
    public LibraryEntryView? SelectionTemplate
    {
        get => selectionTemplate;
        set { Set(ref selectionTemplate, value); InvalidateSelectionPreview(); RaiseSelectionCommands(); }
    }
    public bool SelectionPresetDirty => !SelectionDraft.Matches(CurrentSelectionPreset);
    public bool IsCompanionProfile => CurrentSelectionPreset.Profile == SelectionProfile.TargetCompanion;
    public bool IsPointProfile => CurrentSelectionPreset.Profile == SelectionProfile.PointEmphasis;
    public string SelectionContext => timeline == null ? "対象Sceneを開いてください。" :
        timeline.SelectedItems.Count == 0 ? "YMM4のTimelineで対象Itemを選択してください。表情一覧の行選択は対象外です。" :
        $"Timelineで{timeline.SelectedItems.Count}件選択中。" + (SelectionProfiles.Count == 0 ? "この選択に使えるProfileはありません。" : "関係・Preset・Library Templateを選んで配置します。");
    public string SelectionPresetNotice => SelectionPresetDirty ? "Presetは未保存です。保存または［編集を戻す］を選んでください。" :
        "単位はframe。選択配置は追加のみ・関連付けなしです。再同期は表情一覧から関連付けた表情が対象です。";
    public string SelectionPreview { get => selectionPreview; private set => Set(ref selectionPreview, value); }
    public ActionCommand PreviewSelectionCommand { get; private set; } = null!;
    public ActionCommand PlaceSelectionCommand { get; private set; } = null!;
    public ActionCommand SaveSelectionPresetCommand { get; private set; } = null!;
    public ActionCommand CopySelectionPresetCommand { get; private set; } = null!;
    public ActionCommand DeleteSelectionPresetCommand { get; private set; } = null!;
    public ActionCommand RevertSelectionPresetCommand { get; private set; } = null!;

    private void InitializeSelectionPresets()
    {
        PreviewSelectionCommand = new ActionCommand(_ => CanPlaceSelection(), _ => Guard(() => PreviewSelection()));
        PlaceSelectionCommand = new ActionCommand(_ => undo != null && CanPlaceSelection(), _ => Guard(() => PlaceSelection()));
        SaveSelectionPresetCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(SaveSelectionPreset));
        CopySelectionPresetCommand = new ActionCommand(_ => settingsAvailable && !SelectionPresetDirty, _ => Guard(CopySelectionPreset));
        DeleteSelectionPresetCommand = new ActionCommand(_ => settingsAvailable && !SelectionPresetDirty && settings.SelectionPresets.Count(x => x.Profile == CurrentSelectionPreset.Profile) > 1, _ => Guard(DeleteSelectionPreset));
        RevertSelectionPresetCommand = new ActionCommand(_ => true, _ => LoadSelectionDraft());
        SelectionDraft.PropertyChanged += SelectionDraftChanged;
        RefreshSelectionPresets();
    }
    private void RefreshSelectionPresets()
    {
        if (refreshingSelection) return;
        refreshingSelection = true;
        try
        {
            var id = selectionTemplate?.Id;
            SelectionTemplates.Clear();
            foreach (var entry in settings.Library) SelectionTemplates.Add(new(entry));
            SelectionTemplate = SelectionTemplates.FirstOrDefault(x => x.Id == id);
            SelectionPresets.Clear();
            foreach (var preset in settings.SelectionPresets.Where(x => x.Profile == CurrentSelectionPreset.Profile)) SelectionPresets.Add(preset);
            if (loadedSelectionPreset != CurrentSelectionPreset) LoadSelectionDraft();
            OnPropertyChanged(nameof(SelectedSelectionPreset)); OnPropertyChanged(nameof(IsCompanionProfile)); OnPropertyChanged(nameof(IsPointProfile));
            RefreshSelectionProfiles();
        }
        finally { refreshingSelection = false; }
    }
    partial void RefreshSelectionProfiles()
    {
        var old = refreshingSelection; refreshingSelection = true;
        try
        {
            SelectionProfiles.Clear();
            foreach (var profile in SelectionPlacement.Profiles(timeline?.SelectedItems.Count ?? 0)) SelectionProfiles.Add(profile);
            OnPropertyChanged(nameof(SelectedSelectionProfile)); OnPropertyChanged(nameof(SelectionContext));
            InvalidateSelectionPreview(); RaiseSelectionCommands();
        }
        finally { refreshingSelection = old; }
    }
    private void LoadSelectionDraft()
    {
        loadingSelectionDraft = true;
        try { loadedSelectionPreset = CurrentSelectionPreset; SelectionDraft.Load(CurrentSelectionPreset); }
        finally { loadingSelectionDraft = false; }
        SelectionDraftChanged(null, new PropertyChangedEventArgs(null));
    }
    private void SelectionDraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (loadingSelectionDraft) return;
        OnPropertyChanged(nameof(SelectionPresetDirty)); OnPropertyChanged(nameof(SelectionPresetNotice));
        InvalidateSelectionPreview(); RaiseSelectionCommands();
    }
    private void InvalidateSelectionPreview() => SelectionPreview = "［予定を確認］で再確認してください。配置時には最新のTimelineで再計算します。";
    private void RaiseSelectionCommands()
    {
        PreviewSelectionCommand?.RaiseCanExecuteChanged(); PlaceSelectionCommand?.RaiseCanExecuteChanged();
        SaveSelectionPresetCommand?.RaiseCanExecuteChanged(); CopySelectionPresetCommand?.RaiseCanExecuteChanged();
        DeleteSelectionPresetCommand?.RaiseCanExecuteChanged();
    }
    private bool CanPlaceSelection() => timeline != null && settingsAvailable && !SelectionPresetDirty && selectionTemplate != null &&
        SelectionPlacement.Supports(CurrentSelectionPreset.Profile, timeline.SelectedItems.Count);
    private void RequireCleanSelectionDraft()
    {
        if (!settingsAvailable) throw new InvalidOperationException(LibraryNotice);
        if (SelectionPresetDirty) throw new InvalidOperationException("選択配置Presetを保存するか［編集を戻す］を押してください。");
    }
    private SelectionPlacement PlanSelection()
    {
        RequireCleanSelectionDraft();
        var entry = selectionTemplate?.Entry ?? throw new InvalidOperationException("Library登録から配置するTemplateを選んでください。");
        if (!settings.Library.Contains(entry)) throw new InvalidOperationException("Library登録が変更されています。選び直してください。");
        return SelectionPlacement.Create(RequireTimeline(), entry, CurrentSelectionPreset);
    }
    public SelectionPlacement PreviewSelection()
    {
        var planned = PlanSelection(); HasError = false;
        SelectionPreview = $"予定: Frame {planned.Item.Frame} / Length {planned.Item.Length} / Layer {planned.Item.Layer}。配置時に再計算します。";
        Status = "配置予定を確認しました。Timelineは変更していません。"; return planned;
    }
    public int PlaceSelection()
    {
        if (undo == null) throw new InvalidOperationException("YMM4のUndoに接続できません。");
        var planned = PlanSelection(); var count = planned.Plan.Commit(RequireTimeline(), undo);
        HasError = false; SelectionPreview = "配置済み。続けて配置する場合は予定を再確認してください。";
        Status = $"「{selectionTemplate!.DisplayName}」をPreset「{CurrentSelectionPreset.Name}」で追加: Frame {planned.Item.Frame} / Length {planned.Item.Length} / Layer {planned.Item.Layer}。Undo 1回で戻せます。";
        return count;
    }
    public void SaveSelectionPreset()
    {
        var preset = SelectionDraft.Read(CurrentSelectionPreset);
        EditSettings(next => next.SelectionPresets[next.SelectionPresets.FindIndex(x => x.Id == preset.Id)] = preset);
        LoadSelectionDraft(); HasError = false; Status = "選択配置Presetを保存しました。Timelineは変更していません。";
    }
    public void CopySelectionPreset()
    {
        RequireCleanSelectionDraft(); var current = CurrentSelectionPreset;
        var stem = current.Name.Length > 100 ? current.Name[..100] : current.Name;
        var number = 1; string name;
        do { name = $"{stem}（コピー {number++}）"; } while (settings.SelectionPresets.Any(x => x.Profile == current.Profile && x.Name == name));
        var copy = current with { Id = Guid.NewGuid(), Name = name };
        EditSettings(next => { next.SelectionPresets.Add(copy); next.CurrentSelectionPresetId = copy.Id; });
        HasError = false; Status = "選択配置Presetを複製しました。名前や条件を編集して保存してください。";
    }
    public void DeleteSelectionPreset()
    {
        RequireCleanSelectionDraft(); var current = CurrentSelectionPreset;
        if (settings.SelectionPresets.Count(x => x.Profile == current.Profile) <= 1)
            throw new InvalidOperationException("このProfileの最後のPresetは削除できません。");
        EditSettings(next => { next.SelectionPresets.RemoveAll(x => x.Id == current.Id); next.CurrentSelectionPresetId = next.SelectionPresets.First(x => x.Profile == current.Profile).Id; });
        HasError = false; Status = "選択配置Presetを削除しました。既存Itemは変更していません。";
    }
    private void DisposeSelectionPresets() => SelectionDraft.PropertyChanged -= SelectionDraftChanged;
}
