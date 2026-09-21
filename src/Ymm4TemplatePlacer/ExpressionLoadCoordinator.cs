using System.Windows;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private readonly ExpressionPerformanceDiagnostics expressionPerformance = new();
    private CancellationTokenSource? expressionLoadCancellation;
    private long expressionLoadGeneration;
    private bool expressionCacheDirty = true, expressionCandidateDirty = true, isExpressionLoading, suppressExpressionRowEvents;
    private string? expressionHostFingerprint;
    private Timeline? expressionCacheTimeline;
    private IReadOnlyList<ExpressionCandidateDescriptor>? expressionCandidateCache;
    private IReadOnlyList<VoiceSnapshot> expressionPreparedVoices = [];
    private IReadOnlyList<ExpressionCapturedItem> expressionPreparedItems = [];
    private Dictionary<string, IReadOnlyList<TemplateChoice>> expressionChoicesByCharacter = new(StringComparer.Ordinal);
    private readonly Dictionary<AssignmentRow, ExpressionRowContribution> expressionRowContributions = new(ReferenceEqualityComparer.Instance);
    private int expressionSelectedCount, expressionUnselectedCount, expressionNoCandidateCount, expressionUnavailableCount, expressionPendingCount;
    private string expressionSummary = "0件 / 選択 0件 / 未選択 0件 / 候補なし 0件";

    private readonly record struct ExpressionRowContribution(int Selected, int Unselected, int NoCandidate, int Unavailable, int Pending);

    public bool IsExpressionLoading { get => isExpressionLoading; private set { if (isExpressionLoading == value) return; isExpressionLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(ExpressionLoadingNotice)); OnPropertyChanged(nameof(CanEditExpressionRows)); UpdateCommands(); } }
    public string ExpressionLoadingNotice => IsExpressionLoading ? "表情一覧を読み込み中… 他のタブはそのまま操作できます。" : "";
    internal ExpressionPerformanceDiagnostics ExpressionPerformance => expressionPerformance.Snapshot();
    internal int PendingRelativeExpressionCount => expressionPendingCount;
    internal int UnavailableExpressionCount => expressionUnavailableCount;
    internal int SelectedExpressionCount => expressionSelectedCount;
    internal string CachedExpressionSummary => expressionSummary;

    private void RefreshNonExpressionState()
    {
        CloseExpressionTrialSession();
        if (intentInitialized) RefreshIntentWorkspace();
        RefreshV04(); HasError = false; Status = ""; OnPropertyChanged(nameof(SceneName));
    }

    internal void MarkExpressionVocabularyDirty()
    {
        expressionCandidateDirty = true; expressionCacheDirty = true;
        if (activeTask == "expression" && UsesRelativeExpressions) RequestExpressionLoad(true, true);
    }

    internal void EnterExpressionTask()
    {
        if (!UsesRelativeExpressions) return;
        SetVoiceFreshnessActive(true);
        RequestExpressionLoad(false, false);
    }

    internal void LeaveExpressionTask()
    {
        CancelExpressionLoad();
        expressionCacheDirty = true;
        SetVoiceFreshnessActive(false);
    }

    private void CancelExpressionLoad()
    {
        if (expressionLoadCancellation != null)
        {
            expressionLoadCancellation.Cancel(); expressionLoadCancellation.Dispose(); expressionLoadCancellation = null;
            expressionPerformance.CancelledLoads++;
        }
        expressionLoadGeneration++;
        IsExpressionLoading = false;
    }

    internal void RequestExpressionLoad(bool force, bool forceCandidates = false)
    {
        if (!UsesRelativeExpressions || timeline == null || voiceFreshnessDisposed) return;
        if (HasProtectedPendingVoiceWork() && expressionCacheTimeline != null && ReferenceEquals(expressionCacheTimeline, timeline))
        {
            SetVoiceFreshnessState(ExpressionRowsFreshness.StalePending); return;
        }
        if (!force && !expressionCacheDirty && ReferenceEquals(expressionCacheTimeline, timeline) && expressionHostFingerprint != null)
        {
            if (voiceFreshnessActive && watchedVoiceTimeline != null) return;
        }
        _ = LoadExpressionAsync(forceCandidates);
    }

    private async Task LoadExpressionAsync(bool forceCandidates)
    {
        if (timeline == null || !UsesRelativeExpressions) return;
        var current = timeline;
        expressionLoadCancellation?.Cancel(); expressionLoadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(); expressionLoadCancellation = cancellation;
        var generation = ++expressionLoadGeneration;
        IsExpressionLoading = true;

        ExpressionHostSnapshot snapshot;
        try { snapshot = CaptureExpressionHostSnapshot(generation, current, forceCandidates); }
        catch (Exception ex)
        {
            if (generation != expressionLoadGeneration) return;
            IsExpressionLoading = false; voiceFreshnessProblem = "表情一覧を読み込めませんでした: " + ex.GetBaseException().Message;
            OnPropertyChanged(nameof(ExpressionFreshnessNotice)); return;
        }

        try
        {
            var result = await Task.Run(() => ExpressionPreparation.Prepare(snapshot, cancellation.Token), cancellation.Token);
            if (cancellation.IsCancellationRequested || generation != expressionLoadGeneration || !ReferenceEquals(timeline, current) || activeTask != "expression")
            {
                expressionPerformance.StaleResultsDiscarded++; return;
            }
            PublishExpressionPrepared(result);
        }
        catch (OperationCanceledException)
        {
            if (generation == expressionLoadGeneration) expressionPerformance.CancelledLoads++;
        }
        catch (Exception ex)
        {
            if (generation != expressionLoadGeneration || cancellation.IsCancellationRequested) return;
            voiceFreshnessProblem = "表情一覧を更新できませんでした。現在の一覧は保持しています: " + ex.GetBaseException().Message;
            OnPropertyChanged(nameof(ExpressionFreshnessNotice));
        }
        finally
        {
            if (generation == expressionLoadGeneration)
            {
                expressionLoadCancellation?.Dispose(); expressionLoadCancellation = null; IsExpressionLoading = false;
            }
        }
    }

    private ExpressionHostSnapshot CaptureExpressionHostSnapshot(long generation, Timeline current, bool forceCandidates)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            throw new InvalidOperationException("表情一覧のYMM4 Snapshot取得はUIスレッドで実行する必要があります。");
        expressionPerformance.HostCaptures++;
        var items = new List<ExpressionCapturedItem>(current.Items.Count);
        var voices = new List<VoiceSnapshot>();
        foreach (var item in current.Items)
        {
            var remark = item.Remark ?? "";
            if (item is VoiceItem voice)
            {
                var snapshot = new VoiceSnapshot(voice, voice.CharacterName, voice.Frame, voice.Length, voice.Serif ?? "", voice.Layer);
                voices.Add(snapshot); items.Add(new(item, true, snapshot.Character, item.Group, remark));
            }
            else items.Add(new(item, false, "", item.Group, remark));
        }
        expressionPerformance.TimelineItemsCaptured += items.Count;
        expressionPerformance.FullVoiceReconciles++;
        ReconcileVoiceWatchers(voices);

        var candidateChanged = forceCandidates || expressionCandidateDirty || expressionCandidateCache == null;
        if (candidateChanged)
        {
            expressionCandidateCache = CaptureExpressionCandidates();
            expressionCandidateDirty = false;
        }
        return new(generation, current, voices, items, expressionCandidateCache ?? [], candidateChanged,
            ReferenceEquals(expressionCacheTimeline, current) ? expressionHostFingerprint : null,
            ReferenceEquals(expressionCacheTimeline, current) ? expressionPreparedVoices : [],
            ReferenceEquals(expressionCacheTimeline, current) ? expressionPreparedItems : []);
    }

    private IReadOnlyList<ExpressionCandidateDescriptor> CaptureExpressionCandidates()
    {
        expressionPerformance.CandidateCatalogBuilds++;
        var voiceType = IntentSelectionContext.TypeKey(typeof(VoiceItem));
        var catalog = IntentExpressionCatalog.Read(settings);
        var result = new List<ExpressionCandidateDescriptor>(catalog.Count);
        foreach (var template in catalog)
        {
            if (template.IntentSource is not { } source) continue;
            string? hash = null;
            try { source.Bundle.ValidateCurrent(); hash = IntentAssociationTag.Hash(source.Bundle); }
            catch (InvalidOperationException) { }
            var target = source.Palette.Target;
            var applies = target.TypeMatch == IntentTypeMatch.UniformType && target.MinimumCount <= 1 && target.MaximumCount >= 1 &&
                target.ItemTypeKeys.Contains(voiceType, StringComparer.Ordinal) && (target.CharacterName == null || target.CharacterName == template.Character);
            result.Add(new(template, template.Character, source.Palette.Id, source.Entry.LibraryEntryId, hash,
                source.Library.DisplayName, source.Palette.Name, template.Name, applies));
        }
        return result;
    }

    private void PublishExpressionPrepared(ExpressionPreparedResult result)
    {
        if (result.NoSemanticChange)
        {
            expressionHostFingerprint = result.Fingerprint; expressionCacheTimeline = result.Timeline;
            expressionPreparedVoices = Rows.Select(x => x.Target).ToArray(); expressionPreparedItems = result.Items; expressionCacheDirty = false; voiceFreshnessProblem = "";
            SetVoiceFreshnessState(ExpressionRowsFreshness.Current); return;
        }

        expressionPerformance.AssociationIndexBuilds++;
        expressionPerformance.AssociationItemsParsed += result.AssociationItemsParsed;
        expressionPerformance.CandidateKeyBuilds += result.CandidateKeysBuilt;
        expressionPerformance.LastPrepareMilliseconds = (long)result.PreparationElapsed.TotalMilliseconds;
        var old = Rows.ToDictionary(x => x.Target.Voice, ReferenceEqualityComparer.Instance);
        var next = new List<AssignmentRow>(result.Rows.Count);
        suppressExpressionApply = true; suppressExpressionRowEvents = true;
        try
        {
            for (var i = 0; i < result.Rows.Count; i++)
            {
                var prepared = result.Rows[i];
                if (!old.TryGetValue(prepared.Target.Voice, out var row))
                    row = new AssignmentRow(i + 1, prepared.Target, prepared.Choices, true);
                row.ApplyPrepared(i + 1, prepared.Target, prepared.Choices, prepared.Selected, prepared.UnavailableLabel, prepared.AssociationMatchesSelection);
                next.Add(row);
            }
            var keep = next.ToHashSet(ReferenceEqualityComparer.Instance);
            foreach (var row in Rows.Where(x => !keep.Contains(x))) row.PropertyChanged -= RowChanged;
            foreach (var row in next.Where(x => !Rows.Contains(x))) row.PropertyChanged += RowChanged;
            Rows.ReplaceAll(next);
            expressionPerformance.BatchCollectionPublishes++; expressionPerformance.FullRowPublishes++;
        }
        finally { suppressExpressionRowEvents = false; suppressExpressionApply = false; }

        expressionChoicesByCharacter = result.Rows.GroupBy(x => x.Target.Character, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First().Choices, StringComparer.Ordinal);
        expressionHostFingerprint = result.Fingerprint; expressionCacheTimeline = result.Timeline;
        expressionPreparedVoices = result.Rows.Select(x => x.Target).ToArray(); expressionPreparedItems = result.Items; expressionCacheDirty = false; voiceFreshnessProblem = "";
        AutomaticVoiceRebuildCount++;
        RebuildExpressionAggregates(); RememberVoiceRows(true);
        OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(ShowExpressionBatchPlace)); UpdateCommands();
    }

    internal void ApplyIncrementalVoiceChanges(IReadOnlyList<VoiceItem> voices)
    {
        if (!UsesRelativeExpressions || timeline == null || voices.Count == 0 || IsExpressionLoading) return;
        if (HasProtectedPendingVoiceWork()) { SetVoiceFreshnessState(ExpressionRowsFreshness.StalePending); return; }
        var byVoice = Rows.ToDictionary(x => x.Target.Voice, ReferenceEqualityComparer.Instance);
        foreach (var voice in voices)
        {
            if (!byVoice.TryGetValue(voice, out var row)) { RequestExpressionLoad(true); return; }
            var nextTarget = new VoiceSnapshot(voice, voice.CharacterName, voice.Frame, voice.Length, voice.Serif ?? "", voice.Layer);
            if (nextTarget.Character != row.Target.Character) { RequestExpressionLoad(true); return; }
            var choices = expressionChoicesByCharacter.GetValueOrDefault(nextTarget.Character) ?? row.Choices;
            var selected = choices.FirstOrDefault(x => SameChoiceIdentity(x, row.SelectedChoice));
            if (selected == null && row.SelectedChoice.Template == null && row.SelectedChoice.IsAvailable) selected = choices.FirstOrDefault(x => x.Template == null);
            suppressExpressionApply = true; suppressExpressionRowEvents = true;
            try { row.ApplyPrepared(row.No, nextTarget, choices, selected, selected == null && !row.SelectedChoice.IsAvailable ? row.SelectedChoice.Label : null, row.AssociationMatchesSelection); }
            finally { suppressExpressionRowEvents = false; suppressExpressionApply = false; }
            expressionPerformance.IncrementalVoiceReconciles++; expressionPerformance.IncrementalRowUpdates++;
        }
        var sorted = Rows.OrderBy(x => x.Target.Frame).ThenBy(x => x.Target.Layer).ToArray();
        suppressExpressionRowEvents = true;
        try
        {
            for (var i = 0; i < sorted.Length; i++) sorted[i].ApplyPrepared(i + 1, sorted[i].Target, sorted[i].Choices, sorted[i].SelectedChoice,
                sorted[i].SelectedChoice.IsAvailable ? null : sorted[i].SelectedChoice.Label, sorted[i].AssociationMatchesSelection);
            Rows.ReplaceAll(sorted); expressionPerformance.BatchCollectionPublishes++;
        }
        finally { suppressExpressionRowEvents = false; }
        expressionPreparedVoices = sorted.Select(x => x.Target).ToArray(); expressionHostFingerprint = null; expressionCacheDirty = true;
        RebuildExpressionAggregates(); RememberVoiceRows(true); OnPropertyChanged(nameof(Summary)); UpdateCommands();
    }

    private static bool SameChoiceIdentity(TemplateChoice left, TemplateChoice right)
    {
        if (left.Template == null || right.Template == null) return left.Template == null && right.Template == null && left.IsAvailable == right.IsAvailable;
        return ReferenceEquals(left.Template.Template, right.Template.Template) && ReferenceEquals(left.Template.Face, right.Template.Face) &&
            left.Template.Name == right.Template.Name && left.Template.Character == right.Template.Character;
    }

    internal void RefreshExpressionSynchronously(bool forceCandidates)
    {
        if (timeline == null) return;
        if (!UsesRelativeExpressions)
        {
            var catalog = TemplateCatalog.Read();
            SetRows(VoiceSnapshot.Capture(timeline).Select((x, i) => new AssignmentRow(i + 1, x, catalog, false)).ToArray());
            return;
        }
        var generation = ++expressionLoadGeneration;
        var snapshot = CaptureExpressionHostSnapshot(generation, timeline, forceCandidates);
        var result = ExpressionPreparation.Prepare(snapshot, CancellationToken.None);
        PublishExpressionPrepared(result);
    }

    private ExpressionRowContribution Contribution(AssignmentRow row)
    {
        var selected = row.SelectedChoice.Template != null ? 1 : 0;
        var unavailable = !row.SelectedChoice.IsAvailable ? 1 : 0;
        var noCandidate = !row.HasCandidates ? 1 : 0;
        var unselected = selected == 0 && noCandidate == 0 ? 1 : 0;
        var pending = UsesRelativeExpressions && row.SelectedChoice.Template != null && row.SelectedChoice.IsAvailable && !row.AssociationMatchesSelection ? 1 : 0;
        return new(selected, unselected, noCandidate, unavailable, pending);
    }

    private void RebuildExpressionAggregates()
    {
        expressionRowContributions.Clear(); expressionSelectedCount = expressionUnselectedCount = expressionNoCandidateCount = expressionUnavailableCount = expressionPendingCount = 0;
        foreach (var row in Rows)
        {
            var c = Contribution(row); expressionRowContributions[row] = c; AddContribution(c, 1);
        }
        PublishAggregateProperties();
    }

    private void UpdateExpressionAggregate(AssignmentRow row)
    {
        if (expressionRowContributions.TryGetValue(row, out var previous)) AddContribution(previous, -1);
        var next = Contribution(row); expressionRowContributions[row] = next; AddContribution(next, 1); PublishAggregateProperties();
    }

    private void AddContribution(ExpressionRowContribution c, int sign)
    {
        expressionSelectedCount += c.Selected * sign; expressionUnselectedCount += c.Unselected * sign; expressionNoCandidateCount += c.NoCandidate * sign;
        expressionUnavailableCount += c.Unavailable * sign; expressionPendingCount += c.Pending * sign;
    }

    private void PublishAggregateProperties()
    {
        expressionSummary = $"{Rows.Count}件 / 選択 {expressionSelectedCount}件 / 未選択 {expressionUnselectedCount}件 / 候補なし {expressionNoCandidateCount}件";
        OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(ShowExpressionBatchPlace));
    }
}
