using System.IO;

namespace Ymm4TemplatePlacer;

public enum PaletteKind { Character, Style }
public sealed record PaletteKindChoice(PaletteKind Value, string Label);
public sealed record PaletteDefinition(Guid Id, PaletteKind Kind, string Name, string? CharacterName, List<Guid> LibraryEntryIds)
{
    public LayerPolicy Layer { get; init; } = new();
}
public sealed record PaletteEntryView(Guid LibraryEntryId, LibraryEntry? Entry, string? PaletteCharacterName = null)
{
    public string DisplayName => Entry?.DisplayName ?? "⚠ 登録済みテンプレートが見つかりません";
    public string Status
    {
        get
        {
            if (Entry == null) return "このパレットから外すか、テンプレート管理で登録し直してください。";
            var resolved = TemplateResolver.Resolve(Entry);
            if (resolved.State != TemplateReferenceState.Resolved) return resolved.Message;
            var actual = resolved.Item == null ? null : ItemCharacters.Get(resolved.Item)?.Name;
            return PaletteCharacterName != null && ((Entry.CharacterName != null && Entry.CharacterName != PaletteCharacterName) ||
                (actual != null && actual != PaletteCharacterName)) ? "⚠ パレットとテンプレートのキャラクターが違います。" : "";
        }
    }
}
public sealed partial class PlacerSettings
{
    public List<PaletteDefinition> Palettes { get; set; } = [];
    public Guid? ManualCharacterPaletteId { get; set; }
    public Guid? ManualStylePaletteId { get; set; }
    public PaletteKind PaletteMode { get; set; }
    public CharacterLayerMode CharacterQuickDropMode { get; set; }
}
public static class PaletteSettings
{
    public static void Validate(PlacerSettings settings)
    {
        if (settings.Palettes == null || settings.Palettes.Count > 256 || !Enum.IsDefined(settings.PaletteMode) || !Enum.IsDefined(settings.CharacterQuickDropMode))
            throw new InvalidDataException("パレット設定が不正です。");
        if (settings.Palettes.Any(x => x == null || x.Id == Guid.Empty || !Enum.IsDefined(x.Kind) || string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 256 ||
            (x.Kind == PaletteKind.Character && string.IsNullOrWhiteSpace(x.CharacterName)) || (x.Kind == PaletteKind.Style && x.CharacterName != null) ||
            x.LibraryEntryIds == null || x.LibraryEntryIds.Count > 2048 || x.LibraryEntryIds.Any(id => id == Guid.Empty) || x.LibraryEntryIds.Distinct().Count() != x.LibraryEntryIds.Count || x.Layer == null) ||
            settings.Palettes.Select(x => x.Id).Distinct().Count() != settings.Palettes.Count ||
            settings.Palettes.Where(x => x.Kind == PaletteKind.Character).Select(x => x.CharacterName).Distinct(StringComparer.Ordinal).Count() != settings.Palettes.Count(x => x.Kind == PaletteKind.Character))
            throw new InvalidDataException("パレットID・キャラクター・登録項目が不正または重複しています。");
        foreach (var palette in settings.Palettes) palette.Layer.Validate();
        if ((settings.ManualCharacterPaletteId is Guid characterId && !settings.Palettes.Any(x => x.Id == characterId && x.Kind == PaletteKind.Character)) ||
            (settings.ManualStylePaletteId is Guid styleId && !settings.Palettes.Any(x => x.Id == styleId && x.Kind == PaletteKind.Style)))
            throw new InvalidDataException("手動パレットの参照が不正です。");
    }
}
