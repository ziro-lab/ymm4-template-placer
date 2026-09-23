using System.Globalization;
using System.Windows.Data;

namespace Ymm4TemplatePlacer;

public sealed record IntentSentenceOption<T>(T Value, string Name, bool Available = true);
public sealed class SettingsContextMatchConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values.Length == 2 && values[0] is IntentSettingsItemContext choice && values[1] is IntentSettingsItemContext active &&
        (ReferenceEquals(choice, active) || (active.IsCurrentSelection && choice.TypeKeys.Count == 1 && active.TypeKeys.SequenceEqual(choice.TypeKeys)));
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Presentation tokens only. Saved values and geometry remain the existing finite relation model.
public sealed partial class IntentPaletteDraft
{
    private bool SingleTarget => int.TryParse(MinimumCount, out var min) && int.TryParse(MaximumCount, out var max) && min == 1 && max == 1;
    public string SentenceAnchorJoin => Duration == IntentDuration.UntilRelated ? "から" : Alignment switch
    {
        IntentAlignment.StartAtAnchor => "から",
        IntentAlignment.CenterAtAnchor => "に中央を合わせて",
        IntentAlignment.EndAtAnchor => "で終わるように",
        _ => ""
    };
    public IReadOnlyList<IntentSentenceOption<IntentAnchor>> SentenceAnchors =>
    [
        new(IntentAnchor.SelectedStart, "選択アイテムの開始", SingleTarget), new(IntentAnchor.SelectedEnd, "選択アイテムの終了", SingleTarget),
        new(IntentAnchor.SelectedCenter, "選択アイテムの中央", SingleTarget), new(IntentAnchor.SelectionRangeStart, "選択範囲の開始"),
        new(IntentAnchor.SelectionRangeEnd, "選択範囲の終了"), new(IntentAnchor.PairBoundary, "選択した2件の境界", MinimumCount == "2" && MaximumCount == "2"),
        new(IntentAnchor.RelatedStart, "周囲アイテムの開始"), new(IntentAnchor.RelatedEnd, "周囲アイテムの終了")
    ];
    // None is never offered for a relation that requires a neighbor. Opening an editor does not normalize saved data.
    public IReadOnlyList<IntentOption<IntentNeighbor>> SentenceNeighbors { get; } =
    [
        new(IntentNeighbor.NextSameType, "次の同じ種類"), new(IntentNeighbor.PreviousSameType, "前の同じ種類"),
        new(IntentNeighbor.NextSameCharacter, "次の同じキャラ"), new(IntentNeighbor.PreviousSameCharacter, "前の同じキャラ"),
        new(IntentNeighbor.NextSameTypeAndCharacter, "次の同じ種類・同じキャラ"), new(IntentNeighbor.PreviousSameTypeAndCharacter, "前の同じ種類・同じキャラ")
    ];
}
