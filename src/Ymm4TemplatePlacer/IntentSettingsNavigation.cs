using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record IntentSettingsItemContext(string Key, string Label, IReadOnlyList<string> TypeKeys,
    IReadOnlyList<IItem>? Selection = null)
{
    public bool IsCurrentSelection => Selection != null;
}

public sealed partial class IntentSettingsSession
{
    private IntentSettingsItemContext? selectedItemContext;
    private string? selectedIntent;
    private bool showAllSets;
    private bool refreshingNavigation;
    public ObservableCollection<IntentSettingsItemContext> ItemContexts { get; } = [];
    public ObservableCollection<string> Intents { get; } = [];
    public ICollectionView VisiblePalettes { get; private set; } = null!;
    public bool CanCreateForContext => SelectedItemContext != null;
    public string ContextNotice => SelectedItemContext == null ? "タイムラインでアイテムを選ぶか、対象の種類を選んでください。" : "";
    public IntentSettingsItemContext? SelectedItemContext
    {
        get => selectedItemContext;
        set
        {
            if (refreshingNavigation || value == selectedItemContext || (value != null && !ItemContexts.Contains(value))) return;
            selectedItemContext = value; RefreshNavigation(); NavigationChanged();
            Raise(nameof(CanCreateForContext)); Raise(nameof(ContextNotice));
        }
    }
    public string? SelectedIntent
    {
        get => selectedIntent;
        set
        {
            if (refreshingNavigation || value == selectedIntent || (value != null && !Intents.Contains(value))) return;
            selectedIntent = value; RefreshPaletteFilter(); Raise();
        }
    }
    public bool ShowAllSets
    {
        get => showAllSets;
        set { if (showAllSets == value) return; showAllSets = value; RefreshNavigation(); Raise(); }
    }
    private void InitializeNavigation(IReadOnlyList<IItem> selection)
    {
        VisiblePalettes = new ListCollectionView(Palettes);
        VisiblePalettes.Filter = x => x is IntentPaletteDraft draft &&
            (ShowAllSets || (MatchesContext(draft) && draft.Intent == selectedIntent));
        var names = KnownTypes.Values.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in KnownTypes.OrderBy(x => x.Value, StringComparer.Ordinal))
        {
            ordinals[pair.Value] = ordinals.GetValueOrDefault(pair.Value) + 1;
            var label = names[pair.Value] > 1 ? $"{pair.Value} ({ordinals[pair.Value]})" : pair.Value;
            ItemContexts.Add(new(pair.Key, label, [pair.Key]));
        }
        UpdateSelectionContext(selection);
        RefreshNavigation();
    }
    public void UpdateSelectionContext(IReadOnlyList<IItem> selection)
    {
        var previous = ItemContexts.FirstOrDefault(x => x.IsCurrentSelection);
        if (previous?.Selection is { } old && old.SequenceEqual(selection)) return;
        var followSelection = selectedItemContext == null || selectedItemContext.IsCurrentSelection;
        refreshingNavigation = true;
        if (previous != null) ItemContexts.Remove(previous);
        IntentSettingsItemContext? current = null;
        if (selection.Count != 0)
        {
            var types = selection.Select(x => IntentSelectionContext.TypeKey(x.GetType())).Distinct(StringComparer.Ordinal).ToArray();
            var typeNames = selection.Select(x => ItemDisplayNames.For(x.GetType())).Distinct(StringComparer.Ordinal).ToArray();
            var names = selection.Select(x => ItemCharacters.Get(x)?.Name).Distinct(StringComparer.Ordinal).ToArray();
            var character = names.Length == 1 && !string.IsNullOrWhiteSpace(names[0]) ? names[0] + " " : "";
            var label = "現在: " + character + string.Join("・", typeNames) + (selection.Count > 1 ? $" ({selection.Count}件)" : "");
            current = new("current-selection", label, types, selection.ToArray()); ItemContexts.Insert(0, current);
        }
        if (followSelection) selectedItemContext = current;
        refreshingNavigation = false;
        RefreshNavigation(); NavigationChanged(nameof(SelectedItemContext));
        Raise(nameof(CanCreateForContext)); Raise(nameof(ContextNotice));
    }
    private bool MatchesContext(IntentPaletteDraft draft)
    {
        if (selectedItemContext == null) return false;
        var actual = selectedItemContext.TypeKeys;
        var allowed = draft.TypeChoices.Where(x => x.Selected).Select(x => x.Key).ToArray();
        var matches = draft.TypeMatch == IntentTypeMatch.UniformType
            ? actual.Count == 1 && allowed.Contains(actual[0], StringComparer.Ordinal)
            : actual.Order(StringComparer.Ordinal).SequenceEqual(allowed.Order(StringComparer.Ordinal), StringComparer.Ordinal);
        if (!matches) return false;
        // A manually chosen type is an organization filter, not a fictitious Timeline selection.
        if (selectedItemContext.Selection is not { } selected) return true;
        if (!int.TryParse(draft.MinimumCount, out var minimum) || !int.TryParse(draft.MaximumCount, out var maximum) ||
            selected.Count < minimum || selected.Count > maximum) return false;
        return !draft.CharacterRestricted || string.IsNullOrWhiteSpace(draft.CharacterName) ||
            selected.All(x => ItemCharacters.Get(x)?.Name == draft.CharacterName.Trim());
    }
    private void RefreshNavigation()
    {
        if (VisiblePalettes == null || refreshingNavigation) return;
        refreshingNavigation = true;
        try
        {
            var preferred = selectedPalette?.Id;
            var applicable = Palettes.Where(x => ShowAllSets || MatchesContext(x)).ToArray();
            var intent = applicable.FirstOrDefault(x => x.Id == preferred)?.Intent ?? selectedIntent;
            Intents.Clear(); foreach (var name in applicable.Select(x => x.Intent).Distinct(StringComparer.Ordinal)) Intents.Add(name);
            selectedIntent = intent != null && Intents.Contains(intent) ? intent : Intents.FirstOrDefault();
            SelectFilteredPalette(preferred);
        }
        finally { refreshingNavigation = false; }
        Raise(nameof(SelectedIntent)); NavigationChanged(nameof(SelectedPalette));
    }
    private void RefreshPaletteFilter(Guid? preferred = null)
    {
        if (refreshingNavigation) return;
        refreshingNavigation = true;
        try { SelectFilteredPalette(preferred ?? selectedPalette?.Id); }
        finally { refreshingNavigation = false; }
        NavigationChanged(nameof(SelectedPalette));
    }
    private void SelectFilteredPalette(Guid? preferred)
    {
        VisiblePalettes.Refresh();
        var visible = VisiblePalettes.Cast<IntentPaletteDraft>().ToArray();
        selectedPalette = visible.FirstOrDefault(x => x.Id == preferred) ?? visible.FirstOrDefault();
    }
    public void CreateForContext()
    {
        var context = SelectedItemContext ?? throw new InvalidOperationException("セットを使うアイテムを先に選んでください。");
        if (context.Selection is { } selected) Create(selected);
        else
        {
            var target = new IntentTargetContext { ItemTypeKeys = context.TypeKeys.ToList() };
            var intent = context.TypeKeys.Contains(IntentSelectionContext.TypeKey(typeof(VoiceItem))) ? "表情" : "演出";
            SelectedPalette = AddDraft(new(Guid.NewGuid(), UniqueName("新しいセット"), intent, target, new(), [])); MarkDirty();
        }
        RefreshNavigation();
    }
}
