using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel : Bindable, ITimelineToolViewModel, IToolViewModel, IDisposable
{
    private Timeline? timeline;
    private UndoRedoManager? undo;
    private string status = "";
    private bool hasError;
    public string Title => "YMM4 Template Placer";
    public bool CanSuspend => true;
    public string SceneName => timeline?.Name ?? "シーンなし";
    public ExpressionRowCollection Rows { get; } = [];
    public string Summary => CachedExpressionSummary;
    public string Status { get => status; private set { keepPartialStatus = false; Set(ref status, value); } }
    public bool HasError { get => hasError; private set => Set(ref hasError, value); }
    public ActionCommand RefreshCommand { get; }
    public ActionCommand PlaceCommand { get; }
    public ActionCommand ExportCommand { get; }
    public ActionCommand ImportCommand { get; }
    public bool UsesRelativeExpressions => intentInitialized;
    public bool ShowExpressionBatchPlace => IsTemplateExpressionSource && (!UsesRelativeExpressions || PendingRelativeExpressionCount > 0);

    public PlacerViewModel()
    {
        RefreshCommand = new ActionCommand(_ => timeline != null && !IsExpressionLoading, _ => Guard(() =>
        {
            if (IsTachiePresetExpressionSource)
            {
                InvalidatePresetCapabilityCache(); RequestExpressionLoad(true, true); return;
            }
            if ((HasProtectedPendingVoiceWork() || SelectedExpressionCount > 0) && MessageBox.Show("一覧を読み直すと未配置の割り当てを破棄します。続けますか？", Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            if (UsesRelativeExpressions) { expressionCandidateDirty = true; expressionCacheDirty = true; RequestExpressionLoad(true, true); }
            else Refresh();
        }));
        PlaceCommand = new ActionCommand(_ => IsTemplateExpressionSource && ExpressionRowsMatchSource && timeline != null && undo != null && settingsAvailable && !ExpressionRowsStale && !IsExpressionLoading &&
            (UsesRelativeExpressions ? PendingRelativeExpressionCount > 0 : !ExpressionPresetDirty && SelectedExpressionCount > 0) &&
            UnavailableExpressionCount == 0, _ => Guard(() => Place()));
        ExportCommand = new ActionCommand(_ => IsTemplateExpressionSource && ExpressionRowsMatchSource && !IsExpressionLoading && timeline != null && Rows.Count > 0 && !ExpressionRowsStale, _ => Guard(() =>
        {
            var dialog = new SaveFileDialog { Filter = "Excelブック (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true, FileName = "TemplateAssignments.xlsx", Title = "割り当てをExcelへ出力" };
            if (dialog.ShowDialog() == true) ExportTo(dialog.FileName);
        }));
        ImportCommand = new ActionCommand(_ => IsTemplateExpressionSource && timeline != null, _ => Guard(() =>
        {
            var dialog = new OpenFileDialog { Filter = "Excelブック (*.xlsx)|*.xlsx", CheckFileExists = true, Title = "割り当てをExcelから読み込み" };
            if (dialog.ShowDialog() == true) ImportFrom(dialog.FileName);
        }));
        InitializeV04();
        RestoreExpressionSourceModePreference();
        InitializeExpressionImmediate();
#if YMM4_PROOF
        NativeProof.ViewModel = this;
#endif
    }
    partial void InitializeV04();
    partial void RefreshV04();
    partial void AttachTimelineV04();
    partial void DetachTimelineV04();
    partial void DisposeV04();
    public void SetTimelineToolInfo(TimelineToolInfo info)
    {
        var changed = !ReferenceEquals(timeline, info.Timeline);
        var preservePending = changed && HasProtectedPendingVoiceWork();
        if (changed) { CancelTachiePresetApply(); CancelTachiePresetCalibration(); CancelExpressionLoad(); ClearPresetContextWatchers(); CancelExpressionNavigation(); CloseExpressionTrialSession(); DetachTimelineV04(); DeactivateIntentWorkspace(); }
        timeline = info.Timeline; undo = info.UndoRedoManager;
        if (changed)
        {
            AttachTimelineV04();
            expressionCacheTimeline = null; expressionHostFingerprint = null; expressionPreparedVoices = []; expressionPreparedItems = []; expressionCacheDirty = true;
            if (preservePending)
            {
                SetVoiceFreshnessState(ExpressionRowsFreshness.StalePending);
                if (intentInitialized) RefreshIntentWorkspace();
                RefreshV04(); OnPropertyChanged(nameof(SceneName));
            }
            else Guard(RefreshNonExpressionState);
            RebindVoiceFreshness();
        }
        TryRestoreTransientWork(); UpdateCommands();
    }
    private IReadOnlyList<FaceTemplate> ExpressionCatalog() => UsesRelativeExpressions ? IntentExpressionCatalog.Read(settings) : TemplateCatalog.Read();
    public void RefreshExpressionVocabulary()
    {
        if (IsTachiePresetExpressionSource)
        {
            expressionCandidateDirty = true; expressionCacheDirty = true;
            if (activeTask == "expression") RequestExpressionLoad(true);
            UpdateCommands(); return;
        }
        if (UsesRelativeExpressions)
        {
            MarkExpressionVocabularyDirty(); OnPropertyChanged(nameof(UsesRelativeExpressions)); UpdateCommands(); return;
        }
        var catalog = ExpressionCatalog();
        var previous = suppressExpressionApply; suppressExpressionApply = true;
        try { foreach (var row in Rows) row.RefreshCandidates(catalog, settings, false); }
        finally { suppressExpressionApply = previous; }
        RebuildExpressionAggregates(); OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(UsesRelativeExpressions)); UpdateCommands();
    }
    public void Refresh()
    {
        CloseExpressionTrialSession();
        RequireTimeline();
        RefreshNonExpressionState();
        RefreshExpressionSynchronously(true);
    }
    public int Place()
    {
        RequireTemplateExpressionSource();
        RequireFreshExpressionRows();
        CloseExpressionTrialSession();
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。プラグインを開き直してください。");
        if (UsesRelativeExpressions)
        {
            foreach (var row in Rows.Where(x => x.SelectedChoice.Template != null && !ExpressionChoiceMatchesTimeline(x)))
                RequireExpressionDraftCompatible(current, row, row.SelectedChoice);
            var staged = IntentExpressionPlacement.Create(current, Rows.ToArray(), settings, ExpressionSerialSeed(current));
            ObserveExpressionSerial(staged.NextSerial);
            if (staged.NextSerial != settings.NextAssociationId) EditSettings(next => next.NextAssociationId = staged.NextSerial);
            var added = staged.Commit(current, undo, settings);
            suppressExpressionRowEvents = true;
            try { foreach (var row in staged.SuccessfulRows) row.SetAssociationMatch(true); }
            finally { suppressExpressionRowEvents = false; }
            RebuildExpressionAggregates();
            HasError = false; Status = $"保存済みのパレット設定で{added}アイテムを関連付けて配置しました。設定による配置なし: {staged.Skipped}行。元に戻す1回で戻せます。";
            CompletePendingVoiceWork(); UpdateCommands(); return added;
        }
        var preset = RequireExpressionPreset(); var count = PlaceAssociatedExpression(preset);
        HasError = false; Status = $"{count}件をプリセット「{preset.Name}」で関連付けて配置しました。既存アイテムは保持しています。YMM4の「元に戻す」で戻せます。";
        UpdateCommands(); return count;
    }
    public void ExportTo(string path)
    {
        RequireTemplateExpressionSource();
        RequireFreshExpressionRows();
        CloseExpressionTrialSession();
        var current = RequireTimeline(); PlacementEngine.ValidateSnapshot(current, Rows.Select(x => x.Target).ToArray());
        if (Rows.Any(x => !x.SelectedChoice.IsAvailable)) throw new InvalidOperationException("参照切れの表情選択があります。候補を確認してから出力してください。");
        WorkbookBridge.Export(path, current.Name, Rows.ToArray(), ExpressionCatalog());
        HasError = false; Status = "Excelへ出力しました。テンプレート列だけを編集してください。YMM4側を変更した場合は再出力が必要です。";
    }
    public void ImportFrom(string path)
    {
        RequireTemplateExpressionSource();
        CloseExpressionTrialSession();
        var current = RequireTimeline(); var next = WorkbookBridge.Import(path, current.Name, VoiceSnapshot.Capture(current), ExpressionCatalog());
        CancelExpressionLoad();
        SetRows(next, false); HasError = false; OnPropertyChanged(nameof(ShowExpressionBatchPlace));
        Status = UsesRelativeExpressions ? "Excelを読み込みました。表情を確認して［配置］してください。配置方法は現在保存されているパレットに従います。タイムラインはまだ変更していません。" :
            $"Excelを読み込みました。選択内容と現在のプリセット「{CurrentExpressionPreset.Name}」を確認して［配置］してください。タイムラインはまだ変更していません。";
    }
    private Timeline RequireTimeline() => timeline ?? throw new InvalidOperationException("対象シーンを開き、プラグインを開き直してください。");
    private void SetRows(IReadOnlyList<AssignmentRow> rows, bool restoreAssociations = true)
    {
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        suppressExpressionApply = true; suppressExpressionRowEvents = true;
        try
        {
            foreach (var row in rows)
            {
                row.SetCandidateMode(UsesRelativeExpressions); row.PreferPalette(settings);
                if (UsesRelativeExpressions && restoreAssociations) RestoreExpressionChoiceFromTimeline(row);
                row.PropertyChanged += RowChanged;
            }
            Rows.ReplaceAll(rows); expressionPerformance.BatchCollectionPublishes++;
            expressionRowsSource = ExpressionSourceMode.Template; PublishExpressionSourceProperties();
        }
        finally { suppressExpressionRowEvents = false; suppressExpressionApply = false; }
        RebuildExpressionAggregates(); RememberVoiceRows(restoreAssociations);
        OnPropertyChanged(nameof(Summary)); UpdateCommands();
    }
    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppressExpressionRowEvents) return;
        if (sender is AssignmentRow changedRow) UpdateExpressionAggregate(changedRow);
        if (e.PropertyName == nameof(AssignmentRow.SelectedChoice))
        {
            OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(ShowExpressionBatchPlace)); UpdateCommands();
            if (!suppressExpressionApply && ExpressionRowsMatchSource && sender is AssignmentRow row)
            {
                if (IsTemplateExpressionSource && UsesRelativeExpressions) ApplyImmediateExpressionChoice(row);
                else if (IsTachiePresetExpressionSource && !IsExpressionLoading)
                    RequestImmediateTachiePresetChoice(row);
            }
        }
    }
    private void UpdateCommands()
    {
        OnPropertyChanged(nameof(ExpressionPlaceHint)); OnPropertyChanged(nameof(ShowExpressionBatchPlace)); RefreshCommand?.RaiseCanExecuteChanged(); PlaceCommand?.RaiseCanExecuteChanged();
        ExportCommand?.RaiseCanExecuteChanged(); ImportCommand?.RaiseCanExecuteChanged(); resyncCommand?.RaiseCanExecuteChanged();
        NavigateExpressionRowCommand?.RaiseCanExecuteChanged();
        TachiePresetCalibrationCommand?.RaiseCanExecuteChanged();
    }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { HasError = true; Status = "操作を完了できませんでした: " + ex.GetBaseException().Message; }
    }
    public event EventHandler<CreateNewToolViewRequestedEventArgs>? CreateNewToolViewRequested { add { } remove { } }
    public ToolState SaveState() => new() { Title = Title };
    public void LoadState(ToolState stateData) { }
    public void Dispose()
    {
        DisposeAutomaticSettingsSession();
        disposedTransientWork ??= CaptureTransientWork(); PropertyChanged -= ExpressionModeChanged;
        CancelExpressionNavigation(true); CancelTachiePresetApply(); CancelTachiePresetCalibration(); DisposeVoiceFreshness(); DisposePresetDiscovery();
        CloseExpressionTrialSession(); expressionTrialSession.Dispose();
        DeactivateIntentWorkspace(); DetachTimelineV04(); DisposeV04();
        if (intentSettings != null) intentSettings.Edited -= IntentSettingsEdited;
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear(); timeline = null; undo = null;
    }
}
