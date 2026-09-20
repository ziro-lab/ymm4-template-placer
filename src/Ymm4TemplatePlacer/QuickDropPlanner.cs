using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record PlannedQuickDrop(IItem Item, PlacementPlan Plan);
public static class QuickDropPlanner
{
    public static PlannedQuickDrop Create(Timeline timeline, LibraryEntry entry, PaletteDefinition palette, CharacterLayerMode mode)
    {
        var clone = TemplateResolver.Clone(entry);
        Character? character = null;
        if (palette.Kind == PaletteKind.Character)
        {
            character = ItemCharacters.ResolveUnique(timeline, palette.CharacterName ?? "")
                ?? throw new InvalidOperationException("パレットのキャラクターを一意に特定できません。キャラクター名とテンプレート登録を確認してください。");
            var sourceCharacter = ItemCharacters.Get(clone);
            if ((sourceCharacter != null && !Equals(sourceCharacter, character)) || (entry.CharacterName != null && entry.CharacterName != character.Name))
                throw new InvalidOperationException("テンプレートのキャラクターは現在のキャラクターパレットと違います。自動でキャラクターを書き換えません。");
        }
        else mode = CharacterLayerMode.Base;
        clone.Frame = timeline.CurrentFrame;
        PlacementMath.ValidateSpan(clone.Frame, clone.Length);
        clone.Layer = LayerPlanner.Find(clone.Frame, clone.Length, clone.Layer, palette.Layer, mode, character, timeline.Items);
        clone.Remark = PluginRemarks.WithoutAssociation(clone.Remark);
        return new(clone, PlacementPlan.Create(timeline, [clone]));
    }
}
