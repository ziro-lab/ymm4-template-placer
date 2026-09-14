using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public sealed class PlacerViewModel : Bindable, ITimelineToolViewModel, IToolViewModel, IDisposable
{
    private Timeline? timeline;
    private UndoRedoManager? undo;
    private string status = "対象Sceneを開いてください。";
    private bool hasError;
    public string Title => "YMM4 Template Placer";
    public bool CanSuspend => true;
    public string SceneName => timeline?.Name ?? "Sceneなし";
    public ObservableCollection<AssignmentRow> Rows { get; } = [];
    public string Summary => $"{Rows.Count}件 / 選択 {Rows.Count(x => x.SelectedChoice.Template != null)}件 / 未選択 {Rows.Count(x => x.HasCandidates && x.SelectedChoice.Template == null)}件 / 候補なし {Rows.Count(x => !x.HasCandidates)}件";
    public string Status { get => status; private set => Set(ref status, value); }
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
        PlaceCommand = new ActionCommand(_ => timeline != null && undo != null && (Rows.Count > 0 || timeline.Items.Any(PlacementEngine.IsOwned)), _ => Guard(() =>
        {
            if (!Rows.Any(x => x.SelectedChoice.Template != null) && timeline!.Items.Any(PlacementEngine.IsOwned) &&
                MessageBox.Show("Templateは全件未選択です。このSceneのPlugin配置済み表情だけを削除します。続けますか？", Title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
            Place();
        }));
        ExportCommand = new ActionCommand(_ => timeline != null && Rows.Count > 0, _ => Guard(() =>
        {
            var dialog = new SaveFileDialog { Filter = "Excelブック (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true, FileName = "TemplateAssignments.xlsx", Title = "AssignmentをExcelへ出力" };
            if (dialog.ShowDialog() == true) ExportTo(dialog.FileName);
        }));
        ImportCommand = new ActionCommand(_ => timeline != null, _ => Guard(() =>
        {
            var dialog = new OpenFileDialog { Filter = "Excelブック (*.xlsx)|*.xlsx", CheckFileExists = true, Title = "AssignmentをExcelから読み込み" };
            if (dialog.ShowDialog() == true) ImportFrom(dialog.FileName);
        }));
#if YMM4_PROOF
        NativeProof.ViewModel = this;
#endif
    }
    public void SetTimelineToolInfo(TimelineToolInfo info)
    {
        var changed = !ReferenceEquals(timeline, info.Timeline);
        timeline = info.Timeline;
        undo = info.UndoRedoManager;
        if (changed) Guard(Refresh);
        UpdateCommands();
    }
    public void Refresh()
    {
        var current = RequireTimeline();
        var catalog = TemplateCatalog.Read();
        SetRows(VoiceSnapshot.Capture(current).Select((x, i) => new AssignmentRow(i + 1, x, catalog)).ToArray());
        HasError = false;
        Status = Rows.Count == 0 ? "このSceneにはVoiceItemがありません。" : "Templateを選んで［配置］。未選択の行には何も配置しません。";
        OnPropertyChanged(nameof(SceneName));
    }
    public int Place()
    {
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4のUndoに接続できません。Pluginを開き直してください。");
        var count = PlacementEngine.Replace(current, undo, Rows.ToArray());
        HasError = false;
        Status = $"{count}件を配置しました。手動Itemは保持しています。YMM4のUndoで戻せます。";
        UpdateCommands();
        return count;
    }
    public void ExportTo(string path)
    {
        var current = RequireTimeline();
        PlacementEngine.ValidateSnapshot(current, Rows.Select(x => x.Target).ToArray());
        WorkbookBridge.Export(path, current.Name, Rows.ToArray(), TemplateCatalog.Read());
        HasError = false;
        Status = "Excelへ出力しました。Template列だけを編集してください。YMM4側を変更した場合は再出力が必要です。";
    }
    public void ImportFrom(string path)
    {
        var current = RequireTimeline();
        // Fully validate before replacing the visible selections. Import never mutates Timeline.
        var next = WorkbookBridge.Import(path, current.Name, VoiceSnapshot.Capture(current), TemplateCatalog.Read());
        SetRows(next);
        HasError = false;
        Status = "Excelを読み込みました。選択内容を確認して［配置］してください。Timelineはまだ変更していません。";
    }
    private Timeline RequireTimeline() => timeline ?? throw new InvalidOperationException("対象Sceneを開き、Pluginを開き直してください。");
    private void SetRows(IReadOnlyList<AssignmentRow> rows)
    {
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear();
        foreach (var row in rows) { row.PropertyChanged += RowChanged; Rows.Add(row); }
        OnPropertyChanged(nameof(Summary));
        UpdateCommands();
    }
    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssignmentRow.SelectedChoice)) { OnPropertyChanged(nameof(Summary)); UpdateCommands(); }
    }
    private void UpdateCommands()
    {
        RefreshCommand?.RaiseCanExecuteChanged(); PlaceCommand?.RaiseCanExecuteChanged();
        ExportCommand?.RaiseCanExecuteChanged(); ImportCommand?.RaiseCanExecuteChanged();
    }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { HasError = true; Status = "操作を完了できませんでした: " + ex.GetBaseException().Message; }
    }
    public event EventHandler<CreateNewToolViewRequestedEventArgs>? CreateNewToolViewRequested { add { } remove { } }
    public ToolState SaveState() => new() { Title = Title };
    public void LoadState(ToolState stateData) { /* Assignments intentionally have no persistent identity. */ }
    public void Dispose()
    {
        foreach (var row in Rows) row.PropertyChanged -= RowChanged;
        Rows.Clear(); timeline = null; undo = null;
    }
}
