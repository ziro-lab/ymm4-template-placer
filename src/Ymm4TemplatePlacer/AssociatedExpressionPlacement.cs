using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record AssociatedExpressionPlacement(PlacementPlan Plan, long NextSerial)
{
    public static AssociatedExpressionPlacement Create(Timeline timeline, IReadOnlyList<AssignmentRow> rows,
        ExpressionPreset preset, long nextSerial)
    {
        if (nextSerial < 1) throw new InvalidOperationException("関連付け連番の設定が不正です。");
        var serial = nextSerial;
        var updates = new List<PlannedItemUpdate>();
        long Allocate()
        {
            // A manually imported scene may contain later IDs than this installation's settings.
            foreach (var item in timeline.Items)
            {
                if (AssociationTag.Voice(item.Remark, out var targetId) == AssociationTagState.Valid && targetId >= serial)
                    serial = targetId == long.MaxValue ? throw new InvalidOperationException("関連付けIDを新規発行できません。上限に達しています。") : targetId + 1;
                if (AssociationTag.Source(item.Remark, out var source) == AssociationTagState.Valid && source!.Serial >= serial)
                    serial = source.Serial == long.MaxValue ? throw new InvalidOperationException("関連付けIDを新規発行できません。上限に達しています。") : source.Serial + 1;
            }
            if (serial == long.MaxValue) throw new InvalidOperationException("関連付けIDを新規発行できません。上限に達しています。");
            return serial++;
        }
        var additions = CharacterExpressionProfile.PlanItems(timeline, rows, preset, (row, clone) =>
        {
            var voice = row.Target.Voice;
            var state = AssociationTag.Voice(voice.Remark, out var id);
            if (state == AssociationTagState.Invalid)
                throw new InvalidOperationException("対象Voiceの関連付けタグが重複または不正です。自動修復せず配置を停止しました。");
            if (state == AssociationTagState.None)
            {
                id = Allocate();
                updates.Add(PlannedItemUpdate.RemarkOnly(voice, PluginRemarks.Append(voice.Remark, AssociationTag.TargetLine(id))));
            }
            else if (timeline.Items.OfType<VoiceItem>().Count(x => Equals(x.Character, voice.Character) &&
                AssociationTag.Voice(x.Remark, out var candidate) == AssociationTagState.Valid && candidate == id) != 1)
                throw new InvalidOperationException("IDとCharacterが一致するVoiceが複数あります。コピーされたIDを推測・修復せず配置を停止しました。");
            if (timeline.Items.Any(x => Equals(ItemCharacters.Get(x), voice.Character) &&
                AssociationTag.Source(x.Remark, out var source) == AssociationTagState.Valid && source!.Serial == id))
                throw new InvalidOperationException("このVoiceには関連する表情が既にあります。追加で置き換えず、関連表情またはVoiceを選択して［再同期］してください。");
            clone.Remark = PluginRemarks.Append(PluginRemarks.Append(PluginRemarks.WithoutAssociation(clone.Remark), PlacementEngine.Marker), AssociationTag.SourceLine(id));
        });
        return new(PlacementPlan.Create(timeline, additions, updates), serial);
    }
}
