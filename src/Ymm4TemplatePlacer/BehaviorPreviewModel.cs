using System.Globalization;

namespace Ymm4TemplatePlacer;

public enum PreviewBlockKind { Target, Neighbor, Placed }
public sealed record PreviewBlock(PreviewBlockKind Kind, string Label, double Start, double End, int Row);

/// <summary>Illustrative coordinates only. No Timeline, source lookup, occupancy, or writes.</summary>
public sealed record BehaviorPreviewModel(
    IReadOnlyList<PreviewBlock> Blocks, IReadOnlyList<double> Guides,
    string Caption, string Scope, string LayerHint, string FallbackHint, string Notice,
    bool AbsoluteLayer, bool HasDiagram, double Minimum, double Maximum)
{
    // Example spans, never Frame values from the user's project. One shared affine
    // transform preserves coincident endpoints, equal lengths and alignment.
    internal const double TargetStart = 200;
    internal const double TargetLength = 120;
    internal const double SourceLength = 72;
    public string AccessibleText => string.Join("。", new[] { Scope, Caption, LayerHint, FallbackHint, Notice }.Where(x => x.Length > 0));

    public static BehaviorPreviewModel Create(TargetedPlacementBehaviorDescription d, IntentEntryDraft? entry = null)
    {
        var scope = entry == null ? "セットの基本配置" : "選択タイルの配置";
        var fallback = "";
        BehaviorPreviewModel Unclear(string reason) => new([], [], "配置イメージ", scope, "", "", reason, false, false, 0, 1);
        var startDelta = d.StartOffset;
        var endDelta = d.EndOffset;
        var duration = d.Duration;
        var fixedLength = d.FixedDurationFrames;
        if (entry != null)
        {
            if (!Integer(entry.StartOffset, out var a) || !Integer(entry.EndOffset, out var b) ||
                (!string.IsNullOrWhiteSpace(entry.FixedDuration) && (!Integer(entry.FixedDuration, out var n) || n < 1)))
                return Unclear("タイルの長さ・ずらし量を入力してください。");
            // Match the existing entry precedence without building or saving a Draft.
            var ownLength = string.IsNullOrWhiteSpace(entry.FixedDuration) ? (int?)null : int.Parse(entry.FixedDuration, CultureInfo.InvariantCulture);
            duration = entry.UseTemplateDuration ? IntentDuration.Template : ownLength.HasValue ? IntentDuration.Fixed : duration;
            fixedLength = ownLength ?? fixedLength;
            if (startDelta.HasValue && endDelta.HasValue)
            {
                // Keep overflow explicit; never display a wrapped or silently clamped edge.
                var s = (long)startDelta.Value + a; var e = (long)endDelta.Value + b;
                if (s < int.MinValue || s > int.MaxValue || e < int.MinValue || e > int.MaxValue)
                    return Unclear("ずらし量が大きすぎるため、図を表示できません。");
                startDelta = (int)s; endDelta = (int)e;
            }
        }
        var needsNeighbor = duration == IntentDuration.UntilRelated || d.Anchor is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd;
        fallback = !needsNeighbor ? "" : d.Fallback switch
        {
            IntentFallback.DoNotPlace => "周辺なし：配置しない",
            IntentFallback.CurrentTargetEnd => "周辺なし：対象の終了まで",
            IntentFallback.TargetSpan => "周辺なし：対象と同じ範囲",
            IntentFallback.FixedDuration => $"周辺なし：固定 {fixedLength}f",
            _ => ""
        };
        if (d.MinimumCount is not int min || d.MaximumCount is not int max || min < 1 || max < min || max > 2048)
            return Unclear("対象の選択数を確認してください。");
        if (startDelta == null || endDelta == null)
            return Unclear("開始・終了のずらし量を入力してください。");
        if (duration == IntentDuration.Fixed && fixedLength == null)
            return Unclear("固定の長さを入力してください。");
        if (needsNeighbor && (d.Neighbor == IntentNeighbor.None ||
            (!string.IsNullOrWhiteSpace(d.MaximumNeighborGapText) &&
             (d.MaximumNeighborGap == null || d.MaximumNeighborGap < 0)) ||
            (d.Fallback == IntentFallback.FixedDuration && fixedLength == null)))
            return Unclear("周辺アイテムと、見つからない場合の設定を確認してください。");
        if (d.LayerMinimum is not int lower || d.LayerMaximum is not int upper || lower > upper || upper > 9999 ||
            (d.LayerMode == LayerPlacementMode.RelativeToTarget && (d.LayerOffset == null || d.LayerOffset > 9999)) ||
            (d.LayerMode == LayerPlacementMode.Absolute && (d.AbsoluteLayer == null || d.AbsoluteLayer < lower || d.AbsoluteLayer > upper)))
            return Unclear("配置するレイヤー・探索範囲を確認してください。");
        var pair = d.Anchor == IntentAnchor.PairBoundary;
        var range = d.Anchor is IntentAnchor.SelectionRangeStart or IntentAnchor.SelectionRangeEnd;
        var single = d.Anchor is IntentAnchor.SelectedStart or IntentAnchor.SelectedCenter or IntentAnchor.SelectedEnd;
        if (single && min > 1) return Unclear("この基準位置は1アイテム選択用です。");
        if (pair && (min > 2 || max < 2 || d.BoundaryTolerance == null || d.BoundaryTolerance < 0))
            return Unclear("2アイテムの境界と許容間隔を確認してください。");

        var absolute = d.LayerMode == LayerPlacementMode.Absolute;
        var targetRow = !absolute && d.LayerDirection == RelativeLayerDirection.Up ? 1 : 0;
        var outputRow = 1 - targetRow;
        var blocks = new List<PreviewBlock>();
        var start = TargetStart; var end = start + TargetLength;
        var multiple = pair || (range && max > 1);
        if (multiple)
        {
            blocks.Add(new(PreviewBlockKind.Target, "対象1", start, start + (pair ? 60 : 45), targetRow));
            blocks.Add(new(PreviewBlockKind.Target, "対象2", start + (pair ? 60 : 75), end, targetRow));
        }
        else blocks.Add(new(PreviewBlockKind.Target, "対象アイテム", start, end, targetRow));
        var previous = d.Neighbor is IntentNeighbor.PreviousSameType or IntentNeighbor.PreviousSameCharacter or IntentNeighbor.PreviousSameTypeAndCharacter;
        var gap = needsNeighbor && d.MaximumNeighborGap.HasValue ? Math.Min(80, d.MaximumNeighborGap.Value) : 80;
        var neighborStart = previous ? start - gap - 90 : end + gap;
        var neighborEnd = neighborStart + 90;
        if (needsNeighbor) blocks.Add(new(PreviewBlockKind.Neighbor, previous ? "前の周辺アイテム" : "次の周辺アイテム", neighborStart, neighborEnd, targetRow));
        var anchor = d.Anchor switch
        {
            IntentAnchor.SelectedStart or IntentAnchor.SelectionRangeStart => start,
            IntentAnchor.SelectedEnd or IntentAnchor.SelectionRangeEnd => end,
            IntentAnchor.SelectedCenter or IntentAnchor.PairBoundary => start + TargetLength / 2,
            IntentAnchor.RelatedStart => neighborStart,
            IntentAnchor.RelatedEnd => neighborEnd,
            _ => double.NaN
        };
        if (!double.IsFinite(anchor)) return Unclear("基準位置を確認してください。");
        var length = duration switch
        {
            IntentDuration.TargetSpan => TargetLength,
            IntentDuration.Fixed => fixedLength!.Value,
            IntentDuration.Template => SourceLength,
            _ => 0
        };
        var from = duration == IntentDuration.UntilRelated ? anchor : d.Alignment switch
        {
            IntentAlignment.StartAtAnchor => anchor,
            IntentAlignment.CenterAtAnchor => anchor - Math.Floor(length / 2),
            IntentAlignment.EndAtAnchor => anchor - length,
            _ => double.NaN
        };
        var to = duration == IntentDuration.UntilRelated
            ? (d.NeighborEdge == IntentNeighborEdge.Start ? neighborStart : neighborEnd) : from + length;
        from += startDelta.Value; to += endDelta.Value;
        var caption = duration switch
        {
            IntentDuration.TargetSpan => "対象と同じ長さ",
            IntentDuration.Template => "元の長さで配置（長さは例）",
            IntentDuration.Fixed => $"固定 {fixedLength}f",
            IntentDuration.UntilRelated => $"{(previous ? "前" : "次")}のアイテムの{(d.NeighborEdge == IntentNeighborEdge.Start ? "開始まで" : "終了まで")}",
            _ => "配置イメージ"
        };
        if (multiple && duration == IntentDuration.TargetSpan) caption = "選択範囲と同じ長さ";
        if (startDelta != 0 || endDelta != 0) caption += " ＋ ずらし";
        var notes = new List<string>();
        if (startDelta != 0) notes.Add($"開始 {startDelta:+0;-0;0}f");
        if (endDelta != 0) notes.Add($"終了 {endDelta:+0;-0;0}f");
        if (needsNeighbor) notes.Add(PlacementBehaviorText.Neighbor(d));
        if (multiple && !pair) notes.Add("選択範囲の例");
        if (pair) notes.Add("2アイテムが接する境界の例");
        if (duration == IntentDuration.Fixed) notes.Add("対象・周辺の長さは例");
        var meaningful = double.IsFinite(from) && to > from;
        if (meaningful) blocks.Add(new(PreviewBlockKind.Placed, "配置アイテム", from, to, outputRow));
        else notes.Add("この並びでは配置できません（終了が開始以前）");
        var left = blocks.Min(x => x.Start); var right = blocks.Max(x => x.End);
        if (right - left > TargetLength * 64)
            return Unclear("長さ・ずらし量が大きいため、図は省略しています。");
        var padding = Math.Max(12, (right - left) * .07);
        var guides = new List<double> { anchor };
        if (duration == IntentDuration.UntilRelated) guides.Add(d.NeighborEdge == IntentNeighborEdge.Start ? neighborStart : neighborEnd);
        if (meaningful) { guides.Add(from); guides.Add(to); }
        return new(blocks.AsReadOnly(), guides.Distinct().ToArray(), caption, scope,
            absolute ? $"配置先：レイヤー {d.AbsoluteLayer}（空きは{(d.LayerDirection == RelativeLayerDirection.Up ? "上" : "下")}へ）"
                : $"{(d.LayerDirection == RelativeLayerDirection.Up ? "↑" : "↓")} 対象より{d.LayerOffset}段{(d.LayerDirection == RelativeLayerDirection.Up ? "上" : "下")} ・ 空きは{(d.LayerDirection == RelativeLayerDirection.Up ? "上" : "下")}へ",
            fallback, string.Join(" ・ ", notes), absolute, true, left - padding, right + padding);
    }
    private static bool Integer(string value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}

public sealed partial class IntentPaletteDraft
{
    public BehaviorPreviewModel BehaviorDiagram => BehaviorPreviewModel.Create(BehaviorDescription,
        SelectedEntry != null && Entries.Contains(SelectedEntry) ? SelectedEntry : null);
}
