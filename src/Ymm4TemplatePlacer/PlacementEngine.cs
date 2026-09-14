using System.IO;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public static class PlacementEngine
{
    public const string Marker = "CWT_TPL:face";
    public static bool IsOwned(IItem item) => item is TachieFaceItem && item.Remark == Marker;
    public static void ValidateSnapshot(Timeline timeline, IReadOnlyList<VoiceSnapshot> snapshot)
    {
        var current = VoiceSnapshot.Capture(timeline);
        if (current.Count != snapshot.Count || current.Where((x, i) => !x.Equals(snapshot[i])).Any())
            throw new InvalidOperationException("音声一覧が現在のシーンと一致しません。［更新］を押してください。Excelを使う場合は再出力してください。");
    }
    public static TachieFaceItem CloneForVoice(VoiceSnapshot target, FaceTemplate choice)
    {
        if (!ItemSettings.Default.Templates.Contains(choice.Template) || choice.Template.Items.Count != 1 || !ReferenceEquals(choice.Template.Items[0], choice.Source))
            throw new InvalidOperationException("参照テンプレートが変更・削除されています。［更新］またはExcelの再出力を行ってください。");
        if (target.Frame < 0 || target.Length <= 0)
            throw new InvalidOperationException("音声の開始位置 / 長さが不正です。YMM4上で修正して更新してください。");
        var clone = choice.Source.GetClone() as TachieFaceItem
            ?? throw new InvalidOperationException("表情テンプレートの複製に失敗しました。");
        if (ReferenceEquals(clone, choice.Source) || clone.Character == null || !Equals(clone.Character, target.Voice.Character))
            throw new InvalidOperationException("表情テンプレートを正しいキャラクターの独立したアイテムとして複製できませんでした。");
        clone.Frame = target.Frame; clone.Length = target.Length;
        if (clone.Layer < 0) throw new InvalidOperationException("テンプレートのレイヤーが不正です。YMM4で登録し直してください。");
        clone.Group = 0; clone.Remark = Marker;
        return clone;
    }
    public static int Replace(Timeline timeline, UndoRedoManager undo, IReadOnlyList<AssignmentRow> rows)
    {
        ValidateSnapshot(timeline, rows.Select(x => x.Target).ToArray());
        var additions = rows.Where(x => x.SelectedChoice.Template != null)
            .Select(x => (IItem)CloneForVoice(x.Target, x.SelectedChoice.Template!)).ToArray();
        var plan = PlacementPlan.Create(timeline, additions);
        return plan.Commit(timeline, undo);
    }
}
