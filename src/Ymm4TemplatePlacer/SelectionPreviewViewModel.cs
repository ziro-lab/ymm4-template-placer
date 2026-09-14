using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private bool selectionPreviewActive;
    private DispatcherOperation? queuedSelectionPreview;
    internal int AutomaticPreviewCount { get; private set; }
    internal double LastAutomaticPreviewMilliseconds { get; private set; }

    private void SetSelectionPreviewActive(bool active)
    {
        if (selectionPreviewActive == active) return;
        selectionPreviewActive = active;
        if (active) RequestSelectionPreview(); else CancelSelectionPreview();
    }
    private void CancelSelectionPreview()
    {
        queuedSelectionPreview?.Abort();
        queuedSelectionPreview = null;
    }
    private void RequestSelectionPreview()
    {
        if (!selectionPreviewActive) { CancelSelectionPreview(); SelectionPreview = ""; return; }
        if (!CanPlaceSelection())
        {
            CancelSelectionPreview();
            SelectionPreview = SelectionPresetDirty ? "配置条件を保存するか、編集を戻してください。" :
                timeline?.SelectedItems.Count is not > 0 ? "" :
                selectionTemplate == null ? "配置するテンプレートを選んでください。" : "上の配置方法を選んでください。";
            return;
        }
        SelectionPreview = "配置予定を更新中…";
        if (queuedSelectionPreview?.Status == DispatcherOperationStatus.Pending) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;
        // One deferred calculation per UI event burst; never poll, never calculate in CanExecute.
        queuedSelectionPreview = dispatcher.InvokeAsync(UpdateAutomaticSelectionPreview, DispatcherPriority.Background);
    }
    private static string DescribeSelectionPreview(SelectionPlacement planned) =>
        $"配置予定: 開始 {planned.Item.Frame} / 長さ {planned.Item.Length} / レイヤー {planned.Item.Layer}";
    private void UpdateAutomaticSelectionPreview()
    {
        queuedSelectionPreview = null;
        if (!selectionPreviewActive || !CanPlaceSelection()) return;
        var watch = Stopwatch.StartNew();
        try
        {
            // A preview is discarded, never cached as a future commit. Place re-plans against live state.
            SelectionPreview = DescribeSelectionPreview(PlanSelection());
        }
        catch (Exception ex) { SelectionPreview = "配置できません: " + ex.GetBaseException().Message; }
        finally { watch.Stop(); LastAutomaticPreviewMilliseconds = watch.Elapsed.TotalMilliseconds; AutomaticPreviewCount++; }
        // Automatic information must not clear an error/partial result or invent a success notification.
    }
}
