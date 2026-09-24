using System.Globalization;

namespace Ymm4TemplatePlacer;

/// <summary>
/// Read-only projection of the current Settings Draft.
/// It explains placement meaning for text/preview UI; it does not resolve Timeline context or perform placement.
/// </summary>
public abstract record PlacementBehaviorDescription
{
    public abstract string Summary { get; }
}

public sealed record TargetedPlacementBehaviorDescription(
    string[] TargetTypeNames,
    bool CharacterRestricted,
    string CharacterName,
    string MinimumCountText,
    string MaximumCountText,
    IntentAnchor Anchor,
    IntentAlignment Alignment,
    IntentDuration Duration,
    IntentNeighbor Neighbor,
    IntentNeighborEdge NeighborEdge,
    IntentFallback Fallback,
    string StartOffsetText,
    string EndOffsetText,
    string FixedDurationText,
    string MaximumNeighborGapText,
    string BoundaryToleranceText,
    LayerPlacementMode LayerMode,
    RelativeLayerDirection LayerDirection,
    string LayerOffsetText,
    string AbsoluteLayerText,
    string LayerMinimumText,
    string LayerMaximumText) : PlacementBehaviorDescription
{
    public int? MinimumCount => Parse(MinimumCountText);
    public int? MaximumCount => Parse(MaximumCountText);
    public int? StartOffset => Parse(StartOffsetText);
    public int? EndOffset => Parse(EndOffsetText);
    public int? FixedDurationFrames => ParsePositive(FixedDurationText);
    public int? MaximumNeighborGap => ParseOptional(MaximumNeighborGapText);
    public int? BoundaryTolerance => Parse(BoundaryToleranceText);
    public int? LayerOffset => ParsePositive(LayerOffsetText);
    public int? AbsoluteLayer => ParseNonNegative(AbsoluteLayerText);
    public int? LayerMinimum => ParseNonNegative(LayerMinimumText);
    public int? LayerMaximum => ParseNonNegative(LayerMaximumText);

    public override string Summary => PlacementBehaviorText.Targeted(this);

    private static int? Parse(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static int? ParsePositive(string text) => Parse(text) is int value && value > 0 ? value : null;
    private static int? ParseNonNegative(string text) => Parse(text) is int value && value >= 0 ? value : null;
    private static int? ParseOptional(string text) => string.IsNullOrWhiteSpace(text) ? null : Parse(text);
}

public sealed record GenericPlacementBehaviorDescription(
    bool UseTemplateLayer,
    LayerSearchMode SearchMode,
    string MinimumText,
    string MaximumText,
    string PreferredText) : PlacementBehaviorDescription
{
    public int? Minimum => ParseNonNegative(MinimumText);
    public int? Maximum => ParseNonNegative(MaximumText);
    public int? Preferred => ParseNonNegative(PreferredText);
    public override string Summary => PlacementBehaviorText.Generic(this);

    private static int? ParseNonNegative(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : null;
}

public static class PlacementBehaviorProjection
{
    public static TargetedPlacementBehaviorDescription Describe(IntentPaletteDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return new(
            draft.TypeChoices.Where(x => x.Selected).Select(x => x.Name).ToArray(),
            draft.CharacterRestricted,
            draft.CharacterName,
            draft.MinimumCount,
            draft.MaximumCount,
            draft.Anchor,
            draft.Alignment,
            draft.Duration,
            draft.Neighbor,
            draft.NeighborEdge,
            draft.Fallback,
            draft.StartOffset,
            draft.EndOffset,
            draft.FixedDuration,
            draft.MaximumGap,
            draft.BoundaryTolerance,
            draft.LayerMode,
            draft.Direction,
            draft.LayerOffset,
            draft.AbsoluteLayer,
            draft.LayerMinimum,
            draft.LayerMaximum);
    }

    public static GenericPlacementBehaviorDescription Describe(GenericSetDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return new(
            draft.UseTemplateLayer,
            draft.OccupiedBehavior ?? LayerSearchMode.Legacy,
            draft.Minimum,
            draft.Maximum,
            draft.Preferred);
    }
}

internal static class PlacementBehaviorText
{
    internal static string Targeted(TargetedPlacementBehaviorDescription description)
    {
        var target = description.TargetTypeNames.Length switch
        {
            0 => "対象アイテム",
            1 => description.TargetTypeNames[0],
            _ => string.Join("・", description.TargetTypeNames)
        };
        if (description.CharacterRestricted && !string.IsNullOrWhiteSpace(description.CharacterName))
            target = $"{description.CharacterName.Trim()}の{target}";

        string NeighborPhrase() => description.Neighbor switch
        {
            IntentNeighbor.NextSameType => "次の同じ種類のアイテム",
            IntentNeighbor.PreviousSameType => "前の同じ種類のアイテム",
            IntentNeighbor.NextSameCharacter => "次の同じキャラのアイテム",
            IntentNeighbor.PreviousSameCharacter => "前の同じキャラのアイテム",
            IntentNeighbor.NextSameTypeAndCharacter => "次の同じ種類・同じキャラのアイテム",
            IntentNeighbor.PreviousSameTypeAndCharacter => "前の同じ種類・同じキャラのアイテム",
            _ => "周囲のアイテム"
        };

        var anchor = description.Anchor switch
        {
            IntentAnchor.SelectedStart => "選択アイテムの開始",
            IntentAnchor.SelectedEnd => "選択アイテムの終了",
            IntentAnchor.SelectedCenter => "選択アイテムの中央",
            IntentAnchor.SelectionRangeStart => "選択範囲の開始",
            IntentAnchor.SelectionRangeEnd => "選択範囲の終了",
            IntentAnchor.PairBoundary => "選択した2アイテムの境界",
            IntentAnchor.RelatedStart => NeighborPhrase() + "の開始",
            IntentAnchor.RelatedEnd => NeighborPhrase() + "の終了",
            _ => "選択位置"
        };

        string Aligned(string length) => description.Alignment switch
        {
            IntentAlignment.StartAtAnchor => $"{anchor}から{length}で",
            IntentAlignment.CenterAtAnchor => $"演出の中央を{anchor}に合わせて{length}で",
            IntentAlignment.EndAtAnchor => $"{anchor}で終わるように{length}で",
            _ => $"{anchor}から{length}で"
        };

        var fixedDuration = description.FixedDurationFrames is int frames ? $"{frames}フレーム" : "指定した長さ";
        var timing = description.Duration switch
        {
            IntentDuration.Template => Aligned("テンプレートの長さ"),
            IntentDuration.TargetSpan => Aligned("選択対象と同じ長さ"),
            IntentDuration.Fixed => Aligned(fixedDuration),
            IntentDuration.UntilRelated => $"{anchor}から{NeighborPhrase()}の{(description.NeighborEdge == IntentNeighborEdge.Start ? "開始" : "終了")}まで",
            _ => anchor
        };

        var direction = description.LayerDirection == RelativeLayerDirection.Up ? "上" : "下";
        var absoluteLayer = description.AbsoluteLayer is int layerNumber ? layerNumber.ToString(CultureInfo.InvariantCulture) : "指定";
        var layer = description.LayerMode == LayerPlacementMode.Absolute
            ? $"レイヤー{absoluteLayer}を基準に配置します。塞がっていればさらに{direction}へ探します。"
            : $"対象より{direction}の空いているレイヤーへ配置します。塞がっていればさらに{direction}へ探します。";

        var needsNeighbor = description.Duration == IntentDuration.UntilRelated ||
            description.Anchor is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd;
        var fallback = needsNeighbor && description.Neighbor != IntentNeighbor.None ? description.Fallback switch
        {
            IntentFallback.CurrentTargetEnd => " 見つからなければ現在の対象の終了までにします。",
            IntentFallback.TargetSpan => " 見つからなければ現在の対象と同じ範囲にします。",
            IntentFallback.FixedDuration => $" 見つからなければ{fixedDuration}で配置します。",
            IntentFallback.DoNotPlace => " 見つからなければ配置しません。",
            _ => ""
        } : "";

        return $"{target}を選んだとき、{timing}、{layer}{fallback}".Trim();
    }

    internal static string Generic(GenericPlacementBehaviorDescription description) =>
        description.UseTemplateLayer
            ? "現在の再生位置から、テンプレートの長さ・レイヤーで配置します。選択アイテムには関連付けません。"
            : description.SearchMode == LayerSearchMode.Legacy
                ? $"現在の再生位置から、テンプレートの長さで配置します。レイヤー{description.MinimumText}〜{description.MaximumText}の空きから{description.PreferredText}を優先します。選択アイテムには関連付けません。"
                : $"現在の再生位置から、テンプレートの長さでレイヤー{description.PreferredText}へ配置します。塞がっていれば{(description.SearchMode == LayerSearchMode.DoNotPlace ? "配置しません" : description.SearchMode == LayerSearchMode.SearchUp ? "上（小さい番号）の空きを探します" : "下（大きい番号）の空きを探します")}。範囲は{description.MinimumText}〜{description.MaximumText}です。";
}
