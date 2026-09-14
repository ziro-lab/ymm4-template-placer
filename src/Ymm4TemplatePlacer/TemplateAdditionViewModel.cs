using System.Collections.ObjectModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

// Task adapter only: the strict source locator and reference-only storage remain authoritative.
public sealed partial class PlacerViewModel
{
    private bool isAddingTemplate, refreshingAddTemplateSources;
    private Guid? addingPaletteId;
    private AssignmentRow? expressionAdditionTarget;
    private Character? expressionAdditionCharacter;
    private string? expressionAdditionCharacterName;
    private string addTemplateSearch = "", addTemplateDisplayName = "";
    private ItemTemplate? addTemplateSource;
    public ObservableCollection<ItemTemplate> AddTemplateSources { get; } = [];
    public bool IsAddingTemplate { get => isAddingTemplate; private set => Set(ref isAddingTemplate, value); }
    public string AddTemplateSearch
    {
        get => addTemplateSearch;
        set { Set(ref addTemplateSearch, value); RefreshAddTemplateSources(); }
    }
    public string AddTemplateDisplayName { get => addTemplateDisplayName; set => Set(ref addTemplateDisplayName, value); }
    public ItemTemplate? AddTemplateSource
    {
        get => addTemplateSource;
        set
        {
            if (refreshingAddTemplateSources || ReferenceEquals(addTemplateSource, value)) return;
            Set(ref addTemplateSource, value);
            var existing = ExistingAddEntries();
            AddTemplateDisplayName = existing.Length == 1 ? existing[0].DisplayName : value?.Name ?? "";
            NotifyTemplateAddition();
        }
    }
    private LibraryEntry[] ExistingAddEntries() => AddTemplateSource == null ? [] : settings.Library
        .Where(x => x.Source == TemplateLocator.Capture(AddTemplateSource)).Take(2).ToArray();
    private string? AddSourceCharacter => AddTemplateSource?.Items.Count == 1 ? ItemCharacters.Get(AddTemplateSource.Items[0])?.Name : null;
    public bool AddTemplateUsesExisting => ExistingAddEntries().Length == 1;
    public string AddTemplateTitle => expressionAdditionTarget == null ? "テンプレートを追加" : $"「{expressionAdditionCharacterName}」の表情テンプレートを追加";
    private string NewAddPaletteName => expressionAdditionTarget != null ? expressionAdditionCharacterName + "・表情" : AddSourceCharacter is string character ? character + "・テンプレート" : "よく使うテンプレート";
    public string AddTemplateDestination => addingPaletteId is Guid id
        ? settings.Palettes.SingleOrDefault(x => x.Id == id)?.Name ?? "追加先のパレットが削除されています"
        : NewAddPaletteName + "（新規）";
    public string AddTemplateCharacterSummary => expressionAdditionTarget != null ? "" : AddSourceCharacter is string character ? "キャラクター: " + character + "（自動）" : "キャラクター: 指定なし";
    public string AddTemplateNotice => AddTemplateSources.Count == 0
        ? expressionAdditionTarget == null
            ? "候補がありません。検索を空欄にするか、YMM4でアイテム1つのテンプレートを登録して［一覧更新］してください。"
            : $"候補がありません。YMM4で「{expressionAdditionCharacterName}」の表情アイテム1つをテンプレートに登録し、［一覧更新］してください。"
        : ExistingAddEntries().Length > 1 ? "同じ元テンプレートに複数の登録があります。テンプレート管理で確認するまで追加できません。"
        : AddTemplateUsesExisting ? "登録済みの表示名を使用します。名前の変更はテンプレート管理から行えます。" : "";
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
        CompleteAddTemplateCommand = new ActionCommand(_ => settingsAvailable && IsAddingTemplate && AddTemplateSource != null, _ => Guard(() => CompleteTemplateAddition()));
        CancelAddTemplateCommand = new ActionCommand(_ => true, _ => IsAddingTemplate = false);
        RefreshAddTemplatesCommand = new ActionCommand(_ => true, _ => Guard(RefreshAddTemplateSources));
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
        addTemplateSource = null; OnPropertyChanged(nameof(AddTemplateSource)); AddTemplateDisplayName = "";
        IsAddingTemplate = true; RefreshAddTemplateSources(); NotifyTemplateAddition();
    }
    private void RefreshAddTemplateSources()
    {
        var selected = addTemplateSource;
        refreshingAddTemplateSources = true;
        try
        {
            AddTemplateSources.Clear();
            foreach (var source in ItemSettings.Default.Templates.Where(x => x.Items.Count == 1 &&
                (expressionAdditionTarget == null || x.Items[0] is TachieFaceItem face && ReferenceEquals(face.Character, expressionAdditionCharacter)) &&
                (string.IsNullOrEmpty(AddTemplateSearch) || x.Name.Contains(AddTemplateSearch, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(x => x.Name, StringComparer.Ordinal)) AddTemplateSources.Add(source);
        }
        finally { refreshingAddTemplateSources = false; }
        // A filtered-out or removed source must not remain an invisible selected value.
        if (selected == null || !AddTemplateSources.Contains(selected)) AddTemplateSource = null;
        else { addTemplateSource = selected; OnPropertyChanged(nameof(AddTemplateSource)); }
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
        OnPropertyChanged(nameof(AddTemplateCharacterSummary)); OnPropertyChanged(nameof(AddTemplateNotice));
        CompleteAddTemplateCommand?.RaiseCanExecuteChanged();
    }
    public LibraryEntry CompleteTemplateAddition()
    {
        if (!IsAddingTemplate) throw new InvalidOperationException("［テンプレートを追加］から選び直してください。");
        var source = AddTemplateSource ?? throw new InvalidOperationException("元のYMM4テンプレートを選んでください。");
        if (!ItemSettings.Default.Templates.Contains(source)) throw new InvalidOperationException("元テンプレートが削除されています。入力は保持しています。YMM4側を確認してください。");
        if (expressionAdditionTarget != null && (!Rows.Contains(expressionAdditionTarget) ||
            timeline == null || !timeline.Items.Contains(expressionAdditionTarget.Target.Voice) ||
            !ReferenceEquals(expressionAdditionTarget.Target.Voice.Character, expressionAdditionCharacter) ||
            expressionAdditionTarget.Target.Voice.CharacterName != expressionAdditionCharacterName ||
            source.Items.Count != 1 || source.Items[0] is not TachieFaceItem face || !ReferenceEquals(face.Character, expressionAdditionCharacter)))
            throw new InvalidOperationException("追加先の音声・キャラクター、または表情テンプレートが変更されています。入力は保持しています。元を確認してください。");
        var existing = ExistingAddEntries();
        if (existing.Length > 1) throw new InvalidOperationException("同じ元テンプレートに複数の登録があります。テンプレート管理で確認してください。自動では選びません。");
        var character = AddSourceCharacter;
        if (character != null && !ReferenceEquals(ItemCharacters.ResolveUnique(timeline, character), ItemCharacters.Get(source.Items[0])))
            throw new InvalidOperationException("同名キャラクターを一意に特定できません。YMM4側のキャラクター名を区別してください。");
        var entry = existing.Length == 1 ? existing[0] : TemplateResolver.Reference(source, AddTemplateDisplayName, character);
        var resolution = TemplateResolver.Resolve(entry);
        if (resolution.State != TemplateReferenceState.Resolved) throw new InvalidOperationException(resolution.Message);
        var palette = addingPaletteId is Guid id ? settings.Palettes.SingleOrDefault(x => x.Id == id)
            ?? throw new InvalidOperationException("追加先のパレットが削除されています。入力は保持しています。戻って追加先を選び直してください。")
            : new PaletteDefinition(Guid.NewGuid(), character == null ? PaletteKind.Style : PaletteKind.Character, NewAddPaletteName, character, []);
        if (palette.Kind == PaletteKind.Character &&
            ((entry.CharacterName != null && entry.CharacterName != palette.CharacterName) || (character != null && character != palette.CharacterName)))
            throw new InvalidOperationException("このテンプレートのキャラクターは、追加先のパレットと違います。");
        var alreadyMember = palette.LibraryEntryIds.Contains(entry.Id);
        if (alreadyMember && expressionAdditionTarget == null) throw new InvalidOperationException("このテンプレートは既にパレットにあります。");
        var createPalette = addingPaletteId == null;
        if (!alreadyMember) EditSettings(next =>
        {
            if (existing.Length == 0) next.Library.Add(entry);
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
            next.Palettes.Single(x => x.Id == palette.Id).LibraryEntryIds.Add(entry.Id);
        });
        // Both reference creation/reuse and membership are saved atomically by EditSettings.
        SelectedPaletteEntry = PaletteEntries.FirstOrDefault(x => x.LibraryEntryId == entry.Id);
        var catalog = TemplateCatalog.Read();
        foreach (var row in Rows) row.RefreshCandidates(catalog, settings);
        OnPropertyChanged(nameof(Summary));
        IsAddingTemplate = false;
        HasError = false; Status = $"「{entry.DisplayName}」を「{palette.Name}」へ追加しました。";
        return entry;
    }
}
