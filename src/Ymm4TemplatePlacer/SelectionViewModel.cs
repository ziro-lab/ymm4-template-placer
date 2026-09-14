using System.Collections.ObjectModel;
using System.ComponentModel;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private bool refreshingSelection, loadingSelectionDraft;
    private SelectionPreset? loadedSelectionPreset;
    private LibraryEntryView? selectionTemplate;
    private string selectionPreview = "";
    public SelectionPresetDraft SelectionDraft { get; } = new();
    public ObservableCollection<SelectionProfileChoice> SelectionProfiles { get; } = [];
    public ObservableCollection<SelectionPreset> SelectionPresets { get; } = [];
    public ObservableCollection<LibraryEntryView> SelectionTemplates { get; } = [];
    public IReadOnlyList<AnchorChoice> SelectionAnchors { get; } =
        [new(0, "開始"), new(25, "25%"), new(50, "50%"), new(75, "75%"), new(100, "終了（末尾の次フレーム）")];
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
        get => SelectionPresets.FirstOrDefault(x => x.Id == settings.CurrentSelectionPresetId);
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
    public string SelectionContext => timeline?.SelectedItems.Count is > 0 and var count
        ? $"タイムライン: {count}個選択中"
        : "タイムラインで、配置の基準にするアイテムを選んでください。";
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
            if (!SelectionTemplates.Select(x => x.Entry).SequenceEqual(settings.Library))
            {
                SelectionTemplates.Clear();
                foreach (var entry in settings.Library) SelectionTemplates.Add(new(entry));
            }
            SelectionTemplate = SelectionTemplates.FirstOrDefault(x => x.Id == id);
            var presets = settings.SelectionPresets.Where(x => x.Profile == CurrentSelectionPreset.Profile).ToArray();
            if (!SelectionPresets.SequenceEqual(presets))
            {
                SelectionPresets.Clear();
                foreach (var preset in presets) SelectionPresets.Add(preset);
            }
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
            var profiles = SelectionPlacement.Profiles(timeline?.SelectedItems.Count ?? 0);
            if (!SelectionProfiles.SequenceEqual(profiles))
            {
                SelectionProfiles.Clear();
                foreach (var profile in profiles) SelectionProfiles.Add(profile);
            }
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
    private void InvalidateSelectionPreview() => RequestSelectionPreview();
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
        if (SelectionPresetDirty) throw new InvalidOperationException("選択配置プリセットを保存するか［編集を戻す］を押してください。");
    }
    private SelectionPlacement PlanSelection()
    {
        RequireCleanSelectionDraft();
        var entry = selectionTemplate?.Entry ?? throw new InvalidOperationException("配置するテンプレートを選んでください。");
        if (!settings.Library.Contains(entry)) throw new InvalidOperationException("テンプレート管理の登録が変更されています。選び直してください。");
        return SelectionPlacement.Create(RequireTimeline(), entry, CurrentSelectionPreset);
    }
    public SelectionPlacement PreviewSelection()
    {
        var planned = PlanSelection(); HasError = false;
        CancelSelectionPreview(); SelectionPreview = DescribeSelectionPreview(planned);
        Status = "配置予定を更新しました。タイムラインは変更していません。"; return planned;
    }
    public int PlaceSelection()
    {
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
        var planned = PlanSelection(); var count = planned.Plan.Commit(RequireTimeline(), undo);
        HasError = false; CancelSelectionPreview(); SelectionPreview = "配置しました。対象を選び直すと次の予定を表示します。";
        Status = $"「{selectionTemplate!.DisplayName}」をプリセット「{CurrentSelectionPreset.Name}」で追加: 開始 {planned.Item.Frame} / 長さ {planned.Item.Length} / レイヤー {planned.Item.Layer}。「元に戻す」1回で戻せます。";
        return count;
    }
    public void SaveSelectionPreset()
    {
        var preset = SelectionDraft.Read(CurrentSelectionPreset);
        EditSettings(next => next.SelectionPresets[next.SelectionPresets.FindIndex(x => x.Id == preset.Id)] = preset);
        LoadSelectionDraft(); HasError = false; Status = "選択配置プリセットを保存しました。タイムラインは変更していません。";
    }
    public void CopySelectionPreset()
    {
        RequireCleanSelectionDraft(); var current = CurrentSelectionPreset;
        var stem = current.Name.Length > 100 ? current.Name[..100] : current.Name;
        var number = 1; string name;
        do { name = $"{stem}（コピー {number++}）"; } while (settings.SelectionPresets.Any(x => x.Profile == current.Profile && x.Name == name));
        var copy = current with { Id = Guid.NewGuid(), Name = name };
        EditSettings(next => { next.SelectionPresets.Add(copy); next.CurrentSelectionPresetId = copy.Id; });
        HasError = false; Status = "選択配置プリセットを複製しました。名前や条件を編集して保存してください。";
    }
    public void DeleteSelectionPreset()
    {
        RequireCleanSelectionDraft(); var current = CurrentSelectionPreset;
        if (settings.SelectionPresets.Count(x => x.Profile == current.Profile) <= 1)
            throw new InvalidOperationException("この配置方法の最後のプリセットは削除できません。");
        EditSettings(next => { next.SelectionPresets.RemoveAll(x => x.Id == current.Id); next.CurrentSelectionPresetId = next.SelectionPresets.First(x => x.Profile == current.Profile).Id; });
        HasError = false; Status = "選択配置プリセットを削除しました。既存アイテムは変更していません。";
    }
    private void DisposeSelectionPresets()
    {
        selectionPreviewActive = false; CancelSelectionPreview();
        SelectionDraft.PropertyChanged -= SelectionDraftChanged;
    }
}
