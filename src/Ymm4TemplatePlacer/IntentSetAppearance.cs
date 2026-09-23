using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed record IntentSetShapeRequest(IntentSetChoice Set, IntentTileShape Shape);

public sealed partial class PlacerViewModel
{
    public ActionCommand ShapeIntentSetCommand { get; private set; } = null!;
    public ActionCommand OpenIntentSetSettingsCommand { get; private set; } = null!;
    public bool HasCurrentIntentSet => selectedIntentSet != null;
    private bool IsCurrentIntentSet(IntentSetChoice set) => intentTimeline != null && ReferenceEquals(intentTimeline, timeline) &&
        ReferenceEquals(selectedIntentSet, set) && IntentSets.Any(x => ReferenceEquals(x, set));
    private bool CanEditIntentSet(IntentSetChoice set) => settingsAvailable && !intentExecuting &&
        tileEditState == IntentTileEditState.Idle && IntentSettings?.HasChanges != true && IsCurrentIntentSet(set);
    private void InitializeIntentSetAppearance()
    {
        ShapeIntentSetCommand = new(x => x is IntentSetShapeRequest r && Enum.IsDefined(r.Shape) && CanEditIntentSet(r.Set),
            x => Guard(() => { var r = (IntentSetShapeRequest)x!; ChangeIntentSetShape(r.Set, r.Shape); }));
        OpenIntentSetSettingsCommand = new(x => tileEditState == IntentTileEditState.Idle && x is IntentSetChoice set && IsCurrentIntentSet(set),
            x => Guard(() => OpenSettingsForSet((IntentSetChoice)x!)));
        OnPropertyChanged(nameof(ShapeIntentSetCommand)); OnPropertyChanged(nameof(OpenIntentSetSettingsCommand));
    }
    public void ChangeIntentSetShape(IntentSetChoice set, IntentTileShape shape)
    {
        if (!Enum.IsDefined(shape) || !CanEditIntentSet(set))
            throw new InvalidOperationException("Setまたは下書きが変わりました。保存・破棄してから形を変更してください。");
        var next = PlacerSettingsStore.Copy(settings); var changed = false;
        if (set.Generic != null)
        {
            var index = next.Palettes.FindIndex(x => x.Id == set.Id && x.Kind == PaletteKind.Style);
            if (index < 0) throw new InvalidOperationException("汎用Setが見つかりません。");
            var palette = next.Palettes[index]; var map = palette.TileAppearance ?? [];
            foreach (var id in palette.LibraryEntryIds)
            {
                var before = palette.AppearanceFor(id); changed |= before.Shape != shape;
                var appearance = before with { Shape = shape };
                if (appearance == PaletteTileAppearance.Default) map.Remove(id); else map[id] = appearance;
            }
            next.Palettes[index] = palette with { TileAppearance = map.Count == 0 ? null : map };
        }
        else
        {
            var palette = next.IntentPalettes.Single(x => x.Id == set.Id);
            for (var i = 0; i < palette.Entries.Count; i++)
            {
                changed |= palette.Entries[i].Shape != shape;
                palette.Entries[i] = palette.Entries[i] with { Shape = shape };
            }
        }
        if (!changed) return;
        CloseExpressionTrialSession();
        // One protected write, then one live replacement; never a Set-default/inheritance model.
        settingsStore.Save(next); settings = next;
        RefreshV04(); RefreshIntentWorkspace(); ResetIntentSettings();
        HasError = false; Status = $"このSetの形を「{IntentTileAppearance.ShapeName(shape)}」にそろえました。個別の形も変更できます。";
    }
}
