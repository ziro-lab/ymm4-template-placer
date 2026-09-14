using System.Collections.ObjectModel;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

// The persisted Character/Style split is an implementation detail of the unified Palette task.
public sealed partial class PlacerViewModel
{
    private bool explicitPaletteSelection, isCreatingPalette;
    private Guid? paletteNameDraftId;
    private string paletteNameDraft = "", savedPaletteName = "";
    public ObservableCollection<PaletteDefinition> PaletteChoices { get; } = [];
    public PaletteDefinition? SelectedPalette
    {
        get => CurrentPalette;
        set
        {
            if (refreshingPalettes || value == null || value.Id == CurrentPalette?.Id) return;
            Guard(() =>
            {
                var palette = settings.Palettes.SingleOrDefault(x => x.Id == value.Id)
                    ?? throw new InvalidOperationException("パレットが削除されています。一覧を更新してください。");
                var previous = explicitPaletteSelection;
                try
                {
                    // An explicit picker choice wins over the old selection context until the next Timeline selection.
                    explicitPaletteSelection = true;
                    EditSettings(next =>
                    {
                        next.PaletteMode = palette.Kind;
                        if (palette.Kind == PaletteKind.Character) next.ManualCharacterPaletteId = palette.Id;
                        else next.ManualStylePaletteId = palette.Id;
                    });
                }
                catch { explicitPaletteSelection = previous; ReadSelectionContext(); RefreshPaletteEntries(); throw; }
            });
        }
    }
    public bool IsCreatingPalette { get => isCreatingPalette; private set => Set(ref isCreatingPalette, value); }
    public string PaletteNameDraft { get => paletteNameDraft; set => Set(ref paletteNameDraft, value); }
    public ActionCommand BeginCreatePaletteCommand { get; private set; } = null!;
    public ActionCommand CancelCreatePaletteCommand { get; private set; } = null!;
    public ActionCommand RenamePaletteCommand { get; private set; } = null!;
    private void InitializePaletteTask()
    {
        BeginCreatePaletteCommand = new ActionCommand(_ => settingsAvailable, _ =>
        {
            NewPaletteName = ""; NewPaletteCharacter = LibraryCharacters.FirstOrDefault(x => x.Name == null);
            IsCreatingPalette = true;
        });
        CancelCreatePaletteCommand = new ActionCommand(_ => true, _ => IsCreatingPalette = false);
        RenamePaletteCommand = new ActionCommand(_ => settingsAvailable && CurrentPalette != null, _ => Guard(RenamePalette));
    }
    private void RefreshPaletteNameDraft()
    {
        if (paletteNameDraftId == CurrentPalette?.Id && savedPaletteName == (CurrentPalette?.Name ?? "")) return;
        paletteNameDraftId = CurrentPalette?.Id; savedPaletteName = CurrentPalette?.Name ?? "";
        PaletteNameDraft = savedPaletteName;
    }
    public void RenamePalette()
    {
        var id = CurrentPalette?.Id ?? throw new InvalidOperationException("パレットを選んでください。");
        var name = PaletteNameDraft.Trim();
        if (name.Length == 0 || name.Length > 256) throw new InvalidOperationException("パレット名は1〜256文字で入力してください。");
        EditSettings(next => { var index = next.Palettes.FindIndex(x => x.Id == id); next.Palettes[index] = next.Palettes[index] with { Name = name }; });
        HasError = false; Status = "パレット名を変更しました。";
    }
}
