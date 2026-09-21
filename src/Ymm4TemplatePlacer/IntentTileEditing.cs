using System.Windows;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed record IntentTileColorRequest(IntentTileChoice Tile, IntentTileColor Color);
public sealed record IntentTileShapeRequest(IntentTileChoice Tile, IntentTileShape Shape);

public sealed partial class PlacerViewModel
{
    private enum IntentTileEditState { Idle, ChoosingAlias }
    private IntentTileEditState tileEditState;
    public ActionCommand RenameIntentTileCommand { get; private set; } = null!;
    public ActionCommand ColorIntentTileCommand { get; private set; } = null!;
    public ActionCommand ShapeIntentTileCommand { get; private set; } = null!;
    public ActionCommand OpenTileSettingsCommand { get; private set; } = null!;
    public string IntentTileEditNotice => IntentSettings?.HasChanges == true
        ? "設定に未保存の変更があります。保存・破棄してからタイルを編集してください。" : "右クリックで表示名・色・形を変更できます。";
    private bool IsCurrentIntentTile(IntentTileChoice tile) => intentTimeline != null && ReferenceEquals(intentTimeline, timeline) && !UseLegacyWorkspace && selectedIntentSet?.Id == tile.PaletteId &&
        (selectedIntentSet.Generic != null) == tile.IsGeneric && IntentTiles.Any(x => ReferenceEquals(x, tile));
    private bool CanEditIntentTile(IntentTileChoice tile) => settingsAvailable && !intentExecuting &&
        tileEditState == IntentTileEditState.Idle && IntentSettings?.HasChanges != true && IsCurrentIntentTile(tile);
    private void InitializeIntentTileEditing()
    {
        InitializeIntentSetAppearance();
        RenameIntentTileCommand = new(x => x is IntentTileChoice tile && CanEditIntentTile(tile),
            x => Guard(() => RenameIntentTile((IntentTileChoice)x!)));
        ColorIntentTileCommand = new(x => x is IntentTileColorRequest r && Enum.IsDefined(r.Color) && CanEditIntentTile(r.Tile),
            x => Guard(() => { var r = (IntentTileColorRequest)x!; ChangeIntentTileAppearance(r.Tile, a => a with { Color = r.Color }); }));
        ShapeIntentTileCommand = new(x => x is IntentTileShapeRequest r && Enum.IsDefined(r.Shape) && CanEditIntentTile(r.Tile),
            x => Guard(() => { var r = (IntentTileShapeRequest)x!; ChangeIntentTileAppearance(r.Tile, a => a with { Shape = r.Shape }); }));
        OpenTileSettingsCommand = new(x => tileEditState == IntentTileEditState.Idle && x is IntentTileChoice tile && IsCurrentIntentTile(tile),
            x => Guard(() => OpenSettingsForTile((IntentTileChoice)x!)));
        foreach (var name in new[] { nameof(RenameIntentTileCommand), nameof(ColorIntentTileCommand), nameof(ShapeIntentTileCommand), nameof(OpenTileSettingsCommand) }) OnPropertyChanged(name);
    }
    private void UpdateIntentTileEditingCommands()
    {
        RenameIntentTileCommand?.RaiseCanExecuteChanged(); ColorIntentTileCommand?.RaiseCanExecuteChanged();
        ShapeIntentTileCommand?.RaiseCanExecuteChanged(); OpenTileSettingsCommand?.RaiseCanExecuteChanged();
        ReorderIntentTileCommand?.RaiseCanExecuteChanged(); ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
        ShapeIntentSetCommand?.RaiseCanExecuteChanged(); OpenIntentSetSettingsCommand?.RaiseCanExecuteChanged();
        ShapeCurrentIntentSetCommand?.RaiseCanExecuteChanged(); RefreshPanelQuickSettingsAdmission();
        UpdateGenericLayerCommands();
        OnPropertyChanged(nameof(HasCurrentIntentSet)); OnPropertyChanged(nameof(IntentTileEditNotice));
    }
    private PaletteTileAppearance ReadIntentTileAppearance(IntentTileChoice tile)
    {
        if (tile.IsGeneric) return settings.Palettes.Single(x => x.Id == tile.PaletteId).AppearanceFor(tile.LibraryEntryId);
        var entry = settings.IntentPalettes.Single(x => x.Id == tile.PaletteId).Entries.Single(x => x.LibraryEntryId == tile.LibraryEntryId);
        return new(entry.DisplayAlias, entry.Color, entry.Shape);
    }
    private void RenameIntentTile(IntentTileChoice tile)
    {
        if (!CanEditIntentTile(tile)) throw new InvalidOperationException(IntentTileEditNotice);
        CloseExpressionTrialSession();
        var dialog = new IntentTileAliasDialog(ReadIntentTileAppearance(tile).DisplayAlias ?? "");
        if (Application.Current?.MainWindow is { IsVisible: true } owner) dialog.Owner = owner;
        bool accepted;
        tileEditState = IntentTileEditState.ChoosingAlias; UpdateIntentTileEditingCommands();
        try { accepted = dialog.ShowDialog() == true; }
        finally { tileEditState = IntentTileEditState.Idle; UpdateIntentTileEditingCommands(); }
        if (accepted) ChangeIntentTileAppearance(tile, a => a with { DisplayAlias = string.IsNullOrWhiteSpace(dialog.AliasBox.Text) ? null : dialog.AliasBox.Text.Trim() });
    }
    public void ChangeIntentTileAppearance(IntentTileChoice tile, Func<PaletteTileAppearance, PaletteTileAppearance> change)
    {
        if (!CanEditIntentTile(tile)) throw new InvalidOperationException("Setまたは設定の下書きが変わりました。保存・破棄してからタイルを編集してください。");
        var appearance = change(ReadIntentTileAppearance(tile)); appearance.Validate();
        if (appearance == ReadIntentTileAppearance(tile)) return;
        var next = PlacerSettingsStore.Copy(settings);
        if (tile.IsGeneric)
        {
            var index = next.Palettes.FindIndex(x => x.Id == tile.PaletteId && x.Kind == PaletteKind.Style);
            if (index < 0 || !next.Palettes[index].LibraryEntryIds.Contains(tile.LibraryEntryId)) throw new InvalidOperationException("汎用Setの登録が変わりました。");
            var palette = next.Palettes[index]; var map = palette.TileAppearance ?? [];
            if (appearance == PaletteTileAppearance.Default) map.Remove(tile.LibraryEntryId); else map[tile.LibraryEntryId] = appearance;
            next.Palettes[index] = palette with { TileAppearance = map.Count == 0 ? null : map };
        }
        else
        {
            var palette = next.IntentPalettes.Single(x => x.Id == tile.PaletteId);
            var index = palette.Entries.FindIndex(x => x.LibraryEntryId == tile.LibraryEntryId);
            if (index < 0) throw new InvalidOperationException("Setの登録が変わりました。");
            palette.Entries[index] = palette.Entries[index] with { DisplayAlias = appearance.DisplayAlias, Color = appearance.Color, Shape = appearance.Shape };
        }
        // The live model is replaced only after the existing protected store succeeds.
        settingsStore.Save(next); settings = next;
        RefreshV04(); RefreshIntentWorkspace(); ResetIntentSettings();
        HasError = false; Status = "タイルの表示設定を保存しました。元テンプレートとタイムラインは変更していません。";
    }
    private void OpenSettingsForTile(IntentTileChoice tile)
    {
        if (!IsCurrentIntentTile(tile)) throw new InvalidOperationException("表示中のSetが変わりました。タイルを選び直してください。");
        OpenSettingsForSet(selectedIntentSet!, tile.LibraryEntryId);
    }
    private void OpenSettingsForSet(IntentSetChoice selected, Guid? entryId = null)
    {
        BeginIntentSettings(); var session = IntentSettings!;
        if (selected.Generic != null)
        {
            session.SelectedItemContext = session.ItemContexts.Single(x => x.IsGeneric);
            var set = session.GenericSets.SingleOrDefault(x => x.Id == selected.Id) ?? throw new InvalidOperationException("下書きではこのSetを削除済みです。");
            session.SelectedGenericSet = set; set.SelectedEntry = set.Entries.SingleOrDefault(x => x.LibraryEntryId == entryId);
        }
        else
        {
            var set = session.Palettes.SingleOrDefault(x => x.Id == selected.Id) ?? throw new InvalidOperationException("下書きではこのSetを削除済みです。");
            session.SelectedItemContext = session.ContextForPalette(set);
            session.SelectedPalette = set;
            set.SelectedEntry = set.Entries.SingleOrDefault(x => x.LibraryEntryId == entryId);
        }
        IntentSettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
