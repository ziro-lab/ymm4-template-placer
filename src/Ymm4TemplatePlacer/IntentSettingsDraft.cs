using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Data;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public abstract class IntentEditable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Edited;
    protected void Notify([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new(name)); Edited?.Invoke(this, EventArgs.Empty);
    }
    protected static int Number(string value, string label) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
        ? n : throw new InvalidOperationException($"{label}は整数で入力してください。");
    protected static int? OptionalNumber(string value, string label) => string.IsNullOrWhiteSpace(value) ? null : Number(value, label);
}
public sealed record IntentOption<T>(T Value, string Name);
public sealed class IntentTypeOption(string key, string name, bool included) : IntentEditable
{
    private bool selected = included;
    public string Key => key;
    public string Name => name;
    public bool Selected { get => selected; set { if (selected == value) return; selected = value; Notify(); } }
}
public sealed class IntentSourceOption(ItemTemplate source) : IntentEditable
{
    private bool selected;
    public ItemTemplate Source { get; } = source;
    public TemplateLocator Locator { get; } = TemplateLocator.Capture(source);
    public string Name { get; } = source.Name;
    public string Label { get; } = $"{source.Name}（{source.Items.Count}アイテム）";
    public bool Selected { get => selected; set { if (selected == value) return; selected = value; Notify(); } }
}
public sealed class IntentEntryDraft : IntentEditable
{
    private string start, end, length;
    private bool intrinsic;
    public Guid LibraryEntryId { get; }
    public string Name { get; }
    public string SourceDetail { get; }
    public string StartOffset { get => start; set { start = value; Notify(); } }
    public string EndOffset { get => end; set { end = value; Notify(); } }
    public string FixedDuration { get => length; set { length = value; Notify(); } }
    public bool UseTemplateDuration { get => intrinsic; set { intrinsic = value; Notify(); } }
    public IntentEntryDraft(IntentEntry entry, IReadOnlyList<LibraryEntry> library)
    {
        LibraryEntryId = entry.LibraryEntryId;
        var source = library.SingleOrDefault(x => x.Id == LibraryEntryId);
        Name = source?.DisplayName ?? "参照切れ";
        SourceDetail = source == null ? "元の登録がありません。" : source.Source.Name + "\n" + TemplateResolver.ResolveBundle(source).Message;
        start = entry.StartOffsetDelta.ToString(CultureInfo.InvariantCulture); end = entry.EndOffsetDelta.ToString(CultureInfo.InvariantCulture);
        length = entry.FixedDurationOverride?.ToString(CultureInfo.InvariantCulture) ?? ""; intrinsic = entry.UseTemplateDuration;
    }
    public IntentEntry Build() => new(LibraryEntryId) { StartOffsetDelta = Number(StartOffset, "演出の開始差分"),
        EndOffsetDelta = Number(EndOffset, "演出の終了差分"), FixedDurationOverride = OptionalNumber(FixedDuration, "演出の固定長"), UseTemplateDuration = UseTemplateDuration };
}

