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

    public PlacerViewModel()
    {
        RefreshCommand = new ActionCommand(_ => timeline != null, _ => Guard(() =>
        {
            if (Rows.Any(x => x.SelectedChoice.Template != null) && MessageBox.Show("更新すると現在の選択をクリアします。続けますか？", Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            Refresh();
        }));
        PlaceCommand = new ActionCommand(_ => timeline != null && undo != null && settingsAvailable &&
            (UsesRelativeExpressions || !ExpressionPresetDirty) && Rows.Any(x => x.SelectedChoice.Template != null) &&
            Rows.All(x => x.SelectedChoice.IsAvailable), _ => Guard(() => Place()));
        ExportCommand = new ActionCommand(_ => timeline != null && Rows.Count > 0, _ => Guard(() =>
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
        if (changed) { DetachTimelineV04(); DeactivateIntentWorkspace(); }
        timeline = info.Timeline; undo = info.UndoRedoManager;
        if (changed) { AttachTimelineV04(); Guard(Refresh); }
        TryRestoreTransientWork(); UpdateCommands();
    }
    private IReadOnlyList<FaceTemplate> ExpressionCatalog() => UsesRelativeExpressions ? IntentExpressionCatalog.Read(settings) : TemplateCatalog.Read();
    private void ExpressionModeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UseLegacyWorkspace)) return;
        RefreshExpressionVocabulary(); OnPropertyChanged(nameof(UsesRelativeExpressions)); UpdateCommands();
    }
    public void RefreshExpressionVocabulary()
    {
        var catalog = ExpressionCatalog();
        foreach (var row in Rows) row.RefreshCandidates(catalog, settings, UsesRelativeExpressions);
        OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(UsesRelativeExpressions)); UpdateCommands();
    }
    public void Refresh()
    {
        var current = RequireTimeline();
        if (intentInitialized) RefreshIntentWorkspace();
        var catalog = ExpressionCatalog();
        SetRows(VoiceSnapshot.Capture(current).Select((x, i) => new AssignmentRow(i + 1, x, catalog, UsesRelativeExpressions)).ToArray());
        RefreshV04(); HasError = false; Status = ""; OnPropertyChanged(nameof(SceneName));
    }
    public int Place()
    {
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。プラグインを開き直してください。");
        if (UsesRelativeExpressions)
        {
            var staged = IntentExpressionPlacement.Create(current, Rows.ToArray(), settings);
            if (staged.NextSerial != settings.NextAssociationId) EditSettings(next => next.NextAssociationId = staged.NextSerial);
            var added = staged.Commit(current, undo, settings);
            HasError = false; Status = $"保存済みのパレット設定で{added}アイテムを関連付けて配置しました。設定による配置なし: {staged.Skipped}行。元に戻す1回で戻せます。";
            UpdateCommands(); return added;
        }
        var preset = RequireExpressionPreset(); var count = PlaceAssociatedExpression(preset);
        HasError = false; Status = $"{count}件をプリセット「{preset.Name}」で関連付けて配置しました。既存アイテムは保持しています。YMM4の「元に戻す」で戻せます。";
        UpdateCommands(); return count;
    }
    public void ExportTo(string path)
    {
        var current = RequireTimeline(); PlacementEngine.ValidateSnapshot(current, Rows.Select(x => x.Target).ToArray());
        if (Rows.Any(x => !x.SelectedChoice.IsAvailable)) throw new InvalidOperationException("参照切れの表情選択があります。候補を確認してから出力してください。");
        WorkbookBridge.Export(path, current.Name, Rows.ToArray(), ExpressionCatalog());
        HasError = false; Status = "Excelへ出力しました。テンプレート列だけを編集してください。YMM4側を変更した場合は再出力が必要です。";
    }
    public void ImportFrom(string path)
    {
        var current = RequireTimeline(); var next = WorkbookBridge.Import(path, current.Name, VoiceSnapshot.Capture(current), ExpressionCatalog());
        SetRows(next); HasError = false;
        Status = UsesRelativeExpressions ? "Excelを読み込みました。表情を確認して［配置］してください。配置方法は現在保存されているパレットに従います。タイムラインはまだ変更していません。" :
            $"Excelを読み込みました。選択内容と現在のプリセット「{CurrentExpressionPreset.Name}」を確認して［配置］してください。タイムラインはまだ変更していません。";
    }
    private Timeline RequireTimeline() => timeline ?? throw new InvalidOperationException("対象シーンを開き、プラグインを開き直してください。");
    private void SetRows(IReadOnlyList<AssignmentRow> rows)
    {
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear();
        foreach (var row in rows) { row.SetCandidateMode(UsesRelativeExpressions); row.PreferPalette(settings); row.PropertyChanged += RowChanged; Rows.Add(row); }
        OnPropertyChanged(nameof(Summary)); UpdateCommands();
    }
    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssignmentRow.SelectedChoice)) { OnPropertyChanged(nameof(Summary)); UpdateCommands(); }
    }
    private void UpdateCommands()
    {
        OnPropertyChanged(nameof(ExpressionPlaceHint)); RefreshCommand?.RaiseCanExecuteChanged(); PlaceCommand?.RaiseCanExecuteChanged();
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
        disposedTransientWork ??= CaptureTransientWork(); PropertyChanged -= ExpressionModeChanged;
        DeactivateIntentWorkspace(); DetachTimelineV04(); DisposeV04();
        if (intentSettings != null) intentSettings.Edited -= IntentSettingsEdited;
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear(); timeline = null; undo = null;
    }
}
