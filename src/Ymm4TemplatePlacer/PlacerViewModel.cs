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
    public ObservableCollection<AssignmentRow> Rows { get; } = [];
    public string Summary => $"{Rows.Count}件 / 選択 {Rows.Count(x => x.SelectedChoice.Template != null)}件 / 未選択 {Rows.Count(x => x.HasCandidates && x.SelectedChoice.Template == null)}件 / 候補なし {Rows.Count(x => !x.HasCandidates)}件";
    public string Status { get => status; private set { keepPartialStatus = false; Set(ref status, value); } }
    public bool HasError { get => hasError; private set => Set(ref hasError, value); }
    public ActionCommand RefreshCommand { get; }
    public ActionCommand PlaceCommand { get; }
    public ActionCommand ExportCommand { get; }
    public ActionCommand ImportCommand { get; }
    public bool UsesRelativeExpressions => intentInitialized && !UseLegacyWorkspace;
    public bool ShowExpressionBatchPlace => !UsesRelativeExpressions || HasPendingRelativeAssignments();

    public PlacerViewModel()
    {
        RefreshCommand = new ActionCommand(_ => timeline != null, _ => Guard(() =>
        {
            if ((HasProtectedPendingVoiceWork() || Rows.Any(x => x.SelectedChoice.Template != null)) && MessageBox.Show("一覧を読み直すと未配置の割り当てを破棄します。続けますか？", Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            Refresh();
        }));
        PlaceCommand = new ActionCommand(_ => timeline != null && undo != null && settingsAvailable && !ExpressionRowsStale &&
            (UsesRelativeExpressions ? HasPendingRelativeAssignments() : !ExpressionPresetDirty && Rows.Any(x => x.SelectedChoice.Template != null)) &&
            Rows.All(x => x.SelectedChoice.IsAvailable), _ => Guard(() => Place()));
        ExportCommand = new ActionCommand(_ => timeline != null && Rows.Count > 0 && !ExpressionRowsStale, _ => Guard(() =>
        {
            var dialog = new SaveFileDialog { Filter = "Excelブック (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true, FileName = "TemplateAssignments.xlsx", Title = "割り当てをExcelへ出力" };
            if (dialog.ShowDialog() == true) ExportTo(dialog.FileName);
        }));
        ImportCommand = new ActionCommand(_ => timeline != null, _ => Guard(() =>
        {
            var dialog = new OpenFileDialog { Filter = "Excelブック (*.xlsx)|*.xlsx", CheckFileExists = true, Title = "割り当てをExcelから読み込み" };
            if (dialog.ShowDialog() == true) ImportFrom(dialog.FileName);
        }));
        InitializeV04();
        InitializeExpressionImmediate();
        PropertyChanged += ExpressionModeChanged;
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
        if (changed) { CancelExpressionNavigation(); CloseExpressionTrialSession(); DetachTimelineV04(); DeactivateIntentWorkspace(); deferredExpressionResume = null; }
        timeline = info.Timeline; undo = info.UndoRedoManager;
        if (changed)
        {
            AttachTimelineV04();
            if (preservePending)
            {
                SetVoiceFreshnessState(ExpressionRowsFreshness.StalePending);
                if (intentInitialized) RefreshIntentWorkspace();
                OnPropertyChanged(nameof(SceneName));
            }
            else Guard(Refresh);
            RebindVoiceFreshness();
        }
        TryRestoreTransientWork(); UpdateCommands();
    }
    private IReadOnlyList<FaceTemplate> ExpressionCatalog() => UsesRelativeExpressions ? IntentExpressionCatalog.Read(settings) : TemplateCatalog.Read();
    private void ExpressionModeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UseLegacyWorkspace)) return;
        SetVoiceFreshnessActive(activeTask.Length > 0);
        RefreshExpressionVocabulary(); TryRestoreDeferredExpressionWork(); OnPropertyChanged(nameof(UsesRelativeExpressions)); UpdateCommands();
    }
    public void RefreshExpressionVocabulary()
    {
        var catalog = ExpressionCatalog();
        var keepPending = HasProtectedPendingVoiceWork();
        if (keepPending) { OnPropertyChanged(nameof(Summary)); UpdateCommands(); return; }
        var previous = suppressExpressionApply; suppressExpressionApply = true;
        try
        {
            foreach (var row in Rows)
            {
                row.RefreshCandidates(catalog, settings, UsesRelativeExpressions);
                if (UsesRelativeExpressions && !keepPending) RestoreExpressionChoiceFromTimeline(row);
            }
        }
        finally { suppressExpressionApply = previous; }
        OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(UsesRelativeExpressions)); UpdateCommands();
    }
    public void Refresh()
    {
        CloseExpressionTrialSession();
        var current = RequireTimeline();
        if (intentInitialized) RefreshIntentWorkspace();
        var catalog = ExpressionCatalog();
        SetRows(VoiceSnapshot.Capture(current).Select((x, i) => new AssignmentRow(i + 1, x, catalog, UsesRelativeExpressions)).ToArray());
        RefreshV04(); HasError = false; Status = ""; OnPropertyChanged(nameof(SceneName));
    }
    public int Place()
    {
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
            HasError = false; Status = $"保存済みのパレット設定で{added}アイテムを関連付けて配置しました。設定による配置なし: {staged.Skipped}行。元に戻す1回で戻せます。";
            CompletePendingVoiceWork(); UpdateCommands(); return added;
        }
        var preset = RequireExpressionPreset(); var count = PlaceAssociatedExpression(preset);
        HasError = false; Status = $"{count}件をプリセット「{preset.Name}」で関連付けて配置しました。既存アイテムは保持しています。YMM4の「元に戻す」で戻せます。";
        UpdateCommands(); return count;
    }
    public void ExportTo(string path)
    {
        RequireFreshExpressionRows();
        CloseExpressionTrialSession();
        var current = RequireTimeline(); PlacementEngine.ValidateSnapshot(current, Rows.Select(x => x.Target).ToArray());
        if (Rows.Any(x => !x.SelectedChoice.IsAvailable)) throw new InvalidOperationException("参照切れの表情選択があります。候補を確認してから出力してください。");
        WorkbookBridge.Export(path, current.Name, Rows.ToArray(), ExpressionCatalog());
        HasError = false; Status = "Excelへ出力しました。テンプレート列だけを編集してください。YMM4側を変更した場合は再出力が必要です。";
    }
    public void ImportFrom(string path)
    {
        CloseExpressionTrialSession();
        var current = RequireTimeline(); var next = WorkbookBridge.Import(path, current.Name, VoiceSnapshot.Capture(current), ExpressionCatalog());
        SetRows(next, false); HasError = false; OnPropertyChanged(nameof(ShowExpressionBatchPlace));
        Status = UsesRelativeExpressions ? "Excelを読み込みました。表情を確認して［配置］してください。配置方法は現在保存されているパレットに従います。タイムラインはまだ変更していません。" :
            $"Excelを読み込みました。選択内容と現在のプリセット「{CurrentExpressionPreset.Name}」を確認して［配置］してください。タイムラインはまだ変更していません。";
    }
    private Timeline RequireTimeline() => timeline ?? throw new InvalidOperationException("対象シーンを開き、プラグインを開き直してください。");
    private void SetRows(IReadOnlyList<AssignmentRow> rows, bool restoreAssociations = true)
    {
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear();
        foreach (var row in rows)
        {
            row.SetCandidateMode(UsesRelativeExpressions); row.PreferPalette(settings);
            if (UsesRelativeExpressions && restoreAssociations) RestoreExpressionChoiceFromTimeline(row);
            row.PropertyChanged += RowChanged; Rows.Add(row);
        }
        RememberVoiceRows(restoreAssociations);
        OnPropertyChanged(nameof(Summary)); UpdateCommands();
    }
    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssignmentRow.SelectedChoice))
        {
            OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(ShowExpressionBatchPlace)); UpdateCommands();
            if (!suppressExpressionApply && UsesRelativeExpressions && sender is AssignmentRow row) ApplyImmediateExpressionChoice(row);
        }
    }
    private void UpdateCommands()
    {
        OnPropertyChanged(nameof(ExpressionPlaceHint)); OnPropertyChanged(nameof(ShowExpressionBatchPlace)); RefreshCommand?.RaiseCanExecuteChanged(); PlaceCommand?.RaiseCanExecuteChanged();
        ExportCommand?.RaiseCanExecuteChanged(); ImportCommand?.RaiseCanExecuteChanged(); resyncCommand?.RaiseCanExecuteChanged();
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
        CancelExpressionNavigation(true); DisposeVoiceFreshness();
        CloseExpressionTrialSession(); expressionTrialSession.Dispose();
        DeactivateIntentWorkspace(); DetachTimelineV04(); DisposeV04();
        if (intentSettings != null) intentSettings.Edited -= IntentSettingsEdited;
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear(); timeline = null; undo = null;
    }
}
