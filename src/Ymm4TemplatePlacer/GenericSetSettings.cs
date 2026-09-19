using System.Collections.ObjectModel;
using System.Globalization;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

// A staged editor for the existing Style Palette, not another placement model.
public sealed class GenericSetDraft : IntentEditable
{
    private readonly PaletteDefinition original;
    private string name, minimum, maximum, preferred;
    private bool useTemplateLayer;
    private IntentEntryDraft? selectedEntry;
    public Guid Id => original.Id;
    public string Name { get => name; set { if (name == value) return; name = value; Notify(); Raise(nameof(Label)); } }
    public string Label => Name;
    public bool UseTemplateLayer { get => useTemplateLayer; set { if (useTemplateLayer == value) return; useTemplateLayer = value; Notify(); Raise(nameof(UseSavedRange)); Raise(nameof(Summary)); } }
    public bool UseSavedRange => !UseTemplateLayer;
    public string Minimum { get => minimum; set { minimum = value; Notify(); Raise(nameof(Summary)); } }
    public string Maximum { get => maximum; set { maximum = value; Notify(); Raise(nameof(Summary)); } }
    public string Preferred { get => preferred; set { preferred = value; Notify(); Raise(nameof(Summary)); } }
    public string Summary => UseTemplateLayer
        ? "現在の再生位置から、テンプレートの長さ・レイヤーで配置します。選択アイテムには関連付けません。"
        : $"現在の再生位置から、テンプレートの長さで配置します。レイヤー{Minimum}〜{Maximum}の空きから{Preferred}を優先します。選択アイテムには関連付けません。";
    public ObservableCollection<IntentEntryDraft> Entries { get; } = [];
    public IntentEntryDraft? SelectedEntry { get => selectedEntry; set { if (selectedEntry == value) return; selectedEntry = value; Raise(); } }
    public GenericSetDraft(PaletteDefinition source, IReadOnlyList<LibraryEntry> library)
    {
        original = source; name = source.Name; useTemplateLayer = source.Layer.UseTemplateLayer;
        minimum = source.Layer.Minimum.ToString(CultureInfo.InvariantCulture);
        maximum = source.Layer.Maximum.ToString(CultureInfo.InvariantCulture);
        preferred = source.Layer.Preferred.ToString(CultureInfo.InvariantCulture);
        foreach (var id in source.LibraryEntryIds) AddEntry(id, library);
        Entries.CollectionChanged += (_, _) => Notify(nameof(Entries));
    }
    public void AddEntry(Guid id, IReadOnlyList<LibraryEntry> library)
    {
        var appearance = original.AppearanceFor(id);
        var entry = new IntentEntryDraft(new(id) { DisplayAlias = appearance.DisplayAlias, Color = appearance.Color, Shape = appearance.Shape }, library);
        entry.Edited += (_, _) => Notify(nameof(Entries)); Entries.Add(entry);
    }
    public void MoveEntry(int delta)
    {
        if (SelectedEntry == null) return;
        var index = Entries.IndexOf(SelectedEntry); var next = index + delta;
        if (next >= 0 && next < Entries.Count) Entries.Move(index, next);
    }
    public PaletteDefinition Build()
    {
        var layer = UseTemplateLayer ? original.Layer with { UseTemplateLayer = true } : original.Layer with
        {
            UseTemplateLayer = false, Minimum = Number(Minimum, "最小レイヤー"), Maximum = Number(Maximum, "最大レイヤー"), Preferred = Number(Preferred, "優先レイヤー")
        };
        layer.Validate();
        var appearances = Entries.Select(x => x.Build()).ToDictionary(x => x.LibraryEntryId, x => new PaletteTileAppearance(x.DisplayAlias, x.Color, x.Shape));
        foreach (var id in appearances.Where(x => x.Value == PaletteTileAppearance.Default).Select(x => x.Key).ToArray()) appearances.Remove(id);
        return original with { Name = Name.Trim(), LibraryEntryIds = Entries.Select(x => x.LibraryEntryId).ToList(), Layer = layer,
            TileAppearance = appearances.Count == 0 ? null : appearances };
    }
}

