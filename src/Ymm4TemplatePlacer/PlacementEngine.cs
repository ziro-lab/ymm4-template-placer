using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public static class PlacementEngine
{
    public const string Marker = "CWT_TPL:face";

    public static TachieFaceItem CloneForVoice(VoiceSnapshot target, FaceTemplate template)
    {
        var items = template.Template.Items.ToArray();
        if (!ItemSettings.Default.Templates.Contains(template.Template) || items.Length != 1 ||
            items[0] is not TachieFaceItem source || !Equals(source.Character, target.Voice.Character))
            throw new InvalidOperationException("参照Templateが変更・削除されています。更新またはExcelの再出力を行ってください。");
        if (target.Frame < 0 || target.Length <= 0 || (long)target.Frame + target.Length > int.MaxValue)
            throw new InvalidOperationException("VoiceのFrame / Lengthが不正です。YMM4上で修正して更新してください。");
        var clone = source.GetClone() as TachieFaceItem
            ?? throw new InvalidOperationException("表情Templateの複製に失敗しました。");
        if (ReferenceEquals(source, clone))
            throw new InvalidOperationException("独立したTemplateの複製を取得できませんでした。");
        clone.Frame = target.Frame;
        clone.Length = target.Length;
        clone.Remark = Marker;
        clone.Group = 0;
        return clone;
    }

    // P3 integration primitive; the all-assignment replacement transaction is added after the native API proof.
    public static void AddPrepared(Timeline timeline, UndoRedoManager undo, IReadOnlyList<TachieFaceItem> items)
    {
        foreach (var item in items)
            if (!timeline.TryAddItems([item], item.Frame, item.Layer))
                throw new InvalidOperationException("YMM4が配置を拒否しました。TemplateのLayerと重なりを確認してください。");
        timeline.RefreshTimelineLengthAndMaxLayer();
        undo.Record();
    }
}