public sealed class IntentPaletteDraft : IntentEditable
{
    private IntentPalette model;
    private readonly Dictionary<string, string> text = new(StringComparer.Ordinal);
    private IntentEntryDraft? selectedEntry;
    public Guid Id => model.Id;
    private string Get([CallerMemberName] string key = "") => text.GetValueOrDefault(key, "");
    private void Put(string value, [CallerMemberName] string key = "") { text[key] = value; Notify(key); if (key == nameof(Name)) Notify(nameof(Label)); }
    private void Change(IntentPalette value, [CallerMemberName] string name = "") { model = value; Notify(name); }
    public string Name { get => Get(); set => Put(value); }
    public string Label => Name;
    public string Intent { get => Get(); set => Put(value); }
    public string CharacterName { get => Get(); set => Put(value); }
    public string MinimumCount { get => Get(); set => Put(value); }
    public string MaximumCount { get => Get(); set => Put(value); }
    public string StartOffset { get => Get(); set => Put(value); }
    public string EndOffset { get => Get(); set => Put(value); }
    public string FixedDuration { get => Get(); set => Put(value); }
    public string MaximumGap { get => Get(); set => Put(value); }
    public string BoundaryTolerance { get => Get(); set => Put(value); }
    public string LayerOffset { get => Get(); set => Put(value); }
    public string LayerMinimum { get => Get(); set => Put(value); }
    public string LayerMaximum { get => Get(); set => Put(value); }
    public IntentTypeMatch TypeMatch { get => model.Target.TypeMatch; set => Change(model with { Target = model.Target with { TypeMatch = value } }); }
    public IntentAnchor Anchor { get => model.Relation.Anchor; set => Change(model with { Relation = model.Relation with { Anchor = value } }); }
    public IntentAlignment Alignment { get => model.Relation.Alignment; set => Change(model with { Relation = model.Relation with { Alignment = value } }); }
    public IntentDuration Duration { get => model.Relation.Duration; set => Change(model with { Relation = model.Relation with { Duration = value } }); }
    public IntentNeighbor Neighbor { get => model.Relation.Neighbor; set => Change(model with { Relation = model.Relation with { Neighbor = value } }); }
    public IntentNeighborEdge NeighborEdge { get => model.Relation.NeighborEdge; set => Change(model with { Relation = model.Relation with { NeighborEdge = value } }); }
    public IntentFallback Fallback { get => model.Relation.Fallback; set => Change(model with { Relation = model.Relation with { Fallback = value } }); }
    public RelativeLayerDirection Direction { get => model.Relation.Layer.Direction; set => Change(model with { Relation = model.Relation with { Layer = model.Relation.Layer with { Direction = value } } }); }
    public bool ExpressionCandidates { get => model.ExpressionCandidates; set => Change(model with { ExpressionCandidates = value }); }
    public ObservableCollection<IntentTypeOption> TypeChoices { get; } = [];
    public ObservableCollection<IntentEntryDraft> Entries { get; } = [];
    public IntentEntryDraft? SelectedEntry { get => selectedEntry; set { selectedEntry = value; Notify(); } }
    public IntentPaletteDraft(IntentPalette source, IReadOnlyList<LibraryEntry> library, IReadOnlyDictionary<string, string> types)
    {
        model = IntentPaletteSettings.Copy(source);
        Name = source.Name; Intent = source.Intent; CharacterName = source.Target.CharacterName ?? "";
        MinimumCount = source.Target.MinimumCount.ToString(CultureInfo.InvariantCulture); MaximumCount = source.Target.MaximumCount.ToString(CultureInfo.InvariantCulture);
        StartOffset = source.Relation.StartOffset.ToString(CultureInfo.InvariantCulture); EndOffset = source.Relation.EndOffset.ToString(CultureInfo.InvariantCulture);
        FixedDuration = source.Relation.FixedDuration.ToString(CultureInfo.InvariantCulture); MaximumGap = source.Relation.MaximumNeighborGap?.ToString(CultureInfo.InvariantCulture) ?? "";
        BoundaryTolerance = source.Relation.BoundaryTolerance.ToString(CultureInfo.InvariantCulture);
        LayerOffset = source.Relation.Layer.Offset.ToString(CultureInfo.InvariantCulture); LayerMinimum = source.Relation.Layer.Minimum.ToString(CultureInfo.InvariantCulture);
        LayerMaximum = source.Relation.Layer.Maximum.ToString(CultureInfo.InvariantCulture);
        foreach (var key in types.Keys.Concat(source.Target.ItemTypeKeys).Distinct(StringComparer.Ordinal))
        {
            var option = new IntentTypeOption(key, types.GetValueOrDefault(key) ?? "利用できない種類: " + key, source.Target.ItemTypeKeys.Contains(key));
            option.Edited += (_, _) => Notify(nameof(TypeChoices)); TypeChoices.Add(option);
        }
        foreach (var entry in source.Entries) AddEntry(entry, library);
        Entries.CollectionChanged += (_, _) => Notify(nameof(Entries));
    }
    public void AddEntry(IntentEntry entry, IReadOnlyList<LibraryEntry> library)
    {
        var draft = new IntentEntryDraft(entry, library); draft.Edited += (_, _) => Notify(nameof(Entries)); Entries.Add(draft);
    }
    public IntentPalette Build()
    {
        var target = model.Target with { ItemTypeKeys = TypeChoices.Where(x => x.Selected).Select(x => x.Key).ToList(),
            MinimumCount = Number(MinimumCount, "選択数の最小"), MaximumCount = Number(MaximumCount, "選択数の最大"),
            CharacterName = string.IsNullOrWhiteSpace(CharacterName) ? null : CharacterName.Trim() };
        var relation = model.Relation with { StartOffset = Number(StartOffset, "開始のずらし"), EndOffset = Number(EndOffset, "終了のずらし"),
            FixedDuration = Number(FixedDuration, "固定の長さ"), MaximumNeighborGap = OptionalNumber(MaximumGap, "周囲参照の最大間隔"),
            BoundaryTolerance = Number(BoundaryTolerance, "境界の許容間隔"), Layer = model.Relation.Layer with {
                Offset = Number(LayerOffset, "対象からの段数"), Minimum = Number(LayerMinimum, "探索レイヤーの最小"), Maximum = Number(LayerMaximum, "探索レイヤーの最大") } };
        return model with { Name = Name.Trim(), Intent = Intent.Trim(), Target = target, Relation = relation, Entries = Entries.Select(x => x.Build()).ToList() };
    }
}