public sealed partial class IntentSettingsSession
{
    private GenericSetDraft? selectedGenericSet;
    public ObservableCollection<GenericSetDraft> GenericSets { get; } = [];
    public GenericSetDraft? SelectedGenericSet
    {
        get => selectedGenericSet;
        set { if (value == selectedGenericSet) return; selectedGenericSet = value; NavigationChanged(); Raise(nameof(HasSelectedSet)); Raise(nameof(HasSelectedSetEntry)); }
    }
    public bool HasSelectedSet => IsGenericContext ? SelectedGenericSet != null : SelectedPalette != null;
    public bool HasSelectedSetEntry => IsGenericContext ? SelectedGenericSet?.SelectedEntry != null : SelectedPalette?.SelectedEntry != null;
    public string SelectedSetName => IsGenericContext ? SelectedGenericSet?.Name ?? "" : SelectedPalette?.Name ?? "";
    public int SelectedSetEntryCount => IsGenericContext ? SelectedGenericSet?.Entries.Count ?? 0 : SelectedPalette?.Entries.Count ?? 0;
    private void InitializeGenericSets()
    {
        foreach (var palette in working.Palettes.Where(x => x.Kind == PaletteKind.Style)) AddGenericDraft(palette);
        selectedGenericSet = GenericSets.FirstOrDefault();
        GenericSets.CollectionChanged += (_, _) => MarkDirty();
    }
    private GenericSetDraft AddGenericDraft(PaletteDefinition palette)
    {
        var draft = new GenericSetDraft(palette, working.Library);
        draft.Edited += (_, _) => MarkDirty();
        draft.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(GenericSetDraft.SelectedEntry)) NavigationChanged(nameof(SelectedGenericSet)); };
        GenericSets.Add(draft); return draft;
    }
    private void ApplyGenericSets(PlacerSettings next)
    {
        // Preserve Character palettes and original interleaving; Style order is edited independently.
        var drafts = new Queue<PaletteDefinition>(GenericSets.Select(x => x.Build()));
        var palettes = new List<PaletteDefinition>();
        foreach (var palette in working.Palettes)
        {
            if (palette.Kind != PaletteKind.Style) palettes.Add(palette);
            else if (drafts.Count != 0) palettes.Add(drafts.Dequeue());
        }
        palettes.AddRange(drafts); next.Palettes = palettes;
        if (next.ManualStylePaletteId is Guid id && !palettes.Any(x => x.Id == id && x.Kind == PaletteKind.Style)) next.ManualStylePaletteId = null;
    }
    private string UniqueGenericName(string stem)
    {
        if (!GenericSets.Any(x => x.Name == stem)) return stem;
        for (var i = 2; ; i++) { var name = $"{stem} {i}"; if (!GenericSets.Any(x => x.Name == name)) return name; }
    }
    private void CreateGenericSet() => SelectedGenericSet = AddGenericDraft(new(Guid.NewGuid(), PaletteKind.Style, UniqueGenericName("新しい汎用セット"), null, []));
    private void DuplicateGenericSet()
    {
        var source = SelectedGenericSet?.Build() ?? throw new InvalidOperationException("複製するセットを選んでください。");
        SelectedGenericSet = AddGenericDraft(source with { Id = Guid.NewGuid(), Name = UniqueGenericName(source.Name), LibraryEntryIds = source.LibraryEntryIds.ToList() });
    }
    private void RemoveGenericSet()
    {
        if (SelectedGenericSet == null) return;
        GenericSets.Remove(SelectedGenericSet); SelectedGenericSet = GenericSets.FirstOrDefault();
    }
    private void MoveGenericSet(int delta)
    {
        if (SelectedGenericSet == null) return;
        var index = GenericSets.IndexOf(SelectedGenericSet); var next = index + delta;
        if (next >= 0 && next < GenericSets.Count) GenericSets.Move(index, next);
    }
    public void RemoveSelectedSetEntry()
    {
        if (IsGenericContext)
        {
            if (SelectedGenericSet?.SelectedEntry is { } entry) { SelectedGenericSet.Entries.Remove(entry); SelectedGenericSet.SelectedEntry = null; }
        }
        else if (SelectedPalette?.SelectedEntry is { } entry) { SelectedPalette.Entries.Remove(entry); SelectedPalette.SelectedEntry = null; }
    }
    private int AddGenericSources()
    {
        var palette = SelectedGenericSet ?? throw new InvalidOperationException("追加先の汎用セットを選んでください。");
        var selected = Sources.Where(x => x.Selected).ToArray(); if (selected.Length == 0) return 0;
        var library = working.Library.ToList(); var additions = new List<Guid>();
        foreach (var option in selected)
        {
            if (!ItemSettings.Default.Templates.Contains(option.Source) || !option.Locator.Matches(option.Source))
                throw new InvalidOperationException("選択後に元テンプレートが変更されました。全件追加していません。");
            var matches = library.Where(x => x.Source == option.Locator).Take(2).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("同じ元テンプレートへの登録が複数あります。登録を確認してください。");
            var entry = matches.FirstOrDefault() ?? TemplateResolver.Reference(option.Source, option.Name, null);
            // Keep the existing QuickDrop single-item contract. No bundle/geometry implementation here.
            var resolved = TemplateResolver.Resolve(entry);
            if (resolved.State != TemplateReferenceState.Resolved || resolved.Item == null || !ReferenceEquals(TemplateResolver.RequireBundle(entry).Template, option.Source))
                throw new InvalidOperationException($"「{option.Name}」は汎用配置で使えません。1アイテムのテンプレートを選んでください。全件追加していません。");
            if (matches.Length == 0) library.Add(entry);
            if (!palette.Entries.Any(x => x.LibraryEntryId == entry.Id) && !additions.Contains(entry.Id)) additions.Add(entry.Id);
        }
        working.Library = library;
        foreach (var id in additions) palette.AddEntry(id, working.Library);
        foreach (var option in selected) option.Selected = false;
        MarkDirty(); return additions.Count;
    }
}
