using System.IO;
using System.Text.Json.Serialization;

namespace Ymm4TemplatePlacer;

public enum PaletteKind { Character, Style }
public sealed record PaletteKindChoice(PaletteKind Value, string Label);
// Non-semantic per-membership appearance; LibraryEntryIds remains the only order source.
public sealed record PaletteTileAppearance(string? DisplayAlias = null, IntentTileColor Color = IntentTileColor.Neutral, IntentTileShape Shape = IntentTileShape.Rounded)
{
    public static PaletteTileAppearance Default { get; } = new();
    public void Validate()
    {
        if (DisplayAlias?.Length > 128 || !Enum.IsDefined(Color) || !Enum.IsDefined(Shape))
            throw new InvalidDataException("タイルの表示設定が不正です。");
    }
}
public sealed record PaletteDefinition(Guid Id, PaletteKind Kind, string Name, string? CharacterName, List<Guid> LibraryEntryIds)
{
    public LayerPolicy Layer { get; init; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<Guid, PaletteTileAppearance>? TileAppearance { get; init; }
    public PaletteTileAppearance AppearanceFor(Guid id) => TileAppearance?.GetValueOrDefault(id) ?? PaletteTileAppearance.Default;
}
public sealed record PaletteEntryView(Guid LibraryEntryId, LibraryEntry? Entry, string? PaletteCharacterName = null, bool ShowSourceDetail = false)
{
    public string DisplayName => Entry?.DisplayName ?? "⚠ 登録済みテンプレートが見つかりません";
    public string SourceDetail => ShowSourceDetail && Entry != null ? Entry.Source.Name : "";
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
        foreach (var palette in settings.Palettes)
        {
            palette.Layer.Validate();
            if (palette.TileAppearance is not { } appearances) continue;
            if (appearances.Count > 2048 || appearances.Any(x => x.Key == Guid.Empty || !palette.LibraryEntryIds.Contains(x.Key) || x.Value == null))
                throw new InvalidDataException("汎用タイルの表示設定の参照が不正です。");
            foreach (var appearance in appearances.Values) appearance.Validate();
        }
        if ((settings.ManualCharacterPaletteId is Guid characterId && !settings.Palettes.Any(x => x.Id == characterId && x.Kind == PaletteKind.Character)) ||
            (settings.ManualStylePaletteId is Guid styleId && !settings.Palettes.Any(x => x.Id == styleId && x.Kind == PaletteKind.Style)))
            throw new InvalidDataException("手動パレットの参照が不正です。");
    }
}