public sealed class IntentSettingsSession : IntentEditable
{
    private PlacerSettings working;
    private IntentPaletteDraft? selectedPalette;
    private string sourceSearch = "";
    public string BaselineFingerprint { get; }
    public bool HasChanges { get; private set; }
    public string ChangeNotice => HasChanges ? "未保存の変更があります" : "";
    public IReadOnlyDictionary<string, string> KnownTypes { get; }
    public ObservableCollection<IntentPaletteDraft> Palettes { get; } = [];
    public ObservableCollection<IntentSourceOption> Sources { get; } = [];
    public ICollectionView VisibleSources { get; }
    public IntentPaletteDraft? SelectedPalette { get => selectedPalette; set { selectedPalette = value; Notify(); } }
    public string SourceSearch { get => sourceSearch; set { sourceSearch = value; VisibleSources.Refresh(); Notify(); } }
    public IntentSettingsSession(PlacerSettings source, IEnumerable<Type> knownTypes)
    {
        BaselineFingerprint = JsonSerializer.Serialize(source); working = PlacerSettingsStore.Copy(source);
        KnownTypes = knownTypes.Append(typeof(VoiceItem)).Distinct().ToDictionary(IntentSelectionContext.TypeKey, TypeLabel, StringComparer.Ordinal);
        foreach (var palette in source.IntentPalettes) AddDraft(palette);
        selectedPalette = Palettes.FirstOrDefault();
        foreach (var template in ItemSettings.Default.Templates.Where(x => x.Items.Count > 0).OrderBy(x => x.Name, StringComparer.Ordinal)) Sources.Add(new(template));
        VisibleSources = CollectionViewSource.GetDefaultView(Sources);
        VisibleSources.Filter = x => x is IntentSourceOption option && (sourceSearch.Length == 0 || option.Name.Contains(sourceSearch, StringComparison.OrdinalIgnoreCase));
        Palettes.CollectionChanged += (_, _) => MarkDirty();
    }
    public static string TypeLabel(Type type) => type.Name switch { "VoiceItem" => "ボイス", "TachieFaceItem" => "表情", "TachieItem" => "立ち絵", "TextItem" => "テキスト", "VideoItem" => "動画", "AudioItem" => "音声", "ShapeItem" => "図形", "ImageItem" => "画像", _ => type.Name };
    private void MarkDirty() { HasChanges = true; Notify(nameof(HasChanges)); Notify(nameof(ChangeNotice)); }
    private IntentPaletteDraft AddDraft(IntentPalette palette)
    {
        var draft = new IntentPaletteDraft(palette, working.Library, KnownTypes);
        draft.Edited += (_, _) => MarkDirty(); Palettes.Add(draft); return draft;
    }
    public PlacerSettings Build()
    {
        var next = PlacerSettingsStore.Copy(working); next.IntentPalettes = Palettes.Select(x => x.Build()).ToList();
        next.LegacyWorkspace = false; IntentPaletteSettings.Upgrade(next); PlacerSettingsStore.Validate(next); return next;
    }
    public void Create(IReadOnlyList<IItem> selection)
    {
        var types = selection.Select(x => IntentSelectionContext.TypeKey(x.GetType())).Distinct(StringComparer.Ordinal).ToList();
        if (types.Count == 0) types.Add(IntentSelectionContext.TypeKey(typeof(VoiceItem)));
        var names = selection.Select(x => ItemCharacters.Get(x)?.Name).Distinct(StringComparer.Ordinal).ToArray();
        var context = new IntentTargetContext { ItemTypeKeys = types, TypeMatch = types.Count > 1 ? IntentTypeMatch.ExactMixedTypes : IntentTypeMatch.UniformType,
            MinimumCount = Math.Max(1, selection.Count), MaximumCount = Math.Max(1, selection.Count), CharacterName = names.Length == 1 ? names[0] : null };
        SelectedPalette = AddDraft(new(Guid.NewGuid(), UniqueName("新しいセット"), "演出", context, new(), [])); MarkDirty();
    }
    private string UniqueName(string stem)
    {
        if (!Palettes.Any(x => x.Name == stem)) return stem;
        for (var i = 2; ; i++) { var candidate = $"{stem} {i}"; if (!Palettes.Any(x => x.Name == candidate)) return candidate; }
    }
    public void Duplicate()
    {
        var source = SelectedPalette?.Build() ?? throw new InvalidOperationException("複製するセットを選んでください。");
        SelectedPalette = AddDraft(IntentPaletteSettings.Copy(source) with { Id = Guid.NewGuid(), Name = UniqueName(source.Name) }); MarkDirty();
    }
    public void RemoveSelected()
    {
        if (SelectedPalette == null) return;
        var index = Palettes.IndexOf(SelectedPalette); Palettes.Remove(SelectedPalette);
        SelectedPalette = Palettes.Count == 0 ? null : Palettes[Math.Min(index, Palettes.Count - 1)]; MarkDirty();
    }
    public void MovePalette(int delta)
    {
        if (SelectedPalette == null) return;
        var index = Palettes.IndexOf(SelectedPalette); var target = index + delta;
        if (target >= 0 && target < Palettes.Count) Palettes.Move(index, target);
    }
    public void MoveEntry(int delta)
    {
        var palette = SelectedPalette; var entry = palette?.SelectedEntry;
        if (palette == null || entry == null) return;
        var index = palette.Entries.IndexOf(entry); var target = index + delta;
        if (target >= 0 && target < palette.Entries.Count) palette.Entries.Move(index, target);
    }
    public int AddSelectedSources()
    {
        var palette = SelectedPalette ?? throw new InvalidOperationException("追加先のセットを選んでください。");
        var selected = Sources.Where(x => x.Selected).ToArray(); if (selected.Length == 0) return 0;
        var nextLibrary = working.Library.ToList(); var additions = new List<(IntentEntry Entry, TemplateLocator Locator)>();
        foreach (var option in selected)
        {
            if (!ItemSettings.Default.Templates.Contains(option.Source) || !option.Locator.Matches(option.Source))
                throw new InvalidOperationException("選択後に元テンプレートが削除・改名されました。追加せず停止しました。");
            var matches = nextLibrary.Where(x => x.Source == option.Locator).Take(2).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException($"「{option.Name}」への登録が複数あります。使用する登録を旧テンプレート管理で確認してください。");
            var entry = matches.FirstOrDefault() ?? TemplateResolver.Reference(option.Source, option.Name, null);
            var bundle = TemplateResolver.RequireBundle(entry);
            if (!ReferenceEquals(bundle.Template, option.Source)) throw new InvalidOperationException("元テンプレートを一意に特定できません。");
            var character = palette.CharacterName.Trim();
            if (character.Length != 0 && bundle.Items.Any(x => ItemCharacters.Get(x) is { } c && c.Name != character))
                throw new InvalidOperationException($"「{option.Name}」のキャラクターがセットの条件と違います。全件追加していません。");
            if (palette.ExpressionCandidates && !bundle.HasFace) throw new InvalidOperationException("表情一覧用セットには表情を含むテンプレートを追加してください。");
            if (matches.Length == 0) nextLibrary.Add(entry);
            if (!palette.Entries.Any(x => x.LibraryEntryId == entry.Id) && !additions.Any(x => x.Entry.LibraryEntryId == entry.Id))
                additions.Add((new IntentEntry(entry.Id) { UseTemplateDuration = bundle.Items.Count > 1 }, option.Locator));
        }
        // Publish the entire draft batch only after every selected source passes. No persistent write here.
        working.Library = nextLibrary;
        foreach (var addition in additions)
        {
            palette.AddEntry(addition.Entry, working.Library);
            if (palette.ExpressionCandidates && !working.ImportedExpressionSources.Contains(addition.Locator)) working.ImportedExpressionSources.Add(addition.Locator);
        }
        foreach (var option in selected) option.Selected = false;
        MarkDirty(); return additions.Count;
    }
    public int ImportNewExpressions()
    {
        var scan = IntentPaletteBootstrap.Scan(Build(), true); working = scan.Settings;
        var selected = SelectedPalette?.Id; Palettes.Clear(); foreach (var palette in working.IntentPalettes) AddDraft(palette);
        SelectedPalette = Palettes.FirstOrDefault(x => x.Id == selected) ?? Palettes.FirstOrDefault(); MarkDirty(); return scan.AddedEntries;
    }
}
