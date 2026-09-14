using System.Collections.ObjectModel;
using System.ComponentModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private bool refreshingPalettes;
    private Guid? contextCharacterPaletteId;
    private string selectedCharacterNotice = "";
    private string newPaletteName = "";
    private CharacterOption? newPaletteCharacter;
    private PaletteEntryView? selectedPaletteEntry;
    private LibraryEntryView? paletteLibraryChoice;
    public ObservableCollection<PaletteDefinition> CharacterPalettes { get; } = [];
    public ObservableCollection<PaletteDefinition> StylePalettes { get; } = [];
    public ObservableCollection<PaletteEntryView> PaletteEntries { get; } = [];
    public ObservableCollection<LibraryEntryView> PaletteLibraryChoices { get; } = [];
    public IReadOnlyList<PaletteKindChoice> PaletteKinds { get; } = [new(PaletteKind.Character, "キャラクター"), new(PaletteKind.Style, "スタイル")];
    public PaletteKind ActivePaletteKind
    {
        get => settings.PaletteMode;
        set
        {
            if (refreshingPalettes || value == settings.PaletteMode) return;
            Guard(() => EditSettings(next => next.PaletteMode = value));
        }
    }
    public PaletteDefinition? ManualCharacterPalette
    {
        get => CharacterPalettes.FirstOrDefault(x => x.Id == settings.ManualCharacterPaletteId);
        set
        {
            if (refreshingPalettes || value?.Id == settings.ManualCharacterPaletteId) return;
            Guard(() => EditSettings(next => next.ManualCharacterPaletteId = value?.Id));
        }
    }
    public PaletteDefinition? ManualStylePalette
    {
        get => StylePalettes.FirstOrDefault(x => x.Id == settings.ManualStylePaletteId);
        set
        {
            if (refreshingPalettes || value?.Id == settings.ManualStylePaletteId) return;
            Guard(() => EditSettings(next => next.ManualStylePaletteId = value?.Id));
        }
    }
    public PaletteDefinition? CurrentPalette => settings.Palettes.FirstOrDefault(x => x.Id ==
        (ActivePaletteKind == PaletteKind.Style ? settings.ManualStylePaletteId : contextCharacterPaletteId ?? settings.ManualCharacterPaletteId));
    public string CurrentPaletteName => CurrentPalette?.Name ?? "パレットが未選択です";
    public bool HasCharacterContext => contextCharacterPaletteId != null;
    public bool IsCharacterPaletteMode => ActivePaletteKind == PaletteKind.Character;
    public string PaletteContextStatus => ActivePaletteKind == PaletteKind.Style ? "スタイルパレットは手動で切り替えます。" :
        HasCharacterContext ? $"単体選択のキャラクターに一時切替中。解除すると「{ManualCharacterPalette?.Name ?? "未選択"}」へ戻ります。" :
        string.IsNullOrEmpty(selectedCharacterNotice) ? "手動で選んだキャラクターパレットです。音声・表情を1つだけ選んでいる間だけ自動で切り替わります。" : selectedCharacterNotice;
    public string PaletteEmptyMessage => CurrentPalette == null ? "下の［パレットを作る］でキャラクターまたはスタイルのパレットを登録してください。" :
        PaletteEntries.Count == 0 ? "このパレットは空です。登録済みテンプレートを選び、［このパレットへ追加］してください。" : "";
    public string NewPaletteName { get => newPaletteName; set => Set(ref newPaletteName, value); }
    public CharacterOption? NewPaletteCharacter { get => newPaletteCharacter; set => Set(ref newPaletteCharacter, value); }
    public LibraryEntryView? PaletteLibraryChoice { get => paletteLibraryChoice; set { Set(ref paletteLibraryChoice, value); UpdatePaletteCommands(); } }
    public PaletteEntryView? SelectedPaletteEntry { get => selectedPaletteEntry; set { Set(ref selectedPaletteEntry, value); UpdatePaletteCommands(); } }
    public ActionCommand CreatePaletteCommand { get; private set; } = null!;
    public ActionCommand DeletePaletteCommand { get; private set; } = null!;
    public ActionCommand AddPaletteEntryCommand { get; private set; } = null!;
    public ActionCommand RemovePaletteEntryCommand { get; private set; } = null!;
    partial void InitializePalettes()
    {
        CreatePaletteCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(() => CreatePalette()));
        DeletePaletteCommand = new ActionCommand(_ => settingsAvailable && CurrentPalette != null, _ => Guard(DeleteCurrentPalette));
        AddPaletteEntryCommand = new ActionCommand(_ => settingsAvailable && CurrentPalette != null && PaletteLibraryChoice != null, _ => Guard(AddPaletteEntry));
        RemovePaletteEntryCommand = new ActionCommand(_ => settingsAvailable && CurrentPalette != null && SelectedPaletteEntry != null, _ => Guard(RemovePaletteEntry));
        InitializeQuickDrop();
        RefreshPalettes();
    }
    partial void InitializeQuickDrop();
    partial void UpdateQuickDropCommands();
    partial void RefreshPalettes()
    {
        if (refreshingPalettes) return;
        refreshingPalettes = true;
        try
        {
            var choiceId = paletteLibraryChoice?.Id;
            var createCharacter = newPaletteCharacter?.Name;
            CharacterPalettes.Clear(); StylePalettes.Clear(); PaletteLibraryChoices.Clear();
            foreach (var palette in settings.Palettes)
                (palette.Kind == PaletteKind.Character ? CharacterPalettes : StylePalettes).Add(palette);
            foreach (var entry in settings.Library) PaletteLibraryChoices.Add(new(entry));
            PaletteLibraryChoice = PaletteLibraryChoices.FirstOrDefault(x => x.Id == choiceId);
            NewPaletteCharacter = LibraryCharacters.FirstOrDefault(x => x.Name == createCharacter);
            OnPropertyChanged(nameof(ActivePaletteKind)); OnPropertyChanged(nameof(ManualCharacterPalette));
            OnPropertyChanged(nameof(ManualStylePalette)); OnPropertyChanged(nameof(IsCharacterPaletteMode));
            ReadSelectionContext(); RefreshPaletteEntries(); UpdatePaletteCommands();
        }
        finally { refreshingPalettes = false; }
    }
    private void RefreshPaletteEntries()
    {
        var selectedId = SelectedPaletteEntry?.LibraryEntryId;
        PaletteEntries.Clear();
        var library = settings.Library.ToDictionary(x => x.Id);
        foreach (var id in CurrentPalette?.LibraryEntryIds ?? []) PaletteEntries.Add(new(id, library.GetValueOrDefault(id)));
        SelectedPaletteEntry = PaletteEntries.FirstOrDefault(x => x.LibraryEntryId == selectedId);
        OnPropertyChanged(nameof(CurrentPalette)); OnPropertyChanged(nameof(CurrentPaletteName));
        OnPropertyChanged(nameof(HasCharacterContext)); OnPropertyChanged(nameof(PaletteContextStatus)); OnPropertyChanged(nameof(PaletteEmptyMessage));
    }
    partial void AttachTimelineV04()
    {
        if (timeline != null) timeline.PropertyChanged += PaletteSelectionChanged;
        ReadSelectionContext(); RefreshPaletteEntries();
    }
    partial void DetachTimelineV04()
    {
        if (timeline != null) timeline.PropertyChanged -= PaletteSelectionChanged;
        contextCharacterPaletteId = null; selectedCharacterNotice = "";
    }
    private void PaletteSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Timeline.SelectedItems) && e.PropertyName != nameof(Timeline.SelectedItem)) return;
        var old = contextCharacterPaletteId;
        ReadSelectionContext();
        if (old != contextCharacterPaletteId) RefreshPaletteEntries();
        else OnPropertyChanged(nameof(PaletteContextStatus));
        RefreshSelectionPlacement(); UpdatePaletteCommands();
    }
    partial void RefreshSelectionPlacement();
    private void ReadSelectionContext()
    {
        contextCharacterPaletteId = null; selectedCharacterNotice = "";
        if (timeline?.SelectedItems.Count != 1) return;
        var selected = timeline.SelectedItems[0];
        if (!timeline.Items.Contains(selected)) return;
        var character = ItemCharacters.Get(selected);
        if (character == null) return;
        if (!ReferenceEquals(ItemCharacters.ResolveUnique(timeline, character.Name), character))
        {
            selectedCharacterNotice = "同名キャラクターを一意に特定できないため、自動切替していません。"; return;
        }
        var palettes = settings.Palettes.Where(x => x.Kind == PaletteKind.Character && string.Equals(x.CharacterName, character.Name, StringComparison.Ordinal)).Take(2).ToArray();
        if (palettes.Length == 1) contextCharacterPaletteId = palettes[0].Id;
        else selectedCharacterNotice = $"「{character.Name}」のキャラクターパレットは未登録です。手動で選んだパレットを表示しています。";
    }
    public PaletteDefinition CreatePalette()
    {
        var kind = ActivePaletteKind;
        var character = kind == PaletteKind.Character ? NewPaletteCharacter?.Name : null;
        if (kind == PaletteKind.Character && (character == null || ItemCharacters.ResolveUnique(timeline, character) == null))
            throw new InvalidOperationException("パレットに対応するキャラクターを一意に選んでください。同名キャラクターは自動で区別しません。");
        if (kind == PaletteKind.Character && settings.Palettes.Any(x => x.Kind == kind && x.CharacterName == character))
            throw new InvalidOperationException("このキャラクターのパレットは登録済みです。キャラクターパレット一覧から選んでください。");
        var name = string.IsNullOrWhiteSpace(NewPaletteName) && character != null ? character : NewPaletteName.Trim();
        var palette = new PaletteDefinition(Guid.NewGuid(), kind, name, character, []);
        EditSettings(next => { next.Palettes.Add(palette); if (kind == PaletteKind.Character) next.ManualCharacterPaletteId = palette.Id; else next.ManualStylePaletteId = palette.Id; });
        HasError = false; Status = $"「{name}」のパレットを作りました。登録済みテンプレートから使いたいものを追加してください。";
        return palette;
    }
    public void DeleteCurrentPalette()
    {
        var id = CurrentPalette?.Id ?? throw new InvalidOperationException("パレットを選んでください。");
        EditSettings(next =>
        {
            next.Palettes.RemoveAll(x => x.Id == id);
            if (next.ManualCharacterPaletteId == id) next.ManualCharacterPaletteId = null;
            if (next.ManualStylePaletteId == id) next.ManualStylePaletteId = null;
        });
        HasError = false; Status = "パレットを削除しました。テンプレート管理の登録・元テンプレート・タイムラインは変更していません。";
    }
    public void AddPaletteEntry()
    {
        var palette = CurrentPalette ?? throw new InvalidOperationException("パレットを選んでください。");
        var entry = PaletteLibraryChoice?.Entry ?? throw new InvalidOperationException("追加する登録済みテンプレートを選んでください。");
        if (palette.LibraryEntryIds.Contains(entry.Id)) throw new InvalidOperationException("このテンプレートは既にパレットにあります。");
        if (palette.Kind == PaletteKind.Character)
        {
            var sourceCharacter = TemplateResolver.Resolve(entry).Item is IItem source ? ItemCharacters.Get(source)?.Name : null;
            if ((entry.CharacterName != null && entry.CharacterName != palette.CharacterName) || (sourceCharacter != null && sourceCharacter != palette.CharacterName))
                throw new InvalidOperationException("このテンプレートのキャラクターは、表示中のキャラクターパレットと違います。");
        }
        EditSettings(next => next.Palettes.Single(x => x.Id == palette.Id).LibraryEntryIds.Add(entry.Id));
        SelectedPaletteEntry = PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        HasError = false; Status = $"「{entry.DisplayName}」を「{palette.Name}」へ追加しました。同じ登録済みテンプレートを他のパレットにも使えます。";
    }
    public void RemovePaletteEntry()
    {
        var palette = CurrentPalette ?? throw new InvalidOperationException("パレットを選んでください。");
        var id = SelectedPaletteEntry?.LibraryEntryId ?? throw new InvalidOperationException("パレットから外すテンプレートを選んでください。");
        EditSettings(next => next.Palettes.Single(x => x.Id == palette.Id).LibraryEntryIds.Remove(id));
        HasError = false; Status = "このパレットから外しました。他のパレット・テンプレート管理・タイムラインは変更していません。";
    }
    partial void OnLibraryUnregistered(PlacerSettings next, Guid id)
    {
        foreach (var palette in next.Palettes) palette.LibraryEntryIds.Remove(id);
    }
    private void UpdatePaletteCommands()
    {
        CreatePaletteCommand?.RaiseCanExecuteChanged(); DeletePaletteCommand?.RaiseCanExecuteChanged();
        AddPaletteEntryCommand?.RaiseCanExecuteChanged(); RemovePaletteEntryCommand?.RaiseCanExecuteChanged(); UpdateQuickDropCommands();
    }
}
