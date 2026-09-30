using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class IntentSettingsSession
{
    private string? settingsReferenceCharacter;
    private Guid? activeEditingPaletteId;
    private string? activeEditingContextKey;

    public bool ShowOtherCharacterSets
    {
        get => Presentation.ShowOtherCharacterSets;
        set => Presentation.ShowOtherCharacterSets = value;
    }
    public bool HasSettingsReferenceCharacter => SelectedItemContext?.IsRealItemType == true && settingsReferenceCharacter != null;
    public string SettingsReferenceCharacterLabel => SelectedItemContext switch
    {
        { IsCurrentSelection: true } => "現在の選択条件で表示",
        { IsMultiTypeCompatibility: true } => "複数種類セットを表示",
        { IsRealItemType: true } when settingsReferenceCharacter != null => $"基準キャラ: {settingsReferenceCharacter}",
        { IsRealItemType: true } => "基準キャラ: 特定なし（全セットを表示）",
        _ => ""
    };

    private void InitializeCharacterVisibility(IReadOnlyList<IItem> selection)
    {
        // Capture logical names now, not host objects whose Character may later change.
        settingsReferenceCharacter = DefiniteCharacter(selection);
        Presentation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PalettePresentationDraft.ShowOtherCharacterSets)) return;
            RefreshNavigation(); Raise(nameof(ShowOtherCharacterSets));
        };
    }

    private static string? DefiniteCharacter(IReadOnlyList<IItem> selection)
    {
        var names = selection.Select(x => ItemCharacters.Get(x)?.Name).Distinct(StringComparer.Ordinal).ToArray();
        return names.Length == 1 && !string.IsNullOrWhiteSpace(names[0]) ? names[0] : null;
    }
    private void RaiseCharacterVisibility()
    {
        Raise(nameof(HasSettingsReferenceCharacter)); Raise(nameof(SettingsReferenceCharacterLabel));
    }
    private void ClearActiveEditingPalette()
    {
        activeEditingPaletteId = null; activeEditingContextKey = null;
    }
    private void PinActiveEditingPalette(IntentPaletteDraft? palette)
    {
        activeEditingPaletteId = palette?.Id;
        activeEditingContextKey = selectedItemContext?.IsRealItemType == true ? selectedItemContext.Key : null;
    }
    private bool MatchesSettingsVisibility(IntentPaletteDraft draft)
    {
        // Current-selection applicability and legacy compatibility keep their exact old semantics.
        if (selectedItemContext?.IsRealItemType != true) return MatchesContext(draft);
        // A draft may be temporarily incomplete, or the user may turn OFF while editing
        // another character's Set. Never evict that bound editor from its opening parent.
        if (draft.Id == activeEditingPaletteId && selectedItemContext.Key == activeEditingContextKey) return true;
        if (!MatchesContext(draft)) return false;
        return ShowOtherCharacterSets || settingsReferenceCharacter == null || !draft.CharacterRestricted ||
            string.IsNullOrWhiteSpace(draft.CharacterName) || draft.CharacterName.Trim() == settingsReferenceCharacter;
    }
    internal void OpenPaletteForEditing(IntentPaletteDraft palette)
    {
        if (!Palettes.Contains(palette)) throw new InvalidOperationException("編集するSetが下書きにありません。");
        var context = ContextForPalette(palette);
        settingsReferenceCharacter = palette.CharacterRestricted && !string.IsNullOrWhiteSpace(palette.CharacterName)
            ? palette.CharacterName.Trim() : DefiniteCharacter(ItemContexts.FirstOrDefault(x => x.IsCurrentSelection)?.Selection ?? []);
        selectedItemContext = context;
        selectedPalette = palette;
        PinActiveEditingPalette(palette);
        RefreshNavigation(); NavigationChanged(nameof(SelectedItemContext));
        Raise(nameof(IsGenericContext)); Raise(nameof(IsTargetedContext)); Raise(nameof(CanCreateForContext)); Raise(nameof(ContextNotice));
        RaiseCharacterVisibility();
    }
    internal void RestoreCharacterVisibility(IntentSettingsSession previous)
    {
        settingsReferenceCharacter = previous.settingsReferenceCharacter;
        RaiseCharacterVisibility();
    }
}
