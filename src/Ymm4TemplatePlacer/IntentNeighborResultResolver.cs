namespace Ymm4TemplatePlacer;

/// <summary>
/// Placement-only multi-result projection for Neighbor ambiguity.
/// Strict callers continue to use IntentRelationResolver.Resolve.
/// </summary>
internal static class IntentNeighborResultResolver
{
    public const int MaximumDistinctResults = 32;

    public static IReadOnlyList<ResolvedIntentTime> Resolve(
        IntentSelectionContext context,
        IntentRelation relation,
        IntentEntry entry,
        int templateSpan)
    {
        relation.Validate();
        if (context.Selected.Count == 0 || templateSpan < 1)
            throw new InvalidOperationException("基準アイテムを選択してください。");

        var duration = entry.UseTemplateDuration
            ? IntentDuration.Template
            : entry.FixedDurationOverride.HasValue
                ? IntentDuration.Fixed
                : relation.Duration;
        var fixedDuration = entry.FixedDurationOverride ?? relation.FixedDuration;
        if (fixedDuration < 1)
            throw new InvalidOperationException("演出の長さは1フレーム以上にしてください。");

        var needsNeighbor = duration == IntentDuration.UntilRelated ||
            relation.Anchor is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd;
        if (!needsNeighbor)
            return [IntentRelationResolver.Resolve(context, relation, entry, templateSpan)];

        var startOffset = (long)relation.StartOffset + entry.StartOffsetDelta;
        var endOffset = (long)relation.EndOffset + entry.EndOffsetDelta;
        var neighbors = FindNearest(context, relation);
        if (neighbors.Count == 0)
            return [ResolveOne(context, relation, duration, fixedDuration, templateSpan, startOffset, endOffset, null)];

        var distinct = new List<ResolvedIntentTime>();
        foreach (var neighbor in neighbors)
        {
            var result = ResolveOne(context, relation, duration, fixedDuration, templateSpan, startOffset, endOffset, neighbor);
            if (distinct.Contains(result)) continue;
            distinct.Add(result);
            if (distinct.Count > MaximumDistinctResults)
                throw new InvalidOperationException($"同じ順位の周囲候補から{MaximumDistinctResults}通りを超える配置結果が生じます。安全のため配置していません。");
        }
        return distinct.AsReadOnly();
    }

    private static IReadOnlyList<IntentContextItem> FindNearest(IntentSelectionContext context, IntentRelation relation)
    {
        var sameType = relation.Neighbor is IntentNeighbor.NextSameType or IntentNeighbor.PreviousSameType or
            IntentNeighbor.NextSameTypeAndCharacter or IntentNeighbor.PreviousSameTypeAndCharacter;
        var sameCharacter = relation.Neighbor is IntentNeighbor.NextSameCharacter or IntentNeighbor.PreviousSameCharacter or
            IntentNeighbor.NextSameTypeAndCharacter or IntentNeighbor.PreviousSameTypeAndCharacter;
        if ((sameType && context.UniformType == null) || (sameCharacter && context.UniformCharacter == null))
            throw new InvalidOperationException("周囲参照に必要なアイテム種類・キャラクターが選択内で一致していません。");

        var previous = relation.Neighbor is IntentNeighbor.PreviousSameType or IntentNeighbor.PreviousSameCharacter or
            IntentNeighbor.PreviousSameTypeAndCharacter;
        var selected = context.Selected.Select(x => x.Item).ToHashSet();
        var pivot = previous ? context.Selected.Min(x => x.Frame) : context.Selected.Max(x => x.Frame);
        var candidates = context.Scene.Where(x =>
            !selected.Contains(x.Item) &&
            (previous ? x.Frame < pivot : x.Frame > pivot) &&
            (!sameType || x.TypeKey == context.UniformType) &&
            (!sameCharacter || x.CharacterName == context.UniformCharacter)).ToArray();
        if (candidates.Length == 0) return [];

        var nearestFrame = previous ? candidates.Max(x => x.Frame) : candidates.Min(x => x.Frame);
        var nearest = candidates.Where(x => x.Frame == nearestFrame);

        if (relation.MaximumNeighborGap.HasValue)
        {
            var maximum = relation.MaximumNeighborGap.Value;
            nearest = nearest.Where(x =>
            {
                var gap = previous ? (long)context.Start - x.End : x.Frame - context.End;
                return gap <= maximum;
            });
        }

        return nearest
            .OrderBy(x => x.End)
            .ThenBy(x => x.Layer)
            .ThenBy(x => x.TypeKey, StringComparer.Ordinal)
            .ToArray();
    }

