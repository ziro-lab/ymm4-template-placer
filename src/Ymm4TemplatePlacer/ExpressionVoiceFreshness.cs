using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal enum ExpressionRowsFreshness { Current, PendingBatch, StalePending }
// Same-session work only. Copies retain exact source identities; nothing is serialized or migrated.
internal sealed record PendingVoiceRowsWork(Timeline? SourceTimeline, IReadOnlyList<AssignmentRow> Rows, bool Stale);

public sealed partial class PlacerViewModel
{
    private Timeline? watchedVoiceTimeline, voiceRowsTimeline;
    private readonly HashSet<VoiceItem> watchedVoices = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<VoiceItem> dirtyVoices = new(ReferenceEqualityComparer.Instance);
    private DispatcherOperation? queuedVoiceFreshness;
    private bool voiceFreshnessActive, voiceFreshnessDisposed, fullVoiceReconcilePending;
    private ExpressionRowsFreshness voiceRowsFreshness;
    private string voiceFreshnessProblem = "";
    public bool CanEditExpressionRows => voiceRowsFreshness != ExpressionRowsFreshness.StalePending && !IsExpressionLoading;
    public bool ExpressionRowsStale => voiceRowsFreshness == ExpressionRowsFreshness.StalePending;
    public string ExpressionFreshnessNotice => ExpressionRowsStale
        ? "音声やシーンが変わりました。未配置の割り当ては保持しています。Excelを読み込み直すか、内容を確認して［一覧を読み直す］を選んでください。"
        : voiceFreshnessProblem;
    internal int AutomaticVoiceRebuildCount { get; private set; }
    internal int VoiceFreshnessCheckCount { get; private set; }
    internal int WatchedVoiceCount => watchedVoices.Count;
    internal Timeline? WatchedVoiceTimeline => watchedVoiceTimeline;

    private bool HasProtectedPendingVoiceWork() => UsesRelativeExpressions &&
        voiceRowsFreshness != ExpressionRowsFreshness.Current;

