using System.Windows.Media;
using System.Windows;

namespace Ymm4TemplatePlacer;

// Presentation only. Exactly one existing backend is wrapped; no persisted union,
// synthesized relative Profile, second order list or placement geometry is introduced.
public sealed record IntentSetChoice
{
    public IntentPalette? Targeted { get; }
    public PaletteDefinition? Generic { get; }
    public Guid Id => Targeted?.Id ?? Generic!.Id;
    public string Label { get; }
    public IntentSetChoice(IntentPalette palette, string label) { Targeted = palette; Label = label; }
    public IntentSetChoice(PaletteDefinition palette, string label) { Generic = palette; Label = label; }
}
public sealed record IntentTileChoice
{
    public Guid PaletteId { get; }
    public Guid LibraryEntryId { get; }
    public IntentEntry? TargetedEntry { get; }
    public bool IsGeneric => TargetedEntry == null;
    public string Label { get; }
    public string Detail { get; }
    public bool Available { get; }
    public PaletteTileAppearance? GenericAppearance { get; }
    public IntentTileColor Color => TargetedEntry?.Color ?? GenericAppearance?.Color ?? IntentTileColor.Neutral;
    public IntentTileShape Shape => TargetedEntry?.Shape ?? GenericAppearance?.Shape ?? IntentTileShape.Rounded;
    public CornerRadius Radius => IntentTileAppearance.Radius(Shape);
    public Brush Accent => IntentTileAppearance.Accent(Color);
    public string AppearanceDescription => $"{Detail}\n色ラベル: {IntentTileAppearance.ColorName(Color)} / 形: {IntentTileAppearance.ShapeName(Shape)}\nクリックで配置・ドラッグで並び替え・右クリックで編集";
    public IntentTileChoice(Guid paletteId, IntentEntry entry, string label, string detail, bool available)
    { PaletteId = paletteId; LibraryEntryId = entry.LibraryEntryId; TargetedEntry = entry; Label = label; Detail = detail; Available = available; }
    public IntentTileChoice(Guid paletteId, Guid libraryEntryId, string label, string detail, bool available, PaletteTileAppearance? appearance = null)
    { GenericAppearance = appearance; PaletteId = paletteId; LibraryEntryId = libraryEntryId; Label = label; Detail = detail; Available = available; }
}
