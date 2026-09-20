using System.Runtime.CompilerServices;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private sealed record VoiceNavigation(long Revision, Timeline Timeline, VoiceItem Voice, int Frame, int No, string Status);
    // A native Seek cannot be cancelled. A weak per-live-Preview lock also serializes
    // the last in-flight seek across Tool-root replacement; queued stale work is skipped.
    private static readonly ConditionalWeakTable<object, SemaphoreSlim> previewSeekLocks = new();
    private VoiceNavigation? pendingVoiceNavigation;
    private long voiceNavigationRevision;
    private bool voiceNavigationWorkerActive, voiceNavigationDisposed;
    private Task voiceNavigationTask = Task.CompletedTask;
    internal Func<ExpressionNavigationHost>? NavigationHostFactory { get; set; }
    internal Task NavigationCompletion => voiceNavigationTask;
    internal bool NavigationWorkerActive => voiceNavigationWorkerActive;

    private void QueueExpressionNavigation(AssignmentRow row, bool refreshCurrentContent = false)
    {
        // A content refresh uses this same latest-wins seek worker, but must not
        // finish an expression trial or pull the user back from a newer selection.
        var current = RequireTimeline();
        if (voiceNavigationDisposed || !Rows.Contains(row) || !current.Items.Contains(row.Target.Voice))
            throw new InvalidOperationException("対象音声が現在のシーンにありません。メンテナンスから一覧を読み直してください。");
        var voice = row.Target.Voice;
        var target = voice.Frame;
#if YMM4_PROOF
        NativeProof.TraceRound4Activation(row.No, refreshCurrentContent, expressionTrialSession.IsOpen,
            ReferenceEquals(expressionTrialSession.Voice, voice), current.CurrentFrame,
            current.SelectedItems.Count, ReferenceEquals(current.SelectedItem, voice), HasError, Status);
#endif
        if (refreshCurrentContent)
        {
            if (current.CurrentFrame != target || current.SelectedItems.Count != 1 ||
                !ReferenceEquals(current.SelectedItems[0], voice)) return;
        }
        else
        {
            // Reopening the chooser for an already-active Voice is activation,
            // not leaving the current expression trial. Re-running navigation
            // here would split every A -> B -> C trial into separate Undo steps.
            if (expressionTrialSession.IsOpen && ReferenceEquals(expressionTrialSession.Voice, voice) &&
                current.CurrentFrame == target && current.SelectedItems.Count == 1 &&
                ReferenceEquals(current.SelectedItems[0], voice)) return;
            CloseExpressionTrialSession();
            current.CurrentFrame = target;
            current.SelectItem(voice);
        }
        var message = refreshCurrentContent ? Status : $"No.{row.No} の音声位置へ移動しました。";
        var request = new VoiceNavigation(++voiceNavigationRevision, current, voice, target, row.No, message);
        pendingVoiceNavigation = request;
        if (!refreshCurrentContent) { HasError = false; Status = message; }
        if (voiceNavigationWorkerActive) return;
        voiceNavigationWorkerActive = true;
        voiceNavigationTask = DrainVoiceNavigationAsync();
    }
    private bool NavigationIsCurrent(VoiceNavigation request) => !voiceNavigationDisposed &&
        request.Revision == voiceNavigationRevision && ReferenceEquals(timeline, request.Timeline) &&
        request.Timeline.Items.Contains(request.Voice) && request.Voice.Frame == request.Frame;

    private async Task DrainVoiceNavigationAsync()
    {
        VoiceNavigation? executing = null;
        try
        {
            while (pendingVoiceNavigation is { } request)
            {
                executing = request;
                pendingVoiceNavigation = null;
                if (!NavigationIsCurrent(request)) continue;
                ExpressionNavigationHost host;
                try { host = NavigationHostFactory?.Invoke() ?? ExpressionNavigationHost.Resolve(); }
                catch (Exception) { host = ExpressionNavigationHost.Missing; }
                var notices = new List<string>();
                if (host.SeekAsync is { } seek)
                {
                    var gate = previewSeekLocks.GetValue(host.PreviewOwner ?? this, _ => new SemaphoreSlim(1, 1));
                    await gate.WaitAsync();
                    try
                    {
                        if (!NavigationIsCurrent(request)) continue;
                        try { await seek(request.Frame); }
                        catch (Exception) { notices.Add("プレビュー・再生位置を同期できませんでした。"); }
                    }
                    finally { gate.Release(); }
                }
                else notices.Add("この環境ではプレビュー・再生位置の同期は利用できません。");
                if (!NavigationIsCurrent(request)) continue;
                var follow = settings.Presentation.ViewportFollow;
                if (follow != ExpressionViewportFollow.Off)
                {
                    if (host.CanFollow)
                    {
                        try
                        {
                            if (follow == ExpressionViewportFollow.Always || !host.ContainFrameInViewport!(request.Frame))
                                host.ScrollFrame!(request.Frame);
                        }
                        catch (Exception) { notices.Add("タイムラインの表示範囲を追従できませんでした。"); }
                    }
                    else notices.Add("この環境ではタイムラインの表示範囲の追従は利用できません。");
                }
                // A slow completion never replaces a newer row's status, a different
                // action's result, or an actionable error that appeared during the await.
                if (NavigationIsCurrent(request) && !HasError && Status == request.Status && notices.Count > 0)
                    Status = request.Status + " " + string.Join(" ", notices);
            }
        }
        catch (Exception ex)
        {
            if (executing != null && NavigationIsCurrent(executing))
            {
                HasError = true; Status = "音声位置の同期を完了できませんでした: " + ex.GetBaseException().Message;
            }
        }
        finally { voiceNavigationWorkerActive = false; }
    }
    private void CancelExpressionNavigation(bool dispose = false)
    {
        voiceNavigationRevision++; pendingVoiceNavigation = null;
        if (dispose) voiceNavigationDisposed = true;
        // Keep the in-flight worker/Task until the public host seek finishes.
        // A later request uses that same worker instead of starting another one.
    }
}
