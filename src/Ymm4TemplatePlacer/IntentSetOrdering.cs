using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed record IntentSetReorderRequest(IntentSetChoice Source, IntentSetChoice Target, bool After = false);

public sealed partial class PlacerViewModel
{
    public ActionCommand ReorderIntentSetCommand { get; private set; } = null!;
    private bool IsVisibleIntentSet(IntentSetChoice set) => intentTimeline != null && ReferenceEquals(intentTimeline, timeline) &&
        IntentSets.Any(x => ReferenceEquals(x, set));
    private bool CanReorderIntentSet(IntentSetReorderRequest request) => settingsAvailable && !intentExecuting &&
        tileEditState == IntentTileEditState.Idle && IntentSettings?.HasChanges != true &&
        (!HasSessionSettingsChanges || settingsSessionClosed) &&
        IsVisibleIntentSet(request.Source) && IsVisibleIntentSet(request.Target) &&
        (request.Source.Generic != null) == (request.Target.Generic != null);
    private void InitializeIntentSetOrdering()
    {
        ReorderIntentSetCommand = new(x => x is IntentSetReorderRequest request && CanReorderIntentSet(request),
            x => Guard(() => ReorderIntentSet((IntentSetReorderRequest)x!)));
        OnPropertyChanged(nameof(ReorderIntentSetCommand));
    }
    public void ReorderIntentSet(IntentSetReorderRequest request)
    {
        if (!CanReorderIntentSet(request))
            throw new InvalidOperationException("表示中のセットまたは設定の下書きが変わりました。保存・破棄してから並び替えてください。");
        if (ReferenceEquals(request.Source, request.Target)) return;
        var visible = IntentSets.Select(x => x.Id).ToList();
        var ordered = visible.ToList(); ordered.Remove(request.Source.Id);
        ordered.Insert(ordered.IndexOf(request.Target.Id) + (request.After ? 1 : 0), request.Source.Id);
        if (ordered.SequenceEqual(visible)) return;

        var next = PlacerSettingsStore.Copy(settings);
        // Fill only the original visible slots. Other characters, Item types and
        // non-Style Generic palettes retain both their positions and relative order.
        static void Apply<T>(List<T> saved, IReadOnlyList<Guid> visibleIds, IReadOnlyList<Guid> reorderedIds, Func<T, Guid> id)
        {
            var ids = visibleIds.ToHashSet();
            var slots = saved.Select((value, index) => (value, index)).Where(x => ids.Contains(id(x.value))).ToArray();
            if (slots.Length != visibleIds.Count || !slots.Select(x => id(x.value)).SequenceEqual(visibleIds))
                throw new InvalidOperationException("保存されたセットの並びが変わりました。開き直してから並び替えてください。");
            var values = slots.ToDictionary(x => id(x.value), x => x.value);
            for (var i = 0; i < slots.Length; i++) saved[slots[i].index] = values[reorderedIds[i]];
        }
        if (request.Source.Generic != null) Apply(next.Palettes, visible, ordered, x => x.Id);
        else Apply(next.IntentPalettes, visible, ordered, x => x.Id);

        // The existing protected store is the sole persistence route. A conflict
        // cannot replace the live model or discard a pending Settings draft.
        settingsStore.Save(next); settings = next;
        RefreshIntentWorkspace(); RefreshExpressionVocabulary(); ResetIntentSettings();
        HasError = false; Status = "セットの並び順を保存しました。";
    }
}