    private void SetVoiceFreshnessState(ExpressionRowsFreshness state)
    {
        voiceRowsFreshness = state;
        foreach (var row in Rows) row.AssignmentLocked = state == ExpressionRowsFreshness.StalePending;
        OnPropertyChanged(nameof(CanEditExpressionRows)); OnPropertyChanged(nameof(ExpressionRowsStale));
        OnPropertyChanged(nameof(ExpressionFreshnessNotice)); UpdateCommands();
    }
    private void RememberVoiceRows(bool restoreAssociations)
    {
        voiceRowsTimeline = timeline; voiceFreshnessProblem = "";
        SetVoiceFreshnessState(!restoreAssociations && PendingRelativeExpressionCount > 0
            ? ExpressionRowsFreshness.PendingBatch : ExpressionRowsFreshness.Current);
    }
    private void CompletePendingVoiceWork()
    {
        SetVoiceFreshnessState(ExpressionRowsFreshness.Current);
        expressionCacheDirty = true;
        if (activeTask == "expression" && UsesRelativeExpressions) RequestExpressionLoad(true);
    }
    private void RequireFreshExpressionRows()
    {
        if (ExpressionRowsStale || IsExpressionLoading) throw new InvalidOperationException(IsExpressionLoading ? "表情一覧の読み込み完了後に実行してください。" : ExpressionFreshnessNotice);
    }
    private void SetVoiceFreshnessActive(bool active)
    {
        active &= UsesRelativeExpressions && !voiceFreshnessDisposed;
        if (voiceFreshnessActive == active && ReferenceEquals(watchedVoiceTimeline, active ? timeline : null)) return;
        voiceFreshnessActive = active;
        RebindVoiceFreshness();
    }
    internal void RebindVoiceFreshness()
    {
        queuedVoiceFreshness?.Abort(); queuedVoiceFreshness = null;
        if (watchedVoiceTimeline != null) watchedVoiceTimeline.PropertyChanged -= VoiceTimelineChanged;
        foreach (var voice in watchedVoices) voice.PropertyChanged -= VoiceItemChanged;
        watchedVoices.Clear(); dirtyVoices.Clear(); watchedVoiceTimeline = null; fullVoiceReconcilePending = false;
        if (!voiceFreshnessActive || voiceFreshnessDisposed || timeline == null) return;
        watchedVoiceTimeline = timeline;
        watchedVoiceTimeline.PropertyChanged += VoiceTimelineChanged;
        // Voice subscriptions are populated by the same one-pass host capture used by
        // expression loading. Do not immediately enumerate Timeline.Items again here.
    }
    internal void ReconcileVoiceWatchers(IReadOnlyList<VoiceSnapshot> voices)
    {
        if (!voiceFreshnessActive || voiceFreshnessDisposed || timeline == null || !ReferenceEquals(timeline, watchedVoiceTimeline)) return;
        var current = voices.Select(x => x.Voice).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var removed in watchedVoices.Where(x => !current.Contains(x)).ToArray())
        {
            removed.PropertyChanged -= VoiceItemChanged; watchedVoices.Remove(removed); dirtyVoices.Remove(removed);
        }
        foreach (var added in current)
            if (watchedVoices.Add(added)) added.PropertyChanged += VoiceItemChanged;
    }
    private void VoiceTimelineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, watchedVoiceTimeline)) return;
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(Timeline.Items))
        {
            fullVoiceReconcilePending = true; RequestVoiceFreshnessCheck();
        }
    }
    private void VoiceItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not VoiceItem voice || !watchedVoices.Contains(voice)) return;
        // Remark deliberately does not participate. Managed expression association writes
        // and non-Voice item insertions must not tear down the user's current Rows.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName is nameof(VoiceItem.Character) or nameof(VoiceItem.CharacterName))
        {
            fullVoiceReconcilePending = true; RequestVoiceFreshnessCheck(); return;
        }
        if (e.PropertyName is nameof(VoiceItem.Frame) or nameof(VoiceItem.Length) or nameof(VoiceItem.Layer) or nameof(VoiceItem.Serif))
        {
            dirtyVoices.Add(voice); RequestVoiceFreshnessCheck();
        }
    }
    internal void RequestVoiceFreshnessCheck()
    {
        if (!voiceFreshnessActive || voiceFreshnessDisposed || queuedVoiceFreshness?.Status == DispatcherOperationStatus.Pending) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;
        queuedVoiceFreshness = dispatcher.InvokeAsync(CheckVoiceFreshness, DispatcherPriority.Background);
    }
    internal void CheckVoiceFreshness()
    {
        queuedVoiceFreshness = null;
        if (!voiceFreshnessActive || voiceFreshnessDisposed || timeline == null || !ReferenceEquals(timeline, watchedVoiceTimeline)) return;
        VoiceFreshnessCheckCount++;
        if (HasProtectedPendingVoiceWork())
        {
            dirtyVoices.Clear(); fullVoiceReconcilePending = false; SetVoiceFreshnessState(ExpressionRowsFreshness.StalePending); return;
        }
        if (fullVoiceReconcilePending)
        {
            dirtyVoices.Clear(); fullVoiceReconcilePending = false; expressionCacheDirty = true; RequestExpressionLoad(true); return;
        }
        if (dirtyVoices.Count == 0) return;
        var changed = dirtyVoices.ToArray(); dirtyVoices.Clear();
        try { ApplyIncrementalVoiceChanges(changed); }
        catch (Exception ex)
        {
            voiceFreshnessProblem = "音声一覧を更新できませんでした。現在の割り当ては保持しています: " + ex.GetBaseException().Message;
            OnPropertyChanged(nameof(ExpressionFreshnessNotice));
        }
    }
    private bool VoiceRowsMatch(Timeline current, IReadOnlyList<VoiceSnapshot> next) => ReferenceEquals(voiceRowsTimeline, current) &&
        Rows.Count == next.Count && Rows.Select((row, index) => SameVoice(row.Target, next[index])).All(x => x);
    private PendingVoiceRowsWork? CapturePendingVoiceRows() => HasProtectedPendingVoiceWork()
        ? new(voiceRowsTimeline, Rows.Select(x => x.CopyPending()).ToArray(), ExpressionRowsStale) : null;
    private bool RestorePendingVoiceRows(TransientWorkSnapshot snapshot)
    {
        if (snapshot.PendingVoiceRows is not { } work || !UsesRelativeExpressions) return false;
        SetRows(work.Rows.Select(x => x.CopyPending()).ToArray(), false);
        voiceRowsTimeline = work.SourceTimeline;
        SetVoiceFreshnessState(work.Stale || timeline == null || !VoiceRowsMatch(timeline, VoiceSnapshot.Capture(timeline))
            ? ExpressionRowsFreshness.StalePending : ExpressionRowsFreshness.PendingBatch);
        return true;
    }
    private void DisposeVoiceFreshness()
    {
        voiceFreshnessDisposed = true; voiceFreshnessActive = false; CancelExpressionLoad(); RebindVoiceFreshness();
    }
}
