using System.Collections.ObjectModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private readonly PlacerSettingsStore settingsStore = PlacerSettingsStore.CreateDefault();
    private PlacerSettings settings = new();
    private bool settingsAvailable;
    private string libraryNotice = "", librarySearch = "", libraryDisplayName = "";
    private LibraryEntryView? selectedLibraryEntry;
    private ItemTemplate? selectedSourceTemplate;
    private CharacterOption? selectedLibraryCharacter;
    public ObservableCollection<LibraryEntryView> LibraryEntries { get; } = [];
    public ObservableCollection<ItemTemplate> SourceTemplates { get; } = [];
    public ObservableCollection<CharacterOption> LibraryCharacters { get; } = [];
    public string LibraryNotice { get => libraryNotice; private set => Set(ref libraryNotice, value); }
    public string LibrarySearch { get => librarySearch; set { Set(ref librarySearch, value); RefreshLibraryEntries(); } }
    public string LibraryDisplayName { get => libraryDisplayName; set => Set(ref libraryDisplayName, value); }
    public CharacterOption? SelectedLibraryCharacter { get => selectedLibraryCharacter; set { Set(ref selectedLibraryCharacter, value); OnPropertyChanged(nameof(LibraryCharacterSummary)); } }
    public ItemTemplate? SelectedSourceTemplate
    {
        get => selectedSourceTemplate;
        set
        {
            Set(ref selectedSourceTemplate, value);
            if (value != null)
            {
                LibraryDisplayName = value.Name;
                var character = value.Items.Count == 1 ? ItemCharacters.Get(value.Items[0])?.Name : null;
                SelectedLibraryCharacter = LibraryCharacters.FirstOrDefault(x => x.Name == character) ?? LibraryCharacters.FirstOrDefault();
            }
            UpdateLibraryCommands();
        }
    }
    public LibraryEntryView? SelectedLibraryEntry
    {
        get => selectedLibraryEntry;
        set
        {
            Set(ref selectedLibraryEntry, value);
            if (value != null)
            {
                LibraryDisplayName = value.DisplayName;
                SelectedLibraryCharacter = LibraryCharacters.FirstOrDefault(x => x.Name == value.Entry.CharacterName);
                selectedSourceTemplate = TemplateResolver.Resolve(value.Entry).Template;
                OnPropertyChanged(nameof(SelectedSourceTemplate));
            }
            UpdateLibraryCommands();
        }
    }
    public ActionCommand RefreshLibraryCommand { get; private set; } = null!;
    public ActionCommand RegisterLibraryCommand { get; private set; } = null!;
    public ActionCommand SaveLibraryCommand { get; private set; } = null!;
    public ActionCommand RelinkLibraryCommand { get; private set; } = null!;
    public ActionCommand UnregisterLibraryCommand { get; private set; } = null!;
    partial void InitializeV04()
    {
        RefreshLibraryCommand = new ActionCommand(_ => true, _ => Guard(RefreshV04));
        RegisterLibraryCommand = new ActionCommand(_ => settingsAvailable && SelectedSourceTemplate != null, _ => Guard(() => RegisterLibrary()));
        SaveLibraryCommand = new ActionCommand(_ => settingsAvailable && SelectedLibraryEntry != null, _ => Guard(SaveLibrary));
        RelinkLibraryCommand = new ActionCommand(_ => settingsAvailable && SelectedLibraryEntry != null && SelectedSourceTemplate != null, _ => Guard(RelinkLibrary));
        UnregisterLibraryCommand = new ActionCommand(_ => settingsAvailable && SelectedLibraryEntry != null, _ => Guard(UnregisterLibraryFromUi));
        try
        {
            settings = settingsStore.Load();
            settingsAvailable = true;
            if (settingsStore.MigratedLegacyOnLastLoad)
                LibraryNotice = "旧LocalAppData設定をYMM4フォルダ内へ移行しました。旧設定ファイルはバックアップとして残しています。";
        }
        catch (Exception ex) { LibraryNotice = "設定を読み込めないため保存を停止しています。元ファイルは保持しています: " + ex.Message; }
        RefreshV04();
        InitializePalettes();
        InitializePresets();
    }
    partial void InitializePalettes();
    partial void InitializePresets();
    partial void RefreshPalettes();
    partial void RefreshPresets();
    partial void OnLibraryUnregistered(PlacerSettings next, Guid id);
    partial void RefreshV04()
    {
        var source = selectedSourceTemplate;
        var character = selectedLibraryCharacter?.Name;
        SourceTemplates.Clear();
        foreach (var template in ItemSettings.Default.Templates.Where(x => x.Items.Count == 1).OrderBy(x => x.Name, StringComparer.Ordinal)) SourceTemplates.Add(template);
        LibraryCharacters.Clear(); LibraryCharacters.Add(new(null, "指定なし"));
        foreach (var name in ItemCharacters.Read(timeline).Select(x => x.Name).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)) LibraryCharacters.Add(new(name, name));
        selectedSourceTemplate = source != null && SourceTemplates.Contains(source) ? source : null;
        SelectedLibraryCharacter = LibraryCharacters.FirstOrDefault(x => x.Name == character) ?? LibraryCharacters[0];
        OnPropertyChanged(nameof(SelectedSourceTemplate));
        RefreshLibraryEntries(); RefreshPalettes(); RefreshPresets(); UpdateLibraryCommands();
    }
    private void RefreshLibraryEntries()
    {
        var id = selectedLibraryEntry?.Id;
        LibraryEntries.Clear();
        foreach (var entry in settings.Library.Where(x => string.IsNullOrEmpty(LibrarySearch) || x.DisplayName.Contains(LibrarySearch, StringComparison.OrdinalIgnoreCase) || x.Source.Name.Contains(LibrarySearch, StringComparison.OrdinalIgnoreCase)))
            LibraryEntries.Add(new(entry));
        SelectedLibraryEntry = LibraryEntries.FirstOrDefault(x => x.Id == id);
    }
    private void EditSettings(Action<PlacerSettings> edit)
    {
        if (!settingsAvailable) throw new InvalidOperationException(LibraryNotice);
        var next = PlacerSettingsStore.Copy(settings); edit(next); settingsStore.Save(next); settings = next;
        RefreshV04();
    }
    public LibraryEntry RegisterLibrary()
    {
        var source = SelectedSourceTemplate ?? throw new InvalidOperationException("元のYMM4テンプレートを選んでください。");
        var entry = TemplateResolver.Reference(source, LibraryDisplayName, SelectedLibraryCharacter?.Name);
        EditSettings(next => next.Library.Add(entry));
        SelectedLibraryEntry = LibraryEntries.FirstOrDefault(x => x.Id == entry.Id);
        HasError = false; Status = $"「{entry.DisplayName}」をテンプレート管理へ登録しました。YMM4のテンプレート本体は変更していません。";
        return entry;
    }
    public void SaveLibrary()
    {
        var entry = SelectedLibraryEntry?.Entry ?? throw new InvalidOperationException("テンプレート管理の登録を選んでください。");
        var updated = entry with { DisplayName = LibraryDisplayName.Trim(), CharacterName = SelectedLibraryCharacter?.Name };
        if (TemplateResolver.Resolve(updated).State == TemplateReferenceState.CharacterMismatch) throw new InvalidOperationException("元テンプレートとキャラクターが一致していません。");
        EditSettings(next => next.Library[next.Library.FindIndex(x => x.Id == entry.Id)] = updated);
        HasError = false; Status = "表示名・キャラクターを保存しました。元テンプレート名は変更していません。";
    }
    public void RelinkLibrary()
    {
        var old = SelectedLibraryEntry?.Entry ?? throw new InvalidOperationException("再リンクする登録を選んでください。");
        var source = SelectedSourceTemplate ?? throw new InvalidOperationException("再リンク先のYMM4テンプレートを選んでください。");
        var entry = TemplateResolver.Reference(source, LibraryDisplayName, SelectedLibraryCharacter?.Name, old.Id);
        EditSettings(next => next.Library[next.Library.FindIndex(x => x.Id == old.Id)] = entry);
        HasError = false; Status = "指定したテンプレートへ再リンクしました。登録IDは維持しています。";
    }
    public void UnregisterLibrary()
    {
        var id = SelectedLibraryEntry?.Id ?? throw new InvalidOperationException("登録解除するテンプレートを選んでください。");
        EditSettings(next => { next.Library.RemoveAll(x => x.Id == id); OnLibraryUnregistered(next, id); });
        HasError = false; Status = "テンプレート管理から登録解除しました。すべてのパレットからも外しました。YMM4のテンプレートとタイムラインは変更していません。";
    }
    public string LibraryCharacterSummary
    {
        get
        {
            var chosen = SelectedLibraryCharacter?.Name;
            var actual = SelectedSourceTemplate?.Items.Count == 1 ? ItemCharacters.Get(SelectedSourceTemplate.Items[0]) : null;
            if (chosen == null) return "キャラクター: 指定なし";
            if (actual?.Name == chosen)
                return ReferenceEquals(ItemCharacters.ResolveUnique(timeline, chosen), actual)
                    ? $"キャラクター: {chosen}（自動）" : $"キャラクター: {chosen}（同名のため要確認）";
            return $"キャラクター: {chosen}（指定）";
        }
    }
    private void UpdateLibraryCommands()
    {
        OnPropertyChanged(nameof(LibraryCharacterSummary));
        RegisterLibraryCommand?.RaiseCanExecuteChanged(); SaveLibraryCommand?.RaiseCanExecuteChanged();
        RelinkLibraryCommand?.RaiseCanExecuteChanged(); UnregisterLibraryCommand?.RaiseCanExecuteChanged();
    }
}
