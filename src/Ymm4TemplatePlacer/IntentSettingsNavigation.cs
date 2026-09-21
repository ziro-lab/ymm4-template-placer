using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed record IntentSettingsItemContext(string Key, string Label, IReadOnlyList<string> TypeKeys,
    IReadOnlyList<IItem>? Selection = null)
{
    public bool IsCurrentSelection => Selection != null;
    public bool IsGeneric => Key == "generic";
    public bool IsMultiTypeCompatibility => Key == "multi-type-compat";
    public bool IsRealItemType => !IsCurrentSelection && !IsGeneric && !IsMultiTypeCompatibility && TypeKeys.Count == 1;
}

public sealed partial class IntentSettingsSession
{
    private IntentSettingsItemContext? selectedItemContext;
    private IntentSettingsItemContext? selectedCopyDestination;
    private string? selectedIntent;
    private bool refreshingNavigation;
    public ObservableCollection<IntentSettingsItemContext> ItemContexts { get; } = [];
    public ObservableCollection<string> Intents { get; } = [];
    public ICollectionView VisiblePalettes { get; private set; } = null!;
    public bool CanCreateForContext => SelectedItemContext is { } context &&
        (context.IsGeneric || context.IsRealItemType || (context.IsCurrentSelection && context.TypeKeys.Count == 1));
    public bool IsGenericContext => SelectedItemContext?.IsGeneric == true;
    public bool IsTargetedContext => SelectedItemContext != null && !IsGenericContext;
    public IEnumerable<IntentSettingsItemContext> DirectItemContexts => ItemContexts.Where(x => !x.IsCurrentSelection);
    // Retained API aliases; normal Settings is Item-first and no longer has a global all-Set mode.
    public IEnumerable<IntentSettingsItemContext> CommonItemContexts => DirectItemContexts;
    public IEnumerable<IntentSettingsItemContext> OtherItemContexts => ItemContexts.Where(x => !IsCommonContext(x));
    public IEnumerable<IntentSettingsItemContext> CopyDestinations => ItemContexts.Where(x => x.IsRealItemType && x.Key != SelectedPalette?.OwnerTypeKey);
    public IntentSettingsItemContext? SelectedCopyDestination
    {
        get => selectedCopyDestination;
        set
        {
            var allowed = CopyDestinations.ToArray();
            if (value != null && !allowed.Contains(value)) return;
            if (ReferenceEquals(selectedCopyDestination, value)) return;
            selectedCopyDestination = value; NavigationChanged(); Raise(nameof(CanCopyToOtherItem));
        }
    }
    public bool CanCopyToOtherItem => !IsGenericContext && SelectedPalette != null && SelectedCopyDestination != null;
    private static bool IsCommonContext(IntentSettingsItemContext x) => x.IsGeneric || x.IsMultiTypeCompatibility || (!x.IsCurrentSelection &&
        x.TypeKeys.Any(k => CommonSettingsTypes.Select(IntentSelectionContext.TypeKey).Contains(k, StringComparer.Ordinal)));
    // Fixed known public type names only, not an assembly-wide discovery/reflection scan.
    // Missing host types are omitted; loaded third-party types are supplied by the root.
    private static readonly Type[] CommonSettingsTypes = new[] { "VoiceItem", "TextItem", "ImageItem", "ShapeItem", "AudioItem", "VideoItem",
        "TachieFaceItem", "TachieItem", "TransitionItem", "FrameBufferItem", "EffectItem" }
        .Select(name => typeof(VoiceItem).Assembly.GetType("YukkuriMovieMaker.Project.Items." + name, false))
        .OfType<Type>().Where(type => type.IsVisible && !type.IsAbstract && typeof(IItem).IsAssignableFrom(type)).ToArray();
    public string ContextNotice => SelectedItemContext switch
    {
        null => "タイムラインでアイテムを選ぶか、対象の種類を選んでください。",
        { IsMultiTypeCompatibility: true } => "以前の設定で複数種類を共有しているSetです。新しい共有Setは作成できません。必要なら1つのアイテム種類へコピーしてください。",
        { IsCurrentSelection: true, TypeKeys.Count: > 1 } => "複数種類を選択中です。作成先のアイテム種類を上で選んでからSetを作成してください。",
        { IsCurrentSelection: true } context => context.Label,
        _ => ""
    };
    public IntentSettingsItemContext? SelectedItemContext
    {
        get => selectedItemContext;
        set
        {
            if (refreshingNavigation || value == selectedItemContext || (value != null && !ItemContexts.Contains(value))) return;
            selectedItemContext = value; RefreshNavigation(); NavigationChanged();
            Raise(nameof(IsGenericContext)); Raise(nameof(IsTargetedContext)); Raise(nameof(HasSelectedSet)); Raise(nameof(HasSelectedSetEntry));
            Raise(nameof(CanCreateForContext)); Raise(nameof(ContextNotice)); RefreshCopyDestination();
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
    private void InitializeNavigation(IReadOnlyList<IItem> selection)
    {
        VisiblePalettes = new ListCollectionView(Palettes);
        VisiblePalettes.Filter = x => x is IntentPaletteDraft draft && !IsGenericContext && MatchesContext(draft);
        ItemContexts.Add(new("generic", "汎用", []));
        var names = KnownTypes.Values.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in KnownTypes.OrderBy(x => x.Value, StringComparer.Ordinal))
        {
            ordinals[pair.Value] = ordinals.GetValueOrDefault(pair.Value) + 1;
            var label = names[pair.Value] > 1 ? $"{pair.Value} ({ordinals[pair.Value]})" : pair.Value;
            ItemContexts.Add(new(pair.Key, label, [pair.Key]));
        }
        EnsureCompatibilityContext();
        UpdateSelectionContext(selection);
        RefreshNavigation();
    }
    private void EnsureCompatibilityContext()
    {
        var existing = ItemContexts.FirstOrDefault(x => x.IsMultiTypeCompatibility);
        var required = Palettes.Any(x => x.IsLegacyMultiType);
        if (required && existing == null)
        {
            ItemContexts.Add(new("multi-type-compat", "複数種類", []));
            Raise(nameof(DirectItemContexts)); Raise(nameof(CommonItemContexts)); Raise(nameof(OtherItemContexts));
        }
        else if (!required && existing != null)
        {
            if (ReferenceEquals(selectedItemContext, existing)) selectedItemContext = ItemContexts.FirstOrDefault(x => x.IsGeneric);
            ItemContexts.Remove(existing);
            Raise(nameof(SelectedItemContext)); Raise(nameof(DirectItemContexts)); Raise(nameof(CommonItemContexts)); Raise(nameof(OtherItemContexts));
        }
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
        Raise(nameof(DirectItemContexts)); Raise(nameof(CommonItemContexts)); Raise(nameof(OtherItemContexts));
        Raise(nameof(IsGenericContext)); Raise(nameof(IsTargetedContext));
    }
    private bool MatchesContext(IntentPaletteDraft draft)
    {
        if (selectedItemContext == null) return false;
        if (selectedItemContext.IsMultiTypeCompatibility) return draft.IsLegacyMultiType;
        if (!draft.IsSingleOwner || draft.OwnerTypeKey == null) return false;
        var actual = selectedItemContext.TypeKeys;
        if (actual.Count != 1 || actual[0] != draft.OwnerTypeKey) return false;
        // A manually chosen Item type is an organization parent, not a fictitious Timeline selection.
        if (selectedItemContext.Selection is not { } selected) return true;
        if (!int.TryParse(draft.MinimumCount, out var minimum) || !int.TryParse(draft.MaximumCount, out var maximum) ||
            selected.Count < minimum || selected.Count > maximum) return false;
        return !draft.CharacterRestricted || string.IsNullOrWhiteSpace(draft.CharacterName) ||
            selected.All(x => ItemCharacters.Get(x)?.Name == draft.CharacterName.Trim());
    }
    private void RefreshNavigation()
    {
        if (VisiblePalettes == null || refreshingNavigation) return;
        EnsureCompatibilityContext();
        refreshingNavigation = true;
        try
        {
            var preferred = selectedPalette?.Id;
            var applicable = Palettes.Where(MatchesContext).ToArray();
            var intent = applicable.FirstOrDefault(x => x.Id == preferred)?.Intent ?? selectedIntent;
            Intents.Clear(); foreach (var name in applicable.Select(x => x.Intent).Distinct(StringComparer.Ordinal)) Intents.Add(name);
            selectedIntent = intent != null && Intents.Contains(intent) ? intent : Intents.FirstOrDefault();
            SelectFilteredPalette(preferred);
        }
        finally { refreshingNavigation = false; }
        Raise(nameof(SelectedIntent)); Raise(nameof(HasSelectedSet)); Raise(nameof(HasSelectedSetEntry)); NavigationChanged(nameof(SelectedPalette));
        RefreshCopyDestination();
    }
    private void RefreshPaletteFilter(Guid? preferred = null)
    {
        if (refreshingNavigation) return;
        refreshingNavigation = true;
        try { SelectFilteredPalette(preferred ?? selectedPalette?.Id); }
        finally { refreshingNavigation = false; }
        NavigationChanged(nameof(SelectedPalette)); RefreshCopyDestination();
    }
    private void SelectFilteredPalette(Guid? preferred)
    {
        VisiblePalettes.Refresh();
        var visible = VisiblePalettes.Cast<IntentPaletteDraft>().ToArray();
        selectedPalette = visible.FirstOrDefault(x => x.Id == preferred) ?? visible.FirstOrDefault();
    }
    private void RefreshCopyDestination()
    {
        var destinations = CopyDestinations.ToArray();
        var next = selectedCopyDestination != null && destinations.Contains(selectedCopyDestination) ? selectedCopyDestination : destinations.FirstOrDefault();
        var changed = !ReferenceEquals(selectedCopyDestination, next);
        selectedCopyDestination = next;
        Raise(nameof(CopyDestinations)); Raise(nameof(SelectedCopyDestination)); Raise(nameof(CanCopyToOtherItem));
        if (changed) NavigationChanged(nameof(SelectedCopyDestination));
    }
    internal IntentSettingsItemContext ContextForPalette(IntentPaletteDraft palette)
    {
        if (palette.IsLegacyMultiType)
            return ItemContexts.SingleOrDefault(x => x.IsMultiTypeCompatibility)
                ?? throw new InvalidOperationException("複数種類Setの互換表示を開けません。");
        var owner = palette.OwnerTypeKey ?? throw new InvalidOperationException("Setの対象アイテムを特定できません。");
        return ItemContexts.SingleOrDefault(x => x.IsRealItemType && x.Key == owner)
            ?? throw new InvalidOperationException("Setの対象アイテム種類を現在のYMM4で利用できません。");
    }
    public void CreateForContext()
    {
        var context = SelectedItemContext ?? throw new InvalidOperationException("Setを使うアイテムを先に選んでください。");
        if (context.IsGeneric) { CreateGenericSet(); return; }
        if (context.IsMultiTypeCompatibility) throw new InvalidOperationException("複数種類の共有Setは新しく作成できません。1つのアイテム種類を選んでください。");
        if (context.Selection is { } selected) Create(selected);
        else
        {
            if (!context.IsRealItemType) throw new InvalidOperationException("作成先のアイテム種類を選んでください。");
            var owner = context.TypeKeys.Single();
            var target = new IntentTargetContext { ItemTypeKeys = [owner], TypeMatch = IntentTypeMatch.UniformType };
            var intent = owner == IntentSelectionContext.TypeKey(typeof(VoiceItem)) ? "表情" : "演出";
            SelectedPalette = AddDraft(new(Guid.NewGuid(), UniqueName("新しいセット", owner), intent, target, new(), [])); MarkDirty();
        }
        RefreshNavigation();
    }
}
