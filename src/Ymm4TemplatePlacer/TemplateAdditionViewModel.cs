using System.Collections.ObjectModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed class AddTemplateSourceChoice : Bindable
{
    private bool isSelected;
    private readonly Action<AddTemplateSourceChoice, bool> selectionChanged;
    public ItemTemplate Source { get; }
    public string Name => Source.Name;
    public string CharacterName { get; }
    public bool IsAlreadyAdded { get; }
    public bool CanSelect => !IsAlreadyAdded;
    public string StateLabel => IsAlreadyAdded ? "追加済み" : "";
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (IsAlreadyAdded && value || isSelected == value) return;
            isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            selectionChanged(this, value);
        }
    }
    internal AddTemplateSourceChoice(ItemTemplate source, string? characterName, bool selected, bool alreadyAdded,
        Action<AddTemplateSourceChoice, bool> selectionChanged)
    {
        Source = source; CharacterName = characterName ?? "指定なし"; isSelected = selected;
        IsAlreadyAdded = alreadyAdded; this.selectionChanged = selectionChanged;
    }
}

// Task adapter only: the strict source locator and reference-only storage remain authoritative.
public sealed partial class PlacerViewModel
{
    private bool isAddingTemplate, refreshingAddTemplateSources, changingAddTemplateSelection;
    private Guid? addingPaletteId;
    private AssignmentRow? expressionAdditionTarget;
    private Character? expressionAdditionCharacter;
    private string? expressionAdditionCharacterName;
    private string addTemplateSearch = "", addTemplateDisplayName = "";
    private ItemTemplate? addTemplateSource;
    private readonly List<ItemTemplate> addTemplateSelections = [];
    public ObservableCollection<ItemTemplate> AddTemplateSources { get; } = [];
    public ObservableCollection<AddTemplateSourceChoice> AddTemplateChoices { get; } = [];
    public bool IsAddingTemplate { get => isAddingTemplate; private set => Set(ref isAddingTemplate, value); }
    public string AddTemplateSearch
    {
        get => addTemplateSearch;
        set { Set(ref addTemplateSearch, value); RefreshAddTemplateSources(); }
    }
    public string AddTemplateDisplayName { get => addTemplateDisplayName; set => Set(ref addTemplateDisplayName, value); }
    // Compatibility/basic-path projection: exactly one selected source is exposed here.
    public ItemTemplate? AddTemplateSource
    {
        get => addTemplateSelections.Count == 1 ? addTemplateSelections[0] : null;
        set
        {
            if (refreshingAddTemplateSources || changingAddTemplateSelection ||
                (value != null && addTemplateSelections.Count == 1 && ReferenceEquals(addTemplateSelections[0], value))) return;
            ReplaceAddTemplateSelections(value == null ? [] : [value], true);
        }
    }
    internal IReadOnlyList<ItemTemplate> SelectedAddTemplateSources => addTemplateSelections.ToArray();
    public int AddTemplateSelectedCount => addTemplateSelections.Count;
    public bool AddTemplateIsSingleSelection => addTemplateSelections.Count == 1;
    public string AddTemplateSelectionSummary => addTemplateSelections.Count == 0 ? "" : $"{addTemplateSelections.Count}件選択中";
    public string AddTemplateActionLabel => addTemplateSelections.Count <= 1 ? "パレットへ追加" : $"{addTemplateSelections.Count}件をパレットへ追加";
    public ActionCommand ClearAddTemplateSelectionCommand { get; private set; } = null!;

