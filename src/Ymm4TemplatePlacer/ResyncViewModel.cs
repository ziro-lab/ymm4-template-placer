using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private ActionCommand? resyncCommand;
    private bool resyncExecuting;
    private bool HasSelectedAssociation
    {
        get
        {
            var current = timeline;
            return current != null && current.SelectedItems.Any(x => current.Items.Contains(x) &&
                (x is VoiceItem && AssociationTag.Voice(x.Remark, out _) == AssociationTagState.Valid ||
                 x is TachieFaceItem && AssociationTag.Source(x.Remark, out _) == AssociationTagState.Valid ||
                 UsesRelativeExpressions && IntentAssociationTag.Read(x.Remark, out _) != AssociationTagState.None));
        }
    }
    public string ResyncHint => !UsesRelativeExpressions && IsTachiePresetExpressionSource
        ? "旧配置範囲の再同期はテンプレート表示で対象を確認してください。"
        : !UsesRelativeExpressions && ExpressionPresetDirty
            ? "編集中の配置範囲を保存するか、編集を戻してください。"
            : !HasSelectedAssociation
                ? "タイムラインで関連付け済みの音声または表情を選択してください。「表情をまとめて」の行選択やクイック配置は対象外です。"
                : UsesRelativeExpressions
                    ? "選択した関連演出を、保存済みのSet配置設定でまとめて合わせ直します。Template/Presetとも内容は触らず、位置・長さ・レイヤーだけを再計算します。"
                    : "選択した関連表情だけを、現在の配置範囲で合わせ直します。関連のないアイテムは変更しません。";
    public ActionCommand ResyncCommand
    {
        get
        {
            if (resyncCommand != null) return resyncCommand;
            resyncCommand = new ActionCommand(
                _ => !resyncExecuting && undo != null && settingsAvailable &&
                    (UsesRelativeExpressions || (IsTemplateExpressionSource && !ExpressionPresetDirty)) &&
                    HasSelectedAssociation,
                _ =>
                {
                    if (UsesRelativeExpressions) ExecuteRelativeResyncCommand();
                    else Guard(() => Resync());
                });
            resyncCommand.CanExecuteChanged += (_, _) => OnPropertyChanged(nameof(ResyncHint));
            return resyncCommand;
        }
    }
    partial void RefreshSelectionPlacement() { resyncCommand?.RaiseCanExecuteChanged(); RefreshSelectionProfiles(); }
    partial void RefreshSelectionProfiles();
    private int PlaceAssociatedExpression(ExpressionPreset preset)
    {
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
        var staged = AssociatedExpressionPlacement.Create(current, Rows.ToArray(), preset, settings.NextAssociationId);
        if (staged.NextSerial != settings.NextAssociationId) EditSettings(next => next.NextAssociationId = staged.NextSerial);
        staged.Plan.Commit(current, undo); return staged.Plan.Count;
    }
    private async void ExecuteRelativeResyncCommand()
    {
        if (resyncExecuting) return;
        resyncExecuting = true;
        resyncCommand?.RaiseCanExecuteChanged();
        try
        {
            await ResyncAsync();
        }
        catch (OperationCanceledException)
        {
            HasError = false;
            Status = "画面・作業タブまたは対象シーンが変わったため、再同期を中止しました。必要ならもう一度実行してください。";
            UpdateCommands();
        }
        catch (Exception ex)
        {
            HasError = true;
            Status = "関連演出を再同期できませんでした: " + ex.GetBaseException().Message;
            UpdateCommands();
        }
        finally
        {
            resyncExecuting = false;
            resyncCommand?.RaiseCanExecuteChanged();
        }
    }

    internal async Task<ResyncPlan> ResyncAsync(CancellationToken token = default)
    {
        using var lifetime = BeginAsyncOperation(token);
        CloseExpressionTrialSession();
        var current = RequireTimeline();
        var manager = undo ?? throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
        if (current.SelectedItems.Count == 0)
            throw new InvalidOperationException("YMM4のタイムラインで、関連表情または音声を選択してください。「表情をまとめて」の行選択は対象ではありません。");
        if (!UsesRelativeExpressions)
            return Resync();

        var selected = current.SelectedItems.ToArray();
        var settingsSnapshot = settings;
        var legacyAndTemplate = IntentAssociationResync.Create(
            current, settingsSnapshot, CurrentExpressionPreset);
        var registered = await RegisteredPresetAssociationResync.CreateAsync(
            current,
            settingsSnapshot,
            PresetTargetResolver,
            selected,
            lifetime.Token);

        lifetime.Validate();
        if (!ReferenceEquals(settings, settingsSnapshot) ||
            !ReferenceEquals(current, timeline) ||
            !current.SelectedItems.SequenceEqual(selected, ReferenceEqualityComparer.Instance))
            throw new InvalidOperationException("再同期の準備中に設定・シーンまたは選択が変わりました。何も変更していません。");

        legacyAndTemplate.ValidateCurrent(current, settings);
        registered.ValidateCurrent(current, settings, lifetime.Token);

        lifetime.Validate();
        var combinedPlan = PlacementPlan.Combine(
            current,
            [legacyAndTemplate.Result.Plan, registered.Result.Plan]);
        var result = new ResyncPlan(
            combinedPlan,
            legacyAndTemplate.Result.Unchanged + registered.Result.Unchanged,
            legacyAndTemplate.Result.Ignored + registered.Result.Ignored,
            legacyAndTemplate.Result.Skipped.Concat(registered.Result.Skipped).ToArray());

        lifetime.Validate();
        combinedPlan.Commit(current, manager);
        HasError = false;
        Status = $"関連演出を再同期: {result.Plan.UpdateCount}件更新 / {result.Unchanged}件変更なし / {result.Skipped.Count}件スキップ。" +
            (result.Skipped.Count == 0 ? "" : " " + string.Join(" / ", result.Skipped.Take(3))) +
            " 更新分は元に戻す1回で戻せます。";
        keepPartialStatus = result.Skipped.Count > 0;
        UpdateCommands();
        return result;
    }

    public ResyncPlan Resync()
    {
        RequireTemplateExpressionSource();
        CloseExpressionTrialSession();
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
        if (current.SelectedItems.Count == 0) throw new InvalidOperationException("YMM4のタイムラインで、関連表情または音声を選択してください。「表情をまとめて」の行選択は対象ではありません。");
        if (UsesRelativeExpressions)
        {
            var staged = IntentAssociationResync.Create(current, settings, CurrentExpressionPreset);
            var result = staged.Commit(current, undo, settings);
            HasError = false; Status = $"関連演出を再同期: {result.Plan.UpdateCount}件更新 / {result.Unchanged}件変更なし / {result.Skipped.Count}件スキップ。" +
                (result.Skipped.Count == 0 ? "" : " " + string.Join(" / ", result.Skipped.Take(3))) + " 更新分は元に戻す1回で戻せます。";
            keepPartialStatus = result.Skipped.Count > 0; UpdateCommands(); return result;
        }
        var preset = RequireExpressionPreset(); var old = ResyncPlan.Create(current, preset); old.Plan.Commit(current, undo);
        HasError = false;
        Status = $"プリセット「{preset.Name}」で表情を再同期: {old.Plan.UpdateCount}件更新 / {old.Unchanged}件変更なし / {old.Skipped.Count}件スキップ / 関連なし{old.Ignored}件。" +
            (old.Skipped.Count == 0 ? "" : " " + string.Join(" / ", old.Skipped.Take(3))) +
            " 更新分は「元に戻す」1回で戻せます。テンプレートのレイヤーを使う設定では、現在のレイヤーを維持します。";
        keepPartialStatus = old.Skipped.Count > 0; UpdateCommands(); return old;
    }
}
