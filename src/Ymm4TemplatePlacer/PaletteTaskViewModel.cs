using System.Collections.ObjectModel;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

// The persisted Character/Style split is an implementation detail of the unified Palette task.
public sealed record PaletteChoice(Guid Id, string Name, string DisplayLabel, PaletteKind Kind, string? CharacterName);

public sealed partial class PlacerViewModel
{
    private bool explicitPaletteSelection, isCreatingPalette;
    private Guid? paletteNameDraftId;
    private string paletteNameDraft = "", savedPaletteName = "";
    public ObservableCollection<PaletteChoice> PaletteChoices { get; } = [];
    public PaletteChoice? SelectedPalette
    {
        get => PaletteChoices.FirstOrDefault(x => x.Id == CurrentPalette?.Id);
        set
        {
            if (refreshingPalettes || value == null || value.Id == CurrentPalette?.Id) return;
            Guard(() =>
            {
                var palette = settings.Palettes.SingleOrDefault(x => x.Id == value.Id)
                    ?? throw new InvalidOperationException("パレットが削除されています。［メンテナンス → 一覧を読み直す］を選んでください。");
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
    private static string PaletteCollisionDescriptor(PaletteDefinition palette) =>
        palette.Kind == PaletteKind.Character ? palette.CharacterName ?? "キャラクター" : "手動";
    private void RefreshPaletteTaskChoices()
    {
        // Do not reset a live WPF picker because a preset/serial save deep-copied the storage model.
        // The picker needs stable identity and labels, not mutable membership or Layer records.
        var labels = new Dictionary<Guid, string>();
        foreach (var group in settings.Palettes.GroupBy(x => x.Name, StringComparer.Ordinal))
        {
            var palettes = group.ToArray();
            if (palettes.Length == 1)
            {
                labels[palettes[0].Id] = palettes[0].Name;
                continue;
            }
            var descriptorTotals = palettes.GroupBy(PaletteCollisionDescriptor, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
            var descriptorSeen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var palette in palettes)
            {
                var descriptor = PaletteCollisionDescriptor(palette);
                descriptorSeen.TryGetValue(descriptor, out var seen); seen++; descriptorSeen[descriptor] = seen;
                labels[palette.Id] = descriptorTotals[descriptor] == 1
                    ? $"{palette.Name} — {descriptor}"
                    : $"{palette.Name} — {descriptor} {seen}";
            }
        }
        var choices = settings.Palettes.Select(x => new PaletteChoice(x.Id, x.Name, labels[x.Id], x.Kind, x.CharacterName)).ToArray();
        if (PaletteChoices.SequenceEqual(choices)) return;
        PaletteChoices.Clear();
        foreach (var choice in choices) PaletteChoices.Add(choice);
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
