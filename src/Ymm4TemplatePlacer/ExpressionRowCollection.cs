using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Ymm4TemplatePlacer;

/// <summary>
/// Expression rows are normally published as one coherent snapshot. WPF still sees an
/// ObservableCollection, but a full refresh emits one Reset instead of N Add events.
/// </summary>
public sealed class ExpressionRowCollection : ObservableCollection<AssignmentRow>
{
    internal void ReplaceAll(IReadOnlyList<AssignmentRow> rows)
    {
        CheckReentrancy();
        Items.Clear();
        foreach (var row in rows) Items.Add(row);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
