using System.Collections.Immutable;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

public static class PlacementEngine
{
    public const string Marker = "CWT_TPL:face";
    public static bool IsOwned(IItem item) => item is TachieFaceItem && item.Remark == Marker;

    public static void ValidateSnapshot(Timeline timeline, IReadOnlyList<VoiceSnapshot> snapshot)
    {
        var current = timeline.Items.OfType<VoiceItem>().ToHashSet();
        if (current.Count != snapshot.Count || snapshot.Select(x => x.Voice).Distinct().Count() != snapshot.Count ||
            snapshot.Any(x => !current.Contains(x.Voice) || x.Voice.CharacterName != x.Character ||
                x.Voice.Frame != x.Frame || x.Voice.Length != x.Length || x.Voice.Layer != x.Layer || (x.Voice.Serif ?? "") != x.Serif))
            throw new InvalidOperationException("Voice一覧が現在のSceneと一致しません。［更新］を押してください。Excelを使う場合は再出力してください。");
    }

    public static TachieFaceItem CloneForVoice(VoiceSnapshot target, FaceTemplate template)
    {
        var items = template.Template.Items.ToArray();
        if (!ItemSettings.Default.Templates.Contains(template.Template) || template.Template.Name != template.Name ||
            items.Length != 1 || items[0] is not TachieFaceItem source ||
            !ReferenceEquals(source, template.Face) || target.Voice.Character == null || !Equals(source.Character, target.Voice.Character))
            throw new InvalidOperationException("参照Templateが変更・削除されています。［更新］またはExcelの再出力を行ってください。");
        if (target.Frame < 0 || target.Length <= 0 || (long)target.Frame + target.Length > int.MaxValue)
            throw new InvalidOperationException("VoiceのFrame / Lengthが不正です。YMM4上で修正して更新してください。");
        var clone = source.GetClone() as TachieFaceItem
            ?? throw new InvalidOperationException("表情Templateの複製に失敗しました。");
        if (ReferenceEquals(source, clone) || !Equals(clone.Character, target.Voice.Character))
            throw new InvalidOperationException("表情Templateを正しいCharacterの独立したItemとして複製できませんでした。");
        clone.Frame = target.Frame;
        clone.Length = target.Length;
        clone.Remark = Marker;
        clone.Group = 0;
        if (clone.Layer < 0) throw new InvalidOperationException("TemplateのLayerが不正です。YMM4で登録し直してください。");
        return clone;
    }

    public static int Replace(Timeline timeline, UndoRedoManager undo, IReadOnlyList<AssignmentRow> rows)
    {
        ValidateSnapshot(timeline, rows.Select(x => x.Target).ToArray());
        // Every validation and clone precedes the first Timeline mutation.
        var additions = rows.Where(x => x.SelectedChoice.Template != null)
            .Select(x => CloneForVoice(x.Target, x.SelectedChoice.Template!)).ToArray();
        var before = timeline.Items;
        var retained = before.Where(x => !IsOwned(x)).ToImmutableList();
        if (additions.Length == 0 && retained.Count == before.Count) return 0;
        ValidateCollisions(retained, additions);
        var next = retained.AddRange(additions);
        var selected = timeline.SelectedItems.Where(next.Contains).ToImmutableList();
        // The native immutable-list property is UndoRedo-aware. One state swap avoids
        // deleting groups or partially adding a batch. Never resolve/move manual items.
        undo.Record();
        timeline.Items = next;
        timeline.SelectedItems = selected;
        timeline.RefreshTimelineLengthAndMaxLayer();
        undo.Record();
        return additions.Length;
    }

    private static void ValidateCollisions(IEnumerable<IItem> retained, IReadOnlyList<TachieFaceItem> additions)
    {
        var newItems = additions.Cast<IItem>().ToHashSet();
        foreach (var layer in retained.Concat(additions).GroupBy(x => x.Layer))
        {
            long latestEnd = -1, latestNewEnd = -1;
            foreach (var item in layer.OrderBy(x => x.Frame))
            {
                var isNew = newItems.Contains(item);
                if ((isNew && item.Frame < latestEnd) || (!isNew && item.Frame < latestNewEnd))
                    throw new InvalidOperationException($"配置先が他のItemと重なります（Frame={item.Frame}, Layer={item.Layer}）。Templateを空いているLayerへ移して登録し直し、［更新］してください。Timelineは変更していません。");
                var end = (long)item.Frame + item.Length;
                latestEnd = Math.Max(latestEnd, end);
                if (isNew) latestNewEnd = Math.Max(latestNewEnd, end);
            }
        }
    }
}