    private static ResolvedIntentTime ResolveOne(
        IntentSelectionContext context,
        IntentRelation relation,
        IntentDuration duration,
        int fixedDuration,
        int templateSpan,
        long startOffset,
        long endOffset,
        IntentContextItem? neighbor)
    {
        if (neighbor == null)
        {
            if (relation.Fallback == IntentFallback.DoNotPlace) return ResolvedIntentTime.NotPlaced;
            if (relation.Fallback == IntentFallback.TargetSpan)
                return Span(context.Start + startOffset, context.End + endOffset);
            if (relation.Fallback == IntentFallback.FixedDuration)
                return Span(context.Start + startOffset, context.Start + startOffset + fixedDuration + endOffset);
            if (duration == IntentDuration.UntilRelated)
            {
                var fallbackAnchor = relation.Anchor is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd
                    ? context.Start
                    : Anchor(context, relation, null);
                return Span(fallbackAnchor + startOffset, context.End + endOffset);
            }
        }

        var anchor = neighbor == null ? context.End : Anchor(context, relation, neighbor);
        long length = duration switch
        {
            IntentDuration.Template => templateSpan,
            IntentDuration.TargetSpan => context.End - context.Start,
            IntentDuration.Fixed => fixedDuration,
            _ => 0
        };
        if (duration == IntentDuration.UntilRelated)
        {
            var end = relation.NeighborEdge == IntentNeighborEdge.Start ? neighbor!.Frame : neighbor!.End;
            return Span(anchor + startOffset, end + endOffset);
        }

        var start = relation.Alignment switch
        {
            IntentAlignment.StartAtAnchor => anchor,
            IntentAlignment.CenterAtAnchor => anchor - length / 2,
            IntentAlignment.EndAtAnchor => anchor - length,
            _ => throw new InvalidOperationException("配置の揃え方が不正です。")
        };
        return Span(start + startOffset, start + length + endOffset);
    }

    private static long Anchor(IntentSelectionContext context, IntentRelation relation, IntentContextItem? neighbor) => relation.Anchor switch
    {
        IntentAnchor.SelectedStart when context.Selected.Count == 1 => context.Start,
        IntentAnchor.SelectedEnd when context.Selected.Count == 1 => context.End,
        IntentAnchor.SelectedCenter when context.Selected.Count == 1 => context.Start + (context.End - context.Start) / 2,
        IntentAnchor.SelectionRangeStart => context.Start,
        IntentAnchor.SelectionRangeEnd => context.End,
        IntentAnchor.PairBoundary => Boundary(context, relation.BoundaryTolerance),
        IntentAnchor.RelatedStart when neighbor != null => neighbor.Frame,
        IntentAnchor.RelatedEnd when neighbor != null => neighbor.End,
        _ => throw new InvalidOperationException("選択数と基準位置が合いません。複数選択用には選択範囲または境界を設定してください。")
    };

    private static long Boundary(IntentSelectionContext context, int tolerance)
    {
        if (context.Selected.Count != 2 || context.Selected[0].Frame == context.Selected[1].Frame)
            throw new InvalidOperationException("前後が一意な2アイテムを選択してください。同じ開始位置の境界は推測しません。");
        var cut = context.Selected[0].End;
        if (Math.Abs(context.Selected[1].Frame - cut) > tolerance)
            throw new InvalidOperationException("選択した2アイテムの隙間・重なりが、境界の許容差を超えています。");
        return cut;
    }

    private static ResolvedIntentTime Span(long start, long end)
    {
        if (start < 0 || end > int.MaxValue || end <= start || start > int.MaxValue || end - start > int.MaxValue)
            throw new InvalidOperationException("相対配置の開始・終了が範囲外です。オフセットと周囲参照を確認してください。");
        return new(false, (int)start, (int)(end - start));
    }
}
