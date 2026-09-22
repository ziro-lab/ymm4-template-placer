namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private CancellationTokenSource? tachiePresetApplyCancellation;
    private long tachiePresetApplyGeneration;
    internal Task TachiePresetApplyCompletion { get; private set; } = Task.CompletedTask;

    private void CancelTachiePresetApply()
    {
        tachiePresetApplyGeneration++;
        tachiePresetApplyCancellation?.Cancel();
        tachiePresetApplyCancellation = null;
    }

    private void RequestImmediateTachiePresetChoice(AssignmentRow row)
    {
        if (suppressExpressionApply || !IsTachiePresetExpressionSource ||
            !ExpressionRowsMatchSource || IsExpressionLoading ||
            activeTask != "expression" || !Rows.Contains(row)) return;
        var choice = row.SelectedChoice;
        if (choice.IsCurrentOtherSource || choice.IsInvalidAssociation || !choice.IsAvailable) return;
        if (choice.TachiePreset is { Confidence: TachiePresetCapabilityLevel.Experimental })
        {
            CancelTachiePresetApply();
            HasError = false;
            Status = "この実験候補は状態を安定して確認できないため、候補確認のみです。タイムラインは変更していません。";
            OnPropertyChanged(nameof(Summary));
            UpdateCommands();
            return;
        }
        CancelTachiePresetApply();
        var request = tachiePresetApplyGeneration;
        var own = new CancellationTokenSource();
        tachiePresetApplyCancellation = own;
        TachiePresetApplyCompletion = ApplyImmediateTachiePresetChoiceAsync(row, choice, request, own);
    }

    private async Task ApplyImmediateTachiePresetChoiceAsync(
        AssignmentRow row, TemplateChoice requestedChoice, long request, CancellationTokenSource own)
    {
        var token = own.Token;
        var current = timeline;
        bool IsCurrentRequest() =>
            !token.IsCancellationRequested &&
            request == tachiePresetApplyGeneration &&
            ReferenceEquals(tachiePresetApplyCancellation, own) &&
            current != null && ReferenceEquals(timeline, current) &&
            activeTask == "expression" &&
            IsTachiePresetExpressionSource && ExpressionRowsMatchSource &&
            Rows.Contains(row) && SameRequestedChoice(row.SelectedChoice, requestedChoice);
        try
        {
            if (current == null || undo == null)
                throw new InvalidOperationException("YMM4の対象シーンまたは「元に戻す」に接続できません。");
            int changed;
            if (requestedChoice.TachiePreset is { } candidate)
            {
                var mutation = await TachiePresetExpressionMutation.CreateAsync(
                    current, Rows.ToArray(), row, RequireExpressionPreset(),
                    candidate, PresetTargetResolver, ExpressionSerialSeed(current), token);
                if (!IsCurrentRequest()) return;
                ObserveExpressionSerial(mutation.NextSerial);
                expressionTrialSession.Begin(current, undo, row.Target.Voice);
                changed = expressionTrialSession.ExecuteOwned(() =>
                {
                    if (!IsCurrentRequest()) throw new OperationCanceledException(token);
                    return ExecuteOwnedExpressionTimelineMutation(() =>
                        mutation.CommitWithinOpenRecord(current, token));
                });
            }
            else
            {
                var snapshots = Rows.Select(x => x.Target).ToArray();
                PlacementEngine.ValidateSnapshot(current, snapshots);
                var existing = ManagedExpressionReader.Read(current, row.Target.Voice);
                ManagedExpressionSafety.ValidatePresetState(existing.Bundle, token);
                var plan = PlacementPlan.Create(current, [], removals: existing.Bundle?.Members.ToArray() ?? []);
                if (!IsCurrentRequest()) return;
                if (plan.ChangeCount == 0) changed = 0;
                else
                {
                    expressionTrialSession.Begin(current, undo, row.Target.Voice);
                    changed = expressionTrialSession.ExecuteOwned(() =>
                    {
                        if (!IsCurrentRequest()) throw new OperationCanceledException(token);
                        PlacementEngine.ValidateSnapshot(current, snapshots);
                        var live = ManagedExpressionReader.Read(current, row.Target.Voice);
                        if (!ManagedExpressionSafety.Same(existing, live))
                            throw new InvalidOperationException("確認後に現在の関連表情が変わりました。削除していません。");
                        ManagedExpressionSafety.ValidatePresetState(live.Bundle, token);
                        return ExecuteOwnedExpressionTimelineMutation(() =>
                            plan.CommitWithinOpenRecord(current));
                    });
                }
            }
            if (!IsCurrentRequest()) return;
            HasError = false;
            Status = requestedChoice.TachiePreset != null
                ? $"「{requestedChoice.DisplayName}」を即時反映しました（{changed}変更）。"
                : changed == 0 ? "この音声には管理対象の関連表情がありません。"
                : "この音声の関連表情を外しました。";
            RestoreExpressionChoiceFromTimeline(row);
            if (changed != 0) QueueExpressionNavigation(row, refreshCurrentContent: true);
        }
        catch (OperationCanceledException) when (!IsCurrentRequest()) { }
        catch (Exception ex)
        {
            if (!IsCurrentRequest()) return;
            HasError = true;
            Status = "立ち絵プリセットを変更できませんでした: " + ex.GetBaseException().Message;
            RestoreExpressionChoiceFromTimeline(row);
        }
        finally
        {
            if (ReferenceEquals(tachiePresetApplyCancellation, own)) tachiePresetApplyCancellation = null;
            own.Dispose();
            OnPropertyChanged(nameof(Summary));
            UpdateCommands();
        }
    }

    private static bool SameRequestedChoice(TemplateChoice current, TemplateChoice requested)
    {
        if (requested.TachiePreset != null)
            return current.IsAvailable == requested.IsAvailable && current.TachiePreset == requested.TachiePreset;
        return !current.HasCandidate && current.IsAvailable &&
            !current.IsCurrentOtherSource && !current.IsInvalidAssociation;
    }
}
