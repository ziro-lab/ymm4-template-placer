using System.Collections.ObjectModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

// Task adapter only: the strict source locator and reference-only storage remain authoritative.
public sealed partial class PlacerViewModel
{
    private bool isAddingTemplate, refreshingAddTemplateSources;
    private Guid? addingPaletteId;
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
    private string NewAddPaletteName => AddSourceCharacter is string character ? character + "・テンプレート" : "よく使うテンプレート";
    public string AddTemplateDestination => addingPaletteId is Guid id
        ? settings.Palettes.SingleOrDefault(x => x.Id == id)?.Name ?? "追加先のパレットが削除されています"
        : NewAddPaletteName + "（新規）";
    public string AddTemplateCharacterSummary => AddSourceCharacter is string character ? "キャラクター: " + character + "（自動）" : "キャラクター: 指定なし";
    public string AddTemplateNotice => AddTemplateSources.Count == 0
        ? "候補がありません。検索を空欄にするか、YMM4でアイテム1つのテンプレートを登録して［一覧更新］してください。"
        : ExistingAddEntries().Length > 1 ? "同じ元テンプレートに複数の登録があります。テンプレート管理で確認するまで追加できません。"
        : AddTemplateUsesExisting ? "登録済みの表示名を使用します。名前の変更はテンプレート管理から行えます。" : "";
    public ActionCommand OpenAddTemplateCommand { get; private set; } = null!;
    public ActionCommand CompleteAddTemplateCommand { get; private set; } = null!;
    public ActionCommand CancelAddTemplateCommand { get; private set; } = null!;
    public ActionCommand RefreshAddTemplatesCommand { get; private set; } = null!;
    private void InitializeTemplateAddition()
    {
        OpenAddTemplateCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(BeginTemplateAddition));
        CompleteAddTemplateCommand = new ActionCommand(_ => settingsAvailable && IsAddingTemplate && AddTemplateSource != null, _ => Guard(() => CompleteTemplateAddition()));
        CancelAddTemplateCommand = new ActionCommand(_ => true, _ => IsAddingTemplate = false);
        RefreshAddTemplatesCommand = new ActionCommand(_ => true, _ => Guard(RefreshAddTemplateSources));
    }
    public void BeginTemplateAddition()
    {
        if (CurrentPalette == null && settings.Palettes.Count != 0)
            throw new InvalidOperationException("追加先のパレットを選んでください。");
        // Freeze the destination: a later Timeline selection must not redirect the user's add operation.
        addingPaletteId = CurrentPalette?.Id;
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
                (string.IsNullOrEmpty(AddTemplateSearch) || x.Name.Contains(AddTemplateSearch, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(x => x.Name, StringComparer.Ordinal)) AddTemplateSources.Add(source);
        }
        finally { refreshingAddTemplateSources = false; }
        // A filtered-out or removed source must not remain an invisible selected value.
        if (selected == null || !AddTemplateSources.Contains(selected)) AddTemplateSource = null;
        else { addTemplateSource = selected; OnPropertyChanged(nameof(AddTemplateSource)); }
        NotifyTemplateAddition();
    }
    private void NotifyTemplateAddition()
    {
        OnPropertyChanged(nameof(AddTemplateDestination)); OnPropertyChanged(nameof(AddTemplateUsesExisting));
        OnPropertyChanged(nameof(AddTemplateCharacterSummary)); OnPropertyChanged(nameof(AddTemplateNotice));
        CompleteAddTemplateCommand?.RaiseCanExecuteChanged();
    }
    public LibraryEntry CompleteTemplateAddition()
    {
        if (!IsAddingTemplate) throw new InvalidOperationException("［テンプレートを追加］から選び直してください。");
        var source = AddTemplateSource ?? throw new InvalidOperationException("元のYMM4テンプレートを選んでください。");
        if (!ItemSettings.Default.Templates.Contains(source)) throw new InvalidOperationException("元テンプレートが削除されています。入力は保持しています。YMM4側を確認してください。");
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
        if (palette.LibraryEntryIds.Contains(entry.Id)) throw new InvalidOperationException("このテンプレートは既にパレットにあります。");
        var createPalette = addingPaletteId == null;
        EditSettings(next =>
        {
            if (existing.Length == 0) next.Library.Add(entry);
            if (createPalette)
            {
                next.Palettes.Add(palette); next.PaletteMode = palette.Kind;
                if (palette.Kind == PaletteKind.Character) next.ManualCharacterPaletteId = palette.Id;
                else next.ManualStylePaletteId = palette.Id;
            }
            next.Palettes.Single(x => x.Id == palette.Id).LibraryEntryIds.Add(entry.Id);
        });
        // Both reference creation/reuse and membership are saved atomically by EditSettings.
        SelectedPaletteEntry = PaletteEntries.FirstOrDefault(x => x.LibraryEntryId == entry.Id);
        foreach (var row in Rows) row.PreferPalette(settings);
        IsAddingTemplate = false;
        HasError = false; Status = $"「{entry.DisplayName}」を「{palette.Name}」へ追加しました。";
        return entry;
    }
}
