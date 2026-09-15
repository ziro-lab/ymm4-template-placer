using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;

public enum IntentTypeMatch { UniformType, ExactMixedTypes }
public enum IntentAnchor { SelectedStart, SelectedEnd, SelectedCenter, SelectionRangeStart, SelectionRangeEnd, PairBoundary, RelatedStart, RelatedEnd }
public enum IntentDuration { Template, TargetSpan, Fixed, UntilRelated }
public enum IntentAlignment { StartAtAnchor, EndAtAnchor }
public enum IntentNeighbor { None, NextSameType, PreviousSameType, NextSameCharacter, PreviousSameCharacter, NextSameTypeAndCharacter, PreviousSameTypeAndCharacter }
public enum IntentFallback { CurrentTargetEnd, FixedDuration, TargetSpan, DoNotPlace }
public enum IntentNeighborEdge { Start, End }

public sealed record IntentTargetContext
{
    public List<string> ItemTypeKeys { get; init; } = [];
    public IntentTypeMatch TypeMatch { get; init; }
    public int MinimumCount { get; init; } = 1;
    public int MaximumCount { get; init; } = 1;
    public string? CharacterName { get; init; }
    public bool Matches(IntentSelectionContext context)
    {
        if (context.Selected.Count < MinimumCount || context.Selected.Count > MaximumCount) return false;
        var keys = context.Selected.Select(x => x.TypeKey).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var typeMatch = TypeMatch == IntentTypeMatch.UniformType
            ? keys.Length == 1 && ItemTypeKeys.Contains(keys[0], StringComparer.Ordinal)
            : keys.SequenceEqual(ItemTypeKeys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
        return typeMatch && (CharacterName == null || context.Selected.All(x => x.CharacterName == CharacterName));
    }
    public void Validate()
    {
        if (!Enum.IsDefined(TypeMatch) || ItemTypeKeys == null || ItemTypeKeys.Count is < 1 or > 32 ||
            ItemTypeKeys.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 512) ||
            ItemTypeKeys.Distinct(StringComparer.Ordinal).Count() != ItemTypeKeys.Count ||
            MinimumCount < 1 || MaximumCount < MinimumCount || MaximumCount > 2048 ||
            (TypeMatch == IntentTypeMatch.ExactMixedTypes && (ItemTypeKeys.Count < 2 || MinimumCount < ItemTypeKeys.Count)) ||
            (CharacterName != null && (string.IsNullOrWhiteSpace(CharacterName) || CharacterName.Length > 256)))
            throw new InvalidDataException("パレットの対象種類・選択数・キャラクター条件が不正です。");
    }
}

public sealed record IntentRelation
{
    public IntentAnchor Anchor { get; init; } = IntentAnchor.SelectedStart;
    public IntentDuration Duration { get; init; } = IntentDuration.TargetSpan;
    public IntentAlignment Alignment { get; init; }
    public IntentNeighbor Neighbor { get; init; }
    public IntentNeighborEdge NeighborEdge { get; init; }
    public IntentFallback Fallback { get; init; } = IntentFallback.CurrentTargetEnd;
    public int StartOffset { get; init; }
    public int EndOffset { get; init; }
    public int FixedDuration { get; init; } = 30;
    public int BoundaryTolerance { get; init; }
    public int? MaximumNeighborGap { get; init; }
    public RelativeLayerPolicy Layer { get; init; } = new();
    public void Validate()
    {
        if (!Enum.IsDefined(Anchor) || !Enum.IsDefined(Duration) || !Enum.IsDefined(Alignment) || !Enum.IsDefined(Neighbor) ||
            !Enum.IsDefined(NeighborEdge) || !Enum.IsDefined(Fallback) || FixedDuration < 1 ||
            BoundaryTolerance < 0 || MaximumNeighborGap < 0 || Layer == null ||
            ((Duration == IntentDuration.UntilRelated || Anchor is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd) && Neighbor == IntentNeighbor.None) ||
            (Duration == IntentDuration.UntilRelated && Alignment != IntentAlignment.StartAtAnchor))
            throw new InvalidDataException("パレットの配置関係が不正です。周囲参照と、見つからない場合の動作を確認してください。");
        Layer.Validate();
    }
}

