using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public static class PlacementEngine
{
    public const string Marker = "CWT_TPL:face";
    public static bool IsGenerated(IItem item) => item is TachieFaceItem && (item.Remark ?? "").Split('\n').Any(x => x.TrimEnd('\r') == Marker);

    public static void ValidateSnapshot(Timeline timeline, IReadOnlyList<VoiceSnapshot> snapshot)
    {
        var current = timeline.Items.OfType<VoiceItem>().ToHashSet();
        if (current.Count != snapshot.Count || snapshot.Select(x => x.Voice).Distinct().Count() != snapshot.Count ||
            snapshot.Any(x => !current.Contains(x.Voice) || x.Voice.CharacterName != x.Character ||
                x.Voice.Frame != x.Frame || x.Voice.Length != x.Length || x.Voice.Layer != x.Layer || (x.Voice.Serif ?? "") != x.Serif))
            throw new InvalidOperationException("音声一覧が現在のシーンと一致しません。［シーン更新］を押してください。Excelを使う場合は再出力してください。");
    }

    public static TachieFaceItem CloneForVoice(VoiceSnapshot target, FaceTemplate template)
    {
        var items = template.Template.Items.ToArray();
        if (!ItemSettings.Default.Templates.Contains(template.Template) || template.Template.Name != template.Name ||
            items.Length != 1 || items[0] is not TachieFaceItem source ||
            !ReferenceEquals(source, template.Face) || target.Voice.Character == null || !Equals(source.Character, target.Voice.Character))
            throw new InvalidOperationException("参照テンプレートが変更・削除されています。［シーン更新］またはExcelの再出力を行ってください。");
        if (target.Frame < 0 || target.Length <= 0 || (long)target.Frame + target.Length > int.MaxValue)
            throw new InvalidOperationException("音声の開始位置 / 長さが不正です。YMM4上で修正して［シーン更新］してください。");
        var clone = source.GetClone() as TachieFaceItem
            ?? throw new InvalidOperationException("表情テンプレートの複製に失敗しました。");
        if (ReferenceEquals(source, clone) || !Equals(clone.Character, target.Voice.Character))
            throw new InvalidOperationException("表情テンプレートを正しいキャラクターの独立したアイテムとして複製できませんでした。");
        clone.Frame = target.Frame;
        clone.Length = target.Length;
        clone.Remark = string.IsNullOrEmpty(source.Remark) ? Marker : source.Remark + "\n" + Marker;
        clone.Group = 0;
        if (clone.Layer < 0) throw new InvalidOperationException("テンプレートのレイヤーが不正です。YMM4で登録し直してください。");
        return clone;
    }

    public static int Add(Timeline timeline, UndoRedoManager undo, IReadOnlyList<AssignmentRow> rows, ExpressionPreset? preset = null) =>
        CharacterExpressionProfile.Create(timeline, rows, preset ?? ExpressionPreset.Default).Commit(timeline, undo);
}
