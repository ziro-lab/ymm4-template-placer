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

    public PlacerViewModel()
    {
        RefreshCommand = new ActionCommand(_ => timeline != null, _ => Guard(() =>
        {
            if (Rows.Any(x => x.SelectedChoice.Template != null) && MessageBox.Show("更新すると現在の選択をクリアします。続けますか？", Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            Refresh();
        }));
        PlaceCommand = new ActionCommand(_ => timeline != null && undo != null && settingsAvailable && !ExpressionPresetDirty && Rows.Any(x => x.SelectedChoice.Template != null), _ => Guard(() => Place()));
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
        if (changed) DetachTimelineV04();
        timeline = info.Timeline;
        undo = info.UndoRedoManager;
        if (changed) { AttachTimelineV04(); Guard(Refresh); }
        TryRestoreTransientWork();
        UpdateCommands();
    }
    public void Refresh()
    {
        var current = RequireTimeline();
        var catalog = TemplateCatalog.Read();
        SetRows(VoiceSnapshot.Capture(current).Select((x, i) => new AssignmentRow(i + 1, x, catalog)).ToArray());
        RefreshV04();
        HasError = false;
        Status = "";
        OnPropertyChanged(nameof(SceneName));
    }
    public int Place()
    {
        RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。プラグインを開き直してください。");
        var preset = RequireExpressionPreset();
        var count = PlaceAssociatedExpression(preset);
        HasError = false;
        Status = $"{count}件をプリセット「{preset.Name}」で関連付けて配置しました。既存アイテムは保持しています。YMM4の「元に戻す」で戻せます。";
        UpdateCommands();
        return count;
    }
    public void ExportTo(string path)
    {
        var current = RequireTimeline();
        PlacementEngine.ValidateSnapshot(current, Rows.Select(x => x.Target).ToArray());
        WorkbookBridge.Export(path, current.Name, Rows.ToArray(), TemplateCatalog.Read());
        HasError = false;
        Status = "Excelへ出力しました。テンプレート列だけを編集してください。YMM4側を変更した場合は再出力が必要です。";
    }
    public void ImportFrom(string path)
    {
        var current = RequireTimeline();
        var next = WorkbookBridge.Import(path, current.Name, VoiceSnapshot.Capture(current), TemplateCatalog.Read());
        SetRows(next);
        HasError = false;
        Status = $"Excelを読み込みました。選択内容と現在のプリセット「{CurrentExpressionPreset.Name}」を確認して［配置］してください。タイムラインはまだ変更していません。";
    }
    private Timeline RequireTimeline() => timeline ?? throw new InvalidOperationException("対象シーンを開き、プラグインを開き直してください。");
    private void SetRows(IReadOnlyList<AssignmentRow> rows)
    {
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear();
        foreach (var row in rows) { row.PreferPalette(settings); row.PropertyChanged += RowChanged; Rows.Add(row); }
        OnPropertyChanged(nameof(Summary));
        UpdateCommands();
    }
    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssignmentRow.SelectedChoice)) { OnPropertyChanged(nameof(Summary)); UpdateCommands(); }
    }
    private void UpdateCommands()
    {
        OnPropertyChanged(nameof(ExpressionPlaceHint));
        RefreshCommand?.RaiseCanExecuteChanged(); PlaceCommand?.RaiseCanExecuteChanged();
        ExportCommand?.RaiseCanExecuteChanged(); ImportCommand?.RaiseCanExecuteChanged();
        resyncCommand?.RaiseCanExecuteChanged();
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
        disposedTransientWork ??= CaptureTransientWork();
        DetachTimelineV04(); DisposeV04();
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear(); timeline = null; undo = null;
    }
}