public sealed record IntentEntry(Guid LibraryEntryId)
{
    // Small parameter differences only; a tile cannot replace the Palette's whole relation.
    public int StartOffsetDelta { get; init; }
    public int EndOffsetDelta { get; init; }
    public int? FixedDurationOverride { get; init; }
    public bool UseTemplateDuration { get; init; }
}

public sealed record IntentPalette(Guid Id, string Name, string Intent, IntentTargetContext Target, IntentRelation Relation, List<IntentEntry> Entries)
{
    public bool ExpressionCandidates { get; init; }
}

public sealed partial class PlacerSettings
{
    // Kept separate from the v0.4 envelope so legacy material can be preserved losslessly.
    public int IntentPaletteRevision { get; set; }
    public List<IntentPalette> IntentPalettes { get; set; } = [];
    public bool ExpressionBootstrapComplete { get; set; }
    public List<TemplateLocator> ImportedExpressionSources { get; set; } = [];
    // An explicit compatibility workspace, never an implicit fallback after a failed new operation.
    public bool LegacyWorkspace { get; set; }
}

public static class IntentPaletteSettings
{
    public static void Upgrade(PlacerSettings settings)
    {
        if (settings.IntentPaletteRevision != 0) return;
        if (settings.IntentPalettes == null || settings.IntentPalettes.Count != 0 || settings.ExpressionBootstrapComplete ||
            settings.ImportedExpressionSources == null || settings.ImportedExpressionSources.Count != 0)
            throw new InvalidDataException("版のない相対パレット設定は変換できません。元ファイルは保持しています。");
        // Old Library, Palette and Preset bodies remain intact. No inferred target types/relations.
        settings.IntentPaletteRevision = 1;
    }
    public static void Validate(PlacerSettings settings)
    {
        if (settings.IntentPaletteRevision is < 0 or > 1 || settings.IntentPalettes == null || settings.IntentPalettes.Count > 256 ||
            settings.ImportedExpressionSources == null || settings.ImportedExpressionSources.Count > 2048 ||
            settings.ImportedExpressionSources.Any(x => x == null || x.Name == null || x.PathJson == null) ||
            settings.ImportedExpressionSources.Distinct().Count() != settings.ImportedExpressionSources.Count ||
            settings.IntentPalettes.Any(x => x == null || x.Id == Guid.Empty || string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 256 ||
                string.IsNullOrWhiteSpace(x.Intent) || x.Intent.Length > 128 || x.Target == null || x.Relation == null || x.Entries == null ||
                x.Entries.Count > 2048 || x.Entries.Any(e => e == null || e.LibraryEntryId == Guid.Empty || e.FixedDurationOverride < 1) ||
                x.Entries.Select(e => e.LibraryEntryId).Distinct().Count() != x.Entries.Count) ||
            settings.IntentPalettes.Select(x => x.Id).Distinct().Count() != settings.IntentPalettes.Count)
            throw new InvalidDataException("未対応または不正な相対パレット設定です。元ファイルは保持しています。");
        if (settings.IntentPaletteRevision == 0 && (settings.IntentPalettes.Count != 0 || settings.ExpressionBootstrapComplete || settings.ImportedExpressionSources.Count != 0))
            throw new InvalidDataException("相対パレット設定の版がありません。");
        foreach (var palette in settings.IntentPalettes) { palette.Target.Validate(); palette.Relation.Validate(); }
    }
    public static IntentPalette Copy(IntentPalette palette) => JsonSerializer.Deserialize<IntentPalette>(JsonSerializer.Serialize(palette))!;
}
