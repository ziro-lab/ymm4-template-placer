using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private ActionCommand? resyncCommand;
    public ActionCommand ResyncCommand => resyncCommand ??= new ActionCommand(
        _ => timeline != null && undo != null && timeline.SelectedItems.Count > 0 && settingsAvailable && !ExpressionPresetDirty,
        _ => Guard(() => Resync()));

    partial void RefreshSelectionPlacement()
    {
        resyncCommand?.RaiseCanExecuteChanged();
        RefreshSelectionProfiles();
    }
    partial void RefreshSelectionProfiles();

    private int PlaceAssociatedExpression(ExpressionPreset preset)
    {
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
        var staged = AssociatedExpressionPlacement.Create(current, Rows.ToArray(), preset, settings.NextAssociationId);
        // Persist the serial reservation first. A failure leaves all Timeline Items and tags untouched.
        // Undo never rewinds the serial counter; harmless gaps are preferable to reusing an ID.
        if (staged.NextSerial != settings.NextAssociationId) EditSettings(next => next.NextAssociationId = staged.NextSerial);
        staged.Plan.Commit(current, undo);
        return staged.Plan.Count;
    }
    public ResyncPlan Resync()
    {
        var current = RequireTimeline();
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。");
        if (current.SelectedItems.Count == 0)
            throw new InvalidOperationException("YMM4のタイムラインで、関連表情または音声を選択してください。表情一覧の行選択は対象ではありません。");
        var preset = RequireExpressionPreset();
        var result = ResyncPlan.Create(current, preset);
        result.Plan.Commit(current, undo);
        HasError = false;
        Status = $"プリセット「{preset.Name}」で表情を再同期: {result.Plan.UpdateCount}件更新 / {result.Unchanged}件変更なし / {result.Skipped.Count}件スキップ / 関連なし{result.Ignored}件。" +
            (result.Skipped.Count == 0 ? "" : " " + string.Join(" / ", result.Skipped.Take(3))) +
            " 更新分は「元に戻す」1回で戻せます。テンプレートのレイヤーを使う設定では、現在のレイヤーを維持します。";
        UpdateCommands();
        return result;
    }
}
