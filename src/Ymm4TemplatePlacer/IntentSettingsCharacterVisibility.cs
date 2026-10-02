namespace Ymm4TemplatePlacer;

public sealed record IntentCharacterFilterOption(string? CharacterName, string Label);

public sealed partial class IntentSettingsSession
{
    private string? characterFilterCharacter;
    private Guid? activeEditingPaletteId;
    private string? activeEditingContextKey;

    public string? CharacterFilterCharacter
    {
        get => characterFilterCharacter;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (normalized != null && !CharacterFilterNames().Contains(normalized, StringComparer.Ordinal)) normalized = null;
            if (characterFilterCharacter == normalized) return;
            characterFilterCharacter = normalized;
            // An explicit filter change is authoritative. Do not keep a currently
            // edited Set visible when the user deliberately chooses another character.
            ClearActiveEditingPalette();
            RefreshPaletteFilter();
            Raise(nameof(CharacterFilterCharacter));
            Raise(nameof(CharacterFilterOptions));
            Raise(nameof(HasCharacterFilterOptions));
        }
    }

    public IReadOnlyList<IntentCharacterFilterOption> CharacterFilterOptions
    {
        get
        {
            var result = new List<IntentCharacterFilterOption> { new(null, "すべて") };
            result.AddRange(CharacterFilterNames().Select(x => new IntentCharacterFilterOption(x, x)));
            return result;
        }
    }

    public bool HasCharacterFilterOptions => SelectedItemContext?.IsRealItemType == true && CharacterFilterNames().Count != 0;

    private IReadOnlyList<string> CharacterFilterNames()
    {
        if (selectedItemContext?.IsRealItemType != true) return [];
        return Palettes
            .Where(x => x.IsSingleOwner && x.OwnerTypeKey == selectedItemContext.Key &&
                x.CharacterRestricted && !string.IsNullOrWhiteSpace(x.CharacterName))
            .Select(x => x.CharacterName.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private void InitializeCharacterFilter() => characterFilterCharacter = null;

    private void ResetCharacterFilterForContext()
    {
        characterFilterCharacter = null;
        Raise(nameof(CharacterFilterCharacter));
        Raise(nameof(CharacterFilterOptions));
        Raise(nameof(HasCharacterFilterOptions));
    }

    private void RefreshCharacterFilterState()
    {
        var names = CharacterFilterNames();
        if (characterFilterCharacter != null && !names.Contains(characterFilterCharacter, StringComparer.Ordinal))
            characterFilterCharacter = null;
        Raise(nameof(CharacterFilterCharacter));
        Raise(nameof(CharacterFilterOptions));
        Raise(nameof(HasCharacterFilterOptions));
    }

    private void ClearActiveEditingPalette()
    {
        activeEditingPaletteId = null;
        activeEditingContextKey = null;
    }

    private void PinActiveEditingPalette(IntentPaletteDraft? palette)
    {
        activeEditingPaletteId = palette?.Id;
        activeEditingContextKey = selectedItemContext?.IsRealItemType == true ? selectedItemContext.Key : null;
    }

    private bool MatchesSettingsVisibility(IntentPaletteDraft draft)
    {
        // Current-selection applicability and legacy compatibility keep their exact
        // runtime-like Settings semantics. The explicit character filter belongs only
        // to the manually chosen single Item-type parent.
        if (selectedItemContext?.IsRealItemType != true) return MatchesContext(draft);

        // Keep a bound draft alive while its own target fields are being edited.
        // Selecting a different character filter clears this pin first.
        if (draft.Id == activeEditingPaletteId && selectedItemContext.Key == activeEditingContextKey) return true;
        if (!MatchesContext(draft)) return false;
        if (characterFilterCharacter == null) return true;

        return draft.CharacterRestricted &&
            !string.IsNullOrWhiteSpace(draft.CharacterName) &&
            draft.CharacterName.Trim() == characterFilterCharacter;
    }

    internal void OpenPaletteForEditing(IntentPaletteDraft palette)
    {
        if (!Palettes.Contains(palette)) throw new InvalidOperationException("編集するSetが下書きにありません。");
        var context = ContextForPalette(palette);
        selectedItemContext = context;
        ResetCharacterFilterForContext();
        selectedPalette = palette;
        PinActiveEditingPalette(palette);
        RefreshNavigation();
        NavigationChanged(nameof(SelectedItemContext));
        Raise(nameof(IsGenericContext));
        Raise(nameof(IsTargetedContext));
        Raise(nameof(CanCreateForContext));
        Raise(nameof(ContextNotice));
    }

    internal void RestoreCharacterFilter(IntentSettingsSession previous)
    {
        var previousCharacter = previous.characterFilterCharacter;
        characterFilterCharacter = previousCharacter != null &&
            CharacterFilterNames().Contains(previousCharacter, StringComparer.Ordinal)
                ? previousCharacter
                : null;
        ClearActiveEditingPalette();
        RefreshPaletteFilter();
        RefreshCharacterFilterState();
    }
}
