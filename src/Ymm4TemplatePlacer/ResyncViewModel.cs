using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private ActionCommand? resyncCommand;
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
    public string ResyncHint => !UsesRelativeExpressions && ExpressionPresetDirty ? "編集中の配置範囲を保存するか、編集を戻してください。" :
        !HasSelectedAssociation ? "タイムラインで関連付け済みの音声または表情を選択してください。「表情をまとめて」の行選択やクイック配置は対象外です。" :
        UsesRelativeExpressions ? "選択した関連演出を、保存済みのパレット設定でまとめて合わせ直します。欠落・コピーされたBundleは推測復元しません。" :
        "選択した関連表情だけを、現在の配置範囲で合わせ直します。関連のないアイテムは変更しません。";
    public ActionCommand ResyncCommand
    {
        get
        {
            if (resyncCommand != null) return resyncCommand;
            resyncCommand = new ActionCommand(_ => undo != null && settingsAvailable && (UsesRelativeExpressions || !ExpressionPresetDirty) && HasSelectedAssociation, _ => Guard(() => Resync()));
            resyncCommand.CanExecuteChanged += (_, _) => OnPropertyChanged(nameof(ResyncHint)); return resyncCommand;
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
    public ResyncPlan Resync()
    {
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
