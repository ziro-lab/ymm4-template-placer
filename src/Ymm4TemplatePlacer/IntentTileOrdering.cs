using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed record IntentTileReorderRequest(IntentTileChoice Source, IntentTileChoice Target);

public sealed partial class PlacerViewModel
{
    public ActionCommand ReorderIntentTileCommand { get; private set; } = null!;
    private void InitializeIntentTileOrdering()
    {
        ReorderIntentTileCommand = new ActionCommand(x => x is IntentTileReorderRequest request && CanReorderIntentTile(request),
            x => Guard(() => ReorderIntentTile((IntentTileReorderRequest)x!)));
        OnPropertyChanged(nameof(ReorderIntentTileCommand));
    }
    private bool CanReorderIntentTile(IntentTileReorderRequest request) => settingsAvailable && !intentExecuting &&
        IntentSettings?.HasChanges != true && request.Source.PaletteId == request.Target.PaletteId &&
        selectedIntentSet?.Palette.Id == request.Source.PaletteId &&
        IntentTiles.Any(x => ReferenceEquals(x, request.Source)) && IntentTiles.Any(x => ReferenceEquals(x, request.Target));

    public void ReorderIntentTile(IntentTileReorderRequest request)
    {
        if (!CanReorderIntentTile(request))
            throw new InvalidOperationException("セットまたは設定の下書きが変わりました。保存・破棄してから並び替えてください。");
        if (ReferenceEquals(request.Source, request.Target)) return;
        var next = PlacerSettingsStore.Copy(settings);
        var palette = next.IntentPalettes.Single(x => x.Id == request.Source.PaletteId);
        var from = palette.Entries.FindIndex(x => x.LibraryEntryId == request.Source.Entry.LibraryEntryId);
        var to = palette.Entries.FindIndex(x => x.LibraryEntryId == request.Target.Entry.LibraryEntryId);
        if (from < 0 || to < 0) throw new InvalidOperationException("並び替える演出が見つかりません。セットを開き直してください。");
        var entry = palette.Entries[from]; palette.Entries.RemoveAt(from); palette.Entries.Insert(to, entry);
        // Same protected store and same Entries order. No Timeline access / second order store.
        settingsStore.Save(next); settings = next;
        RefreshIntentWorkspace(); RefreshExpressionVocabulary(); ResetIntentSettings();
        HasError = false; Status = "タイルの並び順を保存しました。";
    }
}