    private LibraryEntry[] ExistingAddEntries(ItemTemplate source) => settings.Library
        .Where(x => x.Source == TemplateLocator.Capture(source)).Take(2).ToArray();
    private LibraryEntry[] ExistingAddEntries() => AddTemplateSource == null ? [] : ExistingAddEntries(AddTemplateSource);
    private static Character? SourceCharacterObject(ItemTemplate source) => source.Items.Count == 1 ? ItemCharacters.Get(source.Items[0]) : null;
    private static string? SourceCharacterName(ItemTemplate source) => SourceCharacterObject(source)?.Name;
    private PaletteDefinition? FrozenAddPalette => addingPaletteId is Guid id ? settings.Palettes.SingleOrDefault(x => x.Id == id) : null;
    private Character? DerivedNewPaletteCharacter
    {
        get
        {
            if (expressionAdditionTarget != null) return expressionAdditionCharacter;
            if (addTemplateSelections.Count == 0) return null;
            var characters = addTemplateSelections.Select(SourceCharacterObject).ToArray();
            if (characters.Any(x => x == null)) return null;
            var first = characters[0]!;
            return characters.All(x => ReferenceEquals(x, first)) && ReferenceEquals(ItemCharacters.ResolveUnique(timeline, first.Name), first) ? first : null;
        }
    }
    public bool AddTemplateUsesExisting => AddTemplateIsSingleSelection && ExistingAddEntries().Length == 1;
    public string AddTemplateTitle => expressionAdditionTarget == null ? "テンプレートを追加" : $"「{expressionAdditionCharacterName}」の表情テンプレートを追加";
    private string NewAddPaletteName => expressionAdditionTarget != null ? expressionAdditionCharacterName + "・表情" :
        DerivedNewPaletteCharacter is Character character ? character.Name + "・テンプレート" : "よく使うテンプレート";
    public string AddTemplateDestination => addingPaletteId is Guid id
        ? settings.Palettes.SingleOrDefault(x => x.Id == id)?.Name ?? "追加先のパレットが削除されています"
        : NewAddPaletteName + "（新規）";
    public string AddTemplateCharacterSummary
    {
        get
        {
            if (expressionAdditionTarget != null) return "";
            if (FrozenAddPalette is { Kind: PaletteKind.Character } palette) return "キャラクター: " + palette.CharacterName + "（連動）";
            if (addingPaletteId != null) return "キャラクター: 指定なし";
            return DerivedNewPaletteCharacter is Character character ? "キャラクター: " + character.Name + "（自動）" :
                addTemplateSelections.Count > 1 ? "キャラクター: 指定なし（手動パレット）" : "キャラクター: 指定なし";
        }
    }
    public string AddTemplateNotice => AddTemplateSources.Count == 0
        ? addTemplateSelections.Count > 0 ? $"検索結果はありません。選択中の{addTemplateSelections.Count}件は保持されています。"
            : expressionAdditionTarget == null
                ? "候補がありません。検索を空欄にするか、YMM4でアイテム1つのテンプレートを登録して［一覧更新］してください。"
                : $"候補がありません。YMM4で「{expressionAdditionCharacterName}」の表情アイテム1つをテンプレートに登録し、［一覧更新］してください。"
        : AddTemplateIsSingleSelection && ExistingAddEntries().Length > 1 ? "同じ元テンプレートに複数の登録があります。テンプレート管理で確認するまで追加できません。"
        : AddTemplateUsesExisting ? "登録済みの表示名を使用します。名前の変更はテンプレート管理から行えます。"
        : addTemplateSelections.Count > 1 ? "複数選択では元テンプレート名を表示名に使います。必要なら追加後にテンプレート管理で変更できます。" : "";
    public ActionCommand AddExpressionTemplateCommand { get; private set; } = null!;
    public ActionCommand OpenAddTemplateCommand { get; private set; } = null!;
    public ActionCommand CompleteAddTemplateCommand { get; private set; } = null!;
    public ActionCommand CancelAddTemplateCommand { get; private set; } = null!;
    public ActionCommand RefreshAddTemplatesCommand { get; private set; } = null!;
    private void InitializeTemplateAddition()
    {
        InitializeTaskNavigation();
        AddExpressionTemplateCommand = new ActionCommand(x => settingsAvailable && x is AssignmentRow, x => Guard(() => BeginExpressionTemplateAddition((AssignmentRow)x!)));
        OpenAddTemplateCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(BeginTemplateAddition));
        CompleteAddTemplateCommand = new ActionCommand(_ => settingsAvailable && IsAddingTemplate && addTemplateSelections.Count > 0, _ => Guard(() => CompleteTemplateAddition()));
        CancelAddTemplateCommand = new ActionCommand(_ => true, _ => IsAddingTemplate = false);
        RefreshAddTemplatesCommand = new ActionCommand(_ => true, _ => Guard(RefreshAddTemplateSources));
        ClearAddTemplateSelectionCommand = new ActionCommand(_ => addTemplateSelections.Count > 0, _ => ReplaceAddTemplateSelections([], true));
    }
    public void BeginTemplateAddition()
    {
        if (CurrentPalette == null && settings.Palettes.Count != 0)
            throw new InvalidOperationException("追加先のパレットを選んでください。");
        expressionAdditionTarget = null; expressionAdditionCharacter = null; expressionAdditionCharacterName = null;
        // Freeze the destination: a later Timeline selection must not redirect the user's add operation.
        addingPaletteId = CurrentPalette?.Id;
        StartTemplateAddition();
    }
    public void BeginExpressionTemplateAddition(AssignmentRow row)
    {
        var current = RequireTimeline();
        if (!Rows.Contains(row) || !current.Items.Contains(row.Target.Voice) || row.Target.Voice.CharacterName != row.Character)
            throw new InvalidOperationException("表情一覧の音声が変更されています。［更新］してから追加してください。");
        var character = ItemCharacters.ResolveUnique(current, row.Character);
        if (character == null || !ReferenceEquals(character, row.Target.Voice.Character))
            throw new InvalidOperationException("キャラクターを一意に特定できません。YMM4側で同名キャラクターを区別してください。");
        var palettes = settings.Palettes.Where(x => x.Kind == PaletteKind.Character && x.CharacterName == row.Character).Take(2).ToArray();
        if (palettes.Length > 1) throw new InvalidOperationException("追加先のキャラクターパレットを一意に特定できません。");
        expressionAdditionTarget = row; expressionAdditionCharacter = character; expressionAdditionCharacterName = row.Character;
        addingPaletteId = palettes.SingleOrDefault()?.Id;
        StartTemplateAddition();
    }
    private void StartTemplateAddition()
    {
        addTemplateSearch = ""; OnPropertyChanged(nameof(AddTemplateSearch));
        ReplaceAddTemplateSelections([], false); AddTemplateDisplayName = "";
        IsAddingTemplate = true; RefreshAddTemplateSources(); NotifyTemplateAddition();
    }
    private bool AddSourceAllowed(ItemTemplate source)
    {
        if (source.Items.Count != 1) return false;
        if (expressionAdditionTarget != null)
            return source.Items[0] is TachieFaceItem face && ReferenceEquals(face.Character, expressionAdditionCharacter);
        if (FrozenAddPalette is not { Kind: PaletteKind.Character } palette) return true;
        var character = SourceCharacterObject(source);
        return character == null || string.Equals(character.Name, palette.CharacterName, StringComparison.Ordinal);
    }
    private bool IsAlreadyInFrozenPalette(ItemTemplate source)
    {
        var palette = FrozenAddPalette;
        if (palette == null) return false;
        var existing = ExistingAddEntries(source);
        return existing.Length == 1 && palette.LibraryEntryIds.Contains(existing[0].Id);
    }
    private void RefreshAddTemplateSources()
    {
        refreshingAddTemplateSources = true;
        try
        {
            AddTemplateSources.Clear(); AddTemplateChoices.Clear();
            foreach (var source in ItemSettings.Default.Templates.Where(AddSourceAllowed)
                .Where(x => string.IsNullOrEmpty(AddTemplateSearch) || x.Name.Contains(AddTemplateSearch, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                AddTemplateSources.Add(source);
                AddTemplateChoices.Add(new(source, SourceCharacterName(source), addTemplateSelections.Any(x => ReferenceEquals(x, source)),
                    IsAlreadyInFrozenPalette(source), AddTemplateChoiceChanged));
            }
        }
        finally { refreshingAddTemplateSources = false; }
        NotifyTemplateAddition();
    }
    private void AddTemplateChoiceChanged(AddTemplateSourceChoice choice, bool selected)
    {
        if (changingAddTemplateSelection) return;
        if (selected)
        {
            if (!addTemplateSelections.Any(x => ReferenceEquals(x, choice.Source))) addTemplateSelections.Add(choice.Source);
        }
        else addTemplateSelections.RemoveAll(x => ReferenceEquals(x, choice.Source));
        UpdateAddTemplateSelectionProjection(true);
    }
    private void ReplaceAddTemplateSelections(IEnumerable<ItemTemplate> sources, bool refreshChoices)
    {
        changingAddTemplateSelection = true;
        try
        {
            addTemplateSelections.Clear();
            foreach (var source in sources)
                if (!addTemplateSelections.Any(x => ReferenceEquals(x, source))) addTemplateSelections.Add(source);
        }
        finally { changingAddTemplateSelection = false; }
        UpdateAddTemplateSelectionProjection(true);
        if (refreshChoices) RefreshAddTemplateSources();
    }
    internal void RestoreAddTemplateSelections(IEnumerable<ItemTemplate> sources, string displayName)
    {
        ReplaceAddTemplateSelections(sources, true);
        if (addTemplateSelections.Count == 1) AddTemplateDisplayName = displayName;
    }
    private void UpdateAddTemplateSelectionProjection(bool resetSingleDisplayName)
    {
        var previous = addTemplateSource;
        addTemplateSource = addTemplateSelections.Count == 1 ? addTemplateSelections[0] : null;
        if (!ReferenceEquals(previous, addTemplateSource)) OnPropertyChanged(nameof(AddTemplateSource));
        if (resetSingleDisplayName)
        {
            if (addTemplateSource != null)
            {
                var existing = ExistingAddEntries(addTemplateSource);
                AddTemplateDisplayName = existing.Length == 1 ? existing[0].DisplayName : addTemplateSource.Name;
            }
            else if (addTemplateSelections.Count != 1) AddTemplateDisplayName = "";
        }
        NotifyTemplateAddition();
    }
    private void ReturnToTemplateAddition()
    {
        if (!IsAddingTemplate) return;
        var existing = ExistingAddEntries();
        if (existing.Length == 1) AddTemplateDisplayName = existing[0].DisplayName;
        NotifyTemplateAddition();
    }
    private void NotifyTemplateAddition()
    {
        OnPropertyChanged(nameof(AddTemplateDestination)); OnPropertyChanged(nameof(AddTemplateUsesExisting)); OnPropertyChanged(nameof(AddTemplateTitle));
        OnPropertyChanged(nameof(AddTemplateCharacterSummary)); OnPropertyChanged(nameof(AddTemplateNotice)); OnPropertyChanged(nameof(AddTemplateSelectedCount));
        OnPropertyChanged(nameof(AddTemplateIsSingleSelection)); OnPropertyChanged(nameof(AddTemplateSelectionSummary)); OnPropertyChanged(nameof(AddTemplateActionLabel));
        CompleteAddTemplateCommand?.RaiseCanExecuteChanged(); ClearAddTemplateSelectionCommand?.RaiseCanExecuteChanged();
    }

    private sealed record TemplateAddPlan(ItemTemplate Source, LibraryEntry Entry, bool NewEntry, bool AlreadyMember);

    private void ValidateExpressionAddTarget(IReadOnlyList<ItemTemplate> sources)
    {
        if (expressionAdditionTarget == null) return;
        if (!Rows.Contains(expressionAdditionTarget) || timeline == null || !timeline.Items.Contains(expressionAdditionTarget.Target.Voice) ||
            !ReferenceEquals(expressionAdditionTarget.Target.Voice.Character, expressionAdditionCharacter) ||
            expressionAdditionTarget.Target.Voice.CharacterName != expressionAdditionCharacterName)
            throw new InvalidOperationException("追加先の音声・キャラクターが変更されています。選択は保持しています。元を確認してください。");
        if (sources.Any(source => source.Items.Count != 1 || source.Items[0] is not TachieFaceItem face || !ReferenceEquals(face.Character, expressionAdditionCharacter)))
            throw new InvalidOperationException("追加する表情テンプレートのキャラクターが変更されています。選択は保持しています。元を確認してください。");
    }
    private PaletteDefinition PlanAddDestination(IReadOnlyList<ItemTemplate> sources)
    {
        if (addingPaletteId is Guid id)
            return settings.Palettes.SingleOrDefault(x => x.Id == id)
                ?? throw new InvalidOperationException("追加先のパレットが削除されています。選択は保持しています。戻って追加先を選び直してください。");
        var character = expressionAdditionTarget != null ? expressionAdditionCharacter : DeriveBatchCharacter(sources);
        return new PaletteDefinition(Guid.NewGuid(), character == null ? PaletteKind.Style : PaletteKind.Character,
            expressionAdditionTarget != null ? expressionAdditionCharacterName + "・表情" : character != null ? character.Name + "・テンプレート" : "よく使うテンプレート",
            character?.Name, []);
    }
    private Character? DeriveBatchCharacter(IReadOnlyList<ItemTemplate> sources)
    {
        if (sources.Count == 0) return null;
        var characters = sources.Select(SourceCharacterObject).ToArray();
        if (characters.Any(x => x == null)) return null;
        var first = characters[0]!;
        return characters.All(x => ReferenceEquals(x, first)) && ReferenceEquals(ItemCharacters.ResolveUnique(timeline, first.Name), first) ? first : null;
    }
    private TemplateAddPlan PlanSource(ItemTemplate source, PaletteDefinition palette, int batchCount)
    {
        if (!ItemSettings.Default.Templates.Contains(source))
            throw new InvalidOperationException($"元テンプレート「{source.Name}」が削除されています。選択は保持しています。YMM4側を確認してください。");
        if (!AddSourceAllowed(source))
            throw new InvalidOperationException($"「{source.Name}」は現在の追加先では使用できません。選択は保持しています。");
        var existing = ExistingAddEntries(source);
        if (existing.Length > 1)
            throw new InvalidOperationException($"「{source.Name}」は同じ元テンプレートに複数の登録があります。テンプレート管理で確認してください。自動では選びません。");
        var actualCharacter = SourceCharacterObject(source);
        var character = actualCharacter?.Name;
        if (character != null && !ReferenceEquals(ItemCharacters.ResolveUnique(timeline, character), actualCharacter))
            throw new InvalidOperationException($"「{source.Name}」の同名キャラクターを一意に特定できません。YMM4側のキャラクター名を区別してください。");
        var displayName = batchCount == 1 ? AddTemplateDisplayName : source.Name;
        var entry = existing.Length == 1 ? existing[0] : TemplateResolver.Reference(source, displayName, character);
        var resolution = TemplateResolver.Resolve(entry);
        if (resolution.State != TemplateReferenceState.Resolved) throw new InvalidOperationException($"「{source.Name}」: {resolution.Message}");
        if (palette.Kind == PaletteKind.Character &&
            ((entry.CharacterName != null && entry.CharacterName != palette.CharacterName) || (character != null && character != palette.CharacterName)))
            throw new InvalidOperationException($"「{source.Name}」のキャラクターは、追加先のパレットと違います。");
        return new(source, entry, existing.Length == 0, palette.LibraryEntryIds.Contains(entry.Id));
    }
    public LibraryEntry CompleteTemplateAddition()
    {
        if (!IsAddingTemplate) throw new InvalidOperationException("［テンプレートを追加］から選び直してください。");
        var sources = addTemplateSelections.ToArray();
        if (sources.Length == 0) throw new InvalidOperationException("元のYMM4テンプレートを1件以上選んでください。");
        ValidateExpressionAddTarget(sources);
        var palette = PlanAddDestination(sources);
        var plans = sources.Select(source => PlanSource(source, palette, sources.Length)).ToArray();
        var createPalette = addingPaletteId == null;
        var additions = plans.Where(x => !x.AlreadyMember).ToArray();
        if (createPalette || additions.Length > 0)
        {
            EditSettings(next =>
            {
                foreach (var plan in plans.Where(x => x.NewEntry))
                    if (!next.Library.Any(x => x.Id == plan.Entry.Id)) next.Library.Add(plan.Entry);
                if (createPalette)
                {
                    next.Palettes.Add(palette);
                    if (expressionAdditionTarget == null || next.Palettes.Count == 1)
                    {
                        next.PaletteMode = palette.Kind;
                        if (palette.Kind == PaletteKind.Character) next.ManualCharacterPaletteId = palette.Id;
                        else next.ManualStylePaletteId = palette.Id;
                    }
                }
                var target = next.Palettes.Single(x => x.Id == palette.Id);
                foreach (var plan in additions)
                    if (!target.LibraryEntryIds.Contains(plan.Entry.Id)) target.LibraryEntryIds.Add(plan.Entry.Id);
            });
        }
        // Reference creation/reuse and all memberships are one settings transaction; candidate refresh happens once after it.
        var catalog = TemplateCatalog.Read();
        foreach (var row in Rows) row.RefreshCandidates(catalog, settings);
        OnPropertyChanged(nameof(Summary));
        var firstAdded = plans.FirstOrDefault(x => !x.AlreadyMember)?.Entry ?? plans[0].Entry;
        SelectedPaletteEntry = PaletteEntries.FirstOrDefault(x => x.LibraryEntryId == firstAdded.Id);
        IsAddingTemplate = false;
        HasError = false;
        Status = plans.Length == 1
            ? plans[0].AlreadyMember ? $"「{plans[0].Entry.DisplayName}」は既に「{palette.Name}」にあります。候補を更新しました。" : $"「{plans[0].Entry.DisplayName}」を「{palette.Name}」へ追加しました。"
            : $"{plans.Length}件を「{palette.Name}」へ確認し、{additions.Length}件を追加しました。";
        return firstAdded;
    }
}
