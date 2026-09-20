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
    private bool CanReorderIntentTile(IntentTileReorderRequest request) => CanEditIntentTile(request.Source) &&
        request.Source.PaletteId == request.Target.PaletteId && request.Source.IsGeneric == request.Target.IsGeneric && IsCurrentIntentTile(request.Target);

    public void ReorderIntentTile(IntentTileReorderRequest request)
    {
        if (!CanReorderIntentTile(request))
            throw new InvalidOperationException("セットまたは設定の下書きが変わりました。保存・破棄してから並び替えてください。");
        if (ReferenceEquals(request.Source, request.Target)) return;
        var next = PlacerSettingsStore.Copy(settings);
        if (request.Source.IsGeneric)
        {
            var palette = next.Palettes.Single(x => x.Id == request.Source.PaletteId && x.Kind == PaletteKind.Style);
            var ids = palette.LibraryEntryIds; var from = ids.IndexOf(request.Source.LibraryEntryId); var to = ids.IndexOf(request.Target.LibraryEntryId);
            if (from < 0 || to < 0) throw new InvalidOperationException("並び替える演出が見つかりません。セットを開き直してください。");
            var id = ids[from]; ids.RemoveAt(from); ids.Insert(to, id);
        }
        else
        {
            var palette = next.IntentPalettes.Single(x => x.Id == request.Source.PaletteId);
            var from = palette.Entries.FindIndex(x => x.LibraryEntryId == request.Source.LibraryEntryId);
            var to = palette.Entries.FindIndex(x => x.LibraryEntryId == request.Target.LibraryEntryId);
            if (from < 0 || to < 0) throw new InvalidOperationException("並び替える演出が見つかりません。セットを開き直してください。");
            var entry = palette.Entries[from]; palette.Entries.RemoveAt(from); palette.Entries.Insert(to, entry);
        }
        // Same protected store and original order lists. No Timeline access / second order store.
        settingsStore.Save(next); settings = next;
        RefreshIntentWorkspace(); RefreshExpressionVocabulary(); ResetIntentSettings();
        HasError = false; Status = "タイルの並び順を保存しました。";
    }
}
