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
    protected void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected void NavigationChanged([CallerMemberName] string? name = null) { Raise(name); Edited?.Invoke(this, EventArgs.Empty); }
    protected void Notify([CallerMemberName] string? name = null)
    {
        Raise(name); Edited?.Invoke(this, EventArgs.Empty);
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
    private string displayAlias;
    private IntentTileColor color;
    private IntentTileShape shape;
    private readonly PlacementSourceRegistration? source;
    public Guid LibraryEntryId { get; }
    public Guid SourceId => LibraryEntryId;
    public string Name => IntentTileAppearance.Label(new(SourceId) { DisplayAlias = DisplayAlias }, source);
    public string DisplayAlias { get => displayAlias; set { if (displayAlias == value) return; displayAlias = value; Notify(); Raise(nameof(Name)); } }
    public IntentTileColor Color { get => color; set { if (color == value) return; color = value; Notify(); } }
    public IntentTileShape Shape { get => shape; set { if (shape == value) return; shape = value; Notify(); } }
    public string SourceDetail { get; }
    public string StartOffset { get => start; set { start = value; Notify(); } }
    public string EndOffset { get => end; set { end = value; Notify(); } }
    public string FixedDuration { get => length; set { length = value; Notify(); } }
    public bool UseTemplateDuration { get => intrinsic; set { intrinsic = value; Notify(); } }

    private IntentEntryDraft(IntentEntry entry, PlacementSourceRegistration? source)
    {
        LibraryEntryId = entry.LibraryEntryId;
        this.source = source;
        displayAlias = entry.DisplayAlias ?? "";
        color = entry.Color;
        shape = entry.Shape;
        SourceDetail = source?.Kind switch
        {
            PlacementSourceKind.Template =>
                source.Template!.Source.Name + "\n" + TemplateResolver.ResolveBundle(source.Template).Message,
            PlacementSourceKind.TachiePreset =>
                $"立ち絵プリセット · {source.TachiePreset!.CharacterName}\n{source.TachiePreset.DisplayName}",
            _ => "元の登録がありません。"
        };
        start = entry.StartOffsetDelta.ToString(CultureInfo.InvariantCulture);
        end = entry.EndOffsetDelta.ToString(CultureInfo.InvariantCulture);
        length = entry.FixedDurationOverride?.ToString(CultureInfo.InvariantCulture) ?? "";
        intrinsic = entry.UseTemplateDuration;
    }

    public IntentEntryDraft(IntentEntry entry, IReadOnlyList<LibraryEntry> library)
        : this(entry, library.SingleOrDefault(x => x.Id == entry.SourceId) is { } template
            ? new PlacementSourceRegistration(template.Id, PlacementSourceKind.Template, template, null)
            : null)
    {
    }

    internal IntentEntryDraft(IntentEntry entry, PlacerSettings settings)
        : this(entry, Resolve(settings, entry.SourceId))
    {
    }

    private static PlacementSourceRegistration? Resolve(PlacerSettings settings, Guid sourceId)
    {
        try { return PlacementSourceRegistry.Resolve(settings, sourceId); }
        catch (InvalidOperationException) { return null; }
    }

    public IntentEntry Build() => new(LibraryEntryId)
    {
        DisplayAlias = string.IsNullOrWhiteSpace(DisplayAlias) ? null : DisplayAlias.Trim(),
        Color = Color,
        Shape = Shape,
        StartOffsetDelta = Number(StartOffset, "演出の開始差分"),
        EndOffsetDelta = Number(EndOffset, "演出の終了差分"),
        FixedDurationOverride = OptionalNumber(FixedDuration, "演出の固定長"),
        UseTemplateDuration = UseTemplateDuration
    };
}

public sealed partial class IntentPaletteDraft : IntentEditable
{
    private IntentPalette model;
    private readonly Dictionary<string, string> text = new(StringComparer.Ordinal);
    private IntentEntryDraft? selectedEntry;
    private bool characterRestricted;
    public Guid Id => model.Id;
    private string Get([CallerMemberName] string key = "") => text.GetValueOrDefault(key, "");
    private void Put(string value, [CallerMemberName] string key = "")
    {
        text[key] = value; Notify(key);
        if (key == nameof(Name)) Raise(nameof(Label));
        RaiseUiState();
    }
    private void Change(IntentPalette value, [CallerMemberName] string name = "")
    {
        model = value; Notify(name); RaiseUiState();
    }
    private void RaiseUiState()
    {
        Raise(nameof(Summary)); Raise(nameof(ShowTypeMatch)); Raise(nameof(ShowFixedDuration)); Raise(nameof(ShowNeighborSettings));
        Raise(nameof(ShowNeighborEdge)); Raise(nameof(ShowNeighborFallback)); Raise(nameof(ShowMaximumGap)); Raise(nameof(ShowBoundaryTolerance));
        Raise(nameof(ShowAlignment)); Raise(nameof(ShowCharacterName)); Raise(nameof(CharacterRestrictionLabel));
        Raise(nameof(SentenceAnchors)); Raise(nameof(SentenceNeighbors)); Raise(nameof(SentenceAnchorJoin));
    }
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
    public IntentAnchor Anchor
    {
        get => model.Relation.Anchor;
        set
        {
            var relation = model.Relation with { Anchor = value };
            if ((value is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd) && relation.Neighbor == IntentNeighbor.None)
                relation = relation with { Neighbor = DefaultNeighbor() };
            Change(model with { Relation = relation });
            Raise(nameof(Neighbor));
        }
    }
    public IntentAlignment Alignment { get => model.Relation.Alignment; set => Change(model with { Relation = model.Relation with { Alignment = value } }); }
    public IntentDuration Duration
    {
        get => model.Relation.Duration;
        set
        {
            var relation = model.Relation with { Duration = value };
            if (value == IntentDuration.UntilRelated)
            {
                if (relation.Neighbor == IntentNeighbor.None) relation = relation with { Neighbor = DefaultNeighbor() };
                if (relation.Alignment != IntentAlignment.StartAtAnchor) relation = relation with { Alignment = IntentAlignment.StartAtAnchor };
            }
            Change(model with { Relation = relation });
            Raise(nameof(Neighbor)); Raise(nameof(Alignment));
        }
    }
    public IntentNeighbor Neighbor { get => model.Relation.Neighbor; set => Change(model with { Relation = model.Relation with { Neighbor = value } }); }
    public IntentNeighborEdge NeighborEdge { get => model.Relation.NeighborEdge; set => Change(model with { Relation = model.Relation with { NeighborEdge = value } }); }
    public IntentFallback Fallback { get => model.Relation.Fallback; set => Change(model with { Relation = model.Relation with { Fallback = value } }); }
    public RelativeLayerDirection Direction { get => model.Relation.Layer.Direction; set => Change(model with { Relation = model.Relation with { Layer = model.Relation.Layer with { Direction = value } } }); }
    public bool ExpressionCandidates { get => model.ExpressionCandidates; set => Change(model with { ExpressionCandidates = value }); }
    public bool CharacterRestricted
    {
        get => characterRestricted;
        set
        {
            if (characterRestricted == value) return;
            characterRestricted = value;
            Notify(); RaiseUiState();
        }
    }
    public string CharacterRestrictionLabel => CharacterRestricted && !string.IsNullOrWhiteSpace(CharacterName)
        ? $"キャラクターを限定: {CharacterName.Trim()}" : "キャラクターを限定";
    public bool ShowCharacterName => CharacterRestricted;
    public bool ShowTypeMatch => TypeChoices.Count(x => x.Selected) > 1;
    public bool ShowFixedDuration => Duration == IntentDuration.Fixed || (ShowNeighborFallback && Fallback == IntentFallback.FixedDuration);
    public bool ShowNeighborSettings => Duration == IntentDuration.UntilRelated || (Anchor is IntentAnchor.RelatedStart or IntentAnchor.RelatedEnd);
    public bool ShowNeighborEdge => Duration == IntentDuration.UntilRelated && Neighbor != IntentNeighbor.None;
    public bool ShowNeighborFallback => ShowNeighborSettings && Neighbor != IntentNeighbor.None;
    public bool ShowMaximumGap => ShowNeighborFallback;
    public bool ShowBoundaryTolerance => Anchor == IntentAnchor.PairBoundary;
    public bool ShowAlignment => Duration != IntentDuration.UntilRelated;
    public ObservableCollection<IntentTypeOption> TypeChoices { get; } = [];
    public ObservableCollection<IntentEntryDraft> Entries { get; } = [];
    public IntentEntryDraft? SelectedEntry { get => selectedEntry; set { if (selectedEntry == value) return; selectedEntry = value; Raise(); } }
    public string Summary => BuildSummary();

    public IntentPaletteDraft(IntentPalette source, IReadOnlyList<LibraryEntry> library, IReadOnlyDictionary<string, string> types)
        : this(source, LegacySettings(library), types)
    {
    }

    internal IntentPaletteDraft(IntentPalette source, PlacerSettings settings, IReadOnlyDictionary<string, string> types)
    {

        model = IntentPaletteSettings.Copy(source);
        text[nameof(Name)] = source.Name; text[nameof(Intent)] = source.Intent; text[nameof(CharacterName)] = source.Target.CharacterName ?? "";
        text[nameof(MinimumCount)] = source.Target.MinimumCount.ToString(CultureInfo.InvariantCulture);
        text[nameof(MaximumCount)] = source.Target.MaximumCount.ToString(CultureInfo.InvariantCulture);
        text[nameof(StartOffset)] = source.Relation.StartOffset.ToString(CultureInfo.InvariantCulture);
        text[nameof(EndOffset)] = source.Relation.EndOffset.ToString(CultureInfo.InvariantCulture);
        text[nameof(FixedDuration)] = source.Relation.FixedDuration.ToString(CultureInfo.InvariantCulture);
        text[nameof(MaximumGap)] = source.Relation.MaximumNeighborGap?.ToString(CultureInfo.InvariantCulture) ?? "";
        text[nameof(BoundaryTolerance)] = source.Relation.BoundaryTolerance.ToString(CultureInfo.InvariantCulture);
        text[nameof(LayerOffset)] = source.Relation.Layer.Offset.ToString(CultureInfo.InvariantCulture);
        text[nameof(LayerMinimum)] = source.Relation.Layer.Minimum.ToString(CultureInfo.InvariantCulture);
        text[nameof(LayerMaximum)] = source.Relation.Layer.Maximum.ToString(CultureInfo.InvariantCulture);
        characterRestricted = !string.IsNullOrWhiteSpace(source.Target.CharacterName);
        foreach (var key in types.Keys.Concat(source.Target.ItemTypeKeys).Distinct(StringComparer.Ordinal))
        {
            var option = new IntentTypeOption(key, types.GetValueOrDefault(key) ?? "利用できない種類", source.Target.ItemTypeKeys.Contains(key));
            option.Edited += (_, _) => { Notify(nameof(TypeChoices)); RaiseUiState(); }; TypeChoices.Add(option);
        }
        foreach (var entry in source.Entries) AddEntry(entry, settings);
        Entries.CollectionChanged += (_, _) => Notify(nameof(Entries));
    }

    private static PlacerSettings LegacySettings(IReadOnlyList<LibraryEntry> library) =>
        new() { Library = library.ToList() };

    private IntentNeighbor DefaultNeighbor() => CharacterRestricted ? IntentNeighbor.NextSameTypeAndCharacter : IntentNeighbor.NextSameType;
    private string BuildSummary()
    {
        var selectedTypes = TypeChoices.Where(x => x.Selected).Select(x => x.Name).ToArray();
        var target = selectedTypes.Length switch { 0 => "対象アイテム", 1 => selectedTypes[0], _ => string.Join("・", selectedTypes) };
        if (CharacterRestricted && !string.IsNullOrWhiteSpace(CharacterName)) target = $"{CharacterName.Trim()}の{target}";
        var anchor = Anchor switch
        {
            IntentAnchor.SelectedStart => "選択アイテムの開始",
            IntentAnchor.SelectedEnd => "選択アイテムの終了",
            IntentAnchor.SelectedCenter => "選択アイテムの中央",
            IntentAnchor.SelectionRangeStart => "選択範囲の開始",
            IntentAnchor.SelectionRangeEnd => "選択範囲の終了",
            IntentAnchor.PairBoundary => "選択した2アイテムの境界",
            IntentAnchor.RelatedStart => NeighborPhrase() + "の開始",
            IntentAnchor.RelatedEnd => NeighborPhrase() + "の終了",
            _ => "選択位置"
        };
        var timing = Duration switch
        {
            IntentDuration.Template => Alignment == IntentAlignment.EndAtAnchor ? $"{anchor}で終わるようにテンプレートの長さで" : $"{anchor}からテンプレートの長さで",
            IntentDuration.TargetSpan => Alignment == IntentAlignment.EndAtAnchor ? $"{anchor}で終わるように選択対象と同じ長さで" : $"{anchor}から選択対象と同じ長さで",
            IntentDuration.Fixed => Alignment == IntentAlignment.EndAtAnchor ? $"{anchor}で終わるように{ReadableFixedDuration()}で" : $"{anchor}から{ReadableFixedDuration()}で",
            IntentDuration.UntilRelated => $"{anchor}から{NeighborPhrase()}の{(NeighborEdge == IntentNeighborEdge.Start ? "開始" : "終了")}まで",
            _ => anchor
        };
        var direction = Direction == RelativeLayerDirection.Up ? "上" : "下";
        var fallback = ShowNeighborFallback ? Fallback switch
        {
            IntentFallback.CurrentTargetEnd => " 見つからなければ現在の対象の終了までにします。",
            IntentFallback.TargetSpan => " 見つからなければ現在の対象と同じ範囲にします。",
            IntentFallback.FixedDuration => $" 見つからなければ{ReadableFixedDuration()}で配置します。",
            IntentFallback.DoNotPlace => " 見つからなければ配置しません。",
            _ => ""
        } : "";
        return $"{target}を選んだとき、{timing}、対象より{direction}の空いているレイヤーへ配置します。塞がっていればさらに{direction}へ探します。{fallback}".Trim();
    }
    private string NeighborPhrase() => Neighbor switch
    {
        IntentNeighbor.NextSameType => "次の同じ種類のアイテム",
        IntentNeighbor.PreviousSameType => "前の同じ種類のアイテム",
        IntentNeighbor.NextSameCharacter => "次の同じキャラのアイテム",
        IntentNeighbor.PreviousSameCharacter => "前の同じキャラのアイテム",
        IntentNeighbor.NextSameTypeAndCharacter => "次の同じ種類・同じキャラのアイテム",
        IntentNeighbor.PreviousSameTypeAndCharacter => "前の同じ種類・同じキャラのアイテム",
        _ => "周囲のアイテム"
    };
    private string ReadableFixedDuration() => int.TryParse(FixedDuration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0 ? $"{n}フレーム" : "指定した長さ";
    public void AddEntry(IntentEntry entry, IReadOnlyList<LibraryEntry> library)
    {
        var draft = new IntentEntryDraft(entry, library); draft.Edited += (_, _) => Notify(nameof(Entries)); Entries.Add(draft);
    }
    public IntentPalette Build()
    {
        var selectedTypes = TypeChoices.Where(x => x.Selected).Select(x => x.Key).ToList();
        var target = model.Target with { ItemTypeKeys = selectedTypes, TypeMatch = selectedTypes.Count > 1 ? model.Target.TypeMatch : IntentTypeMatch.UniformType,
            MinimumCount = Number(MinimumCount, "選択数の最小"), MaximumCount = Number(MaximumCount, "選択数の最大"),
            CharacterName = CharacterRestricted && !string.IsNullOrWhiteSpace(CharacterName) ? CharacterName.Trim() : null };
        var relation = model.Relation with { StartOffset = Number(StartOffset, "開始のずらし"), EndOffset = Number(EndOffset, "終了のずらし"),
            FixedDuration = ShowFixedDuration ? Number(FixedDuration, "固定の長さ") : model.Relation.FixedDuration,
            MaximumNeighborGap = ShowMaximumGap ? OptionalNumber(MaximumGap, "周囲参照の最大間隔") : model.Relation.MaximumNeighborGap,
            BoundaryTolerance = ShowBoundaryTolerance ? Number(BoundaryTolerance, "境界の許容間隔") : model.Relation.BoundaryTolerance,
            Layer = model.Relation.Layer with { Offset = Number(LayerOffset, "対象からの段数"), Minimum = Number(LayerMinimum, "探索レイヤーの最小"), Maximum = Number(LayerMaximum, "探索レイヤーの最大") } };
        return model with { Name = Name.Trim(), Intent = Intent, Target = target, Relation = relation, Entries = Entries.Select(x => x.Build()).ToList() };
    }
}

public sealed partial class IntentSettingsSession : IntentEditable
{
    private PlacerSettings working;
    private IntentPaletteDraft? selectedPalette;
    private string sourceSearch = "";
    public PalettePresentationDraft Presentation { get; }
    public string BaselineFingerprint { get; }
    public bool HasChanges { get; private set; }
    public string ChangeNotice => HasChanges ? "未保存の変更があります" : "";
    public IReadOnlyDictionary<string, string> KnownTypes { get; }
    public ObservableCollection<IntentPaletteDraft> Palettes { get; } = [];
    public ObservableCollection<IntentSourceOption> Sources { get; } = [];
    public ICollectionView VisibleSources { get; }
    public IntentPaletteDraft? SelectedPalette { get => selectedPalette; set { if (refreshingNavigation || selectedPalette == value) return; selectedPalette = value; NavigationChanged(); } }
    public string SourceSearch { get => sourceSearch; set { sourceSearch = value; VisibleSources.Refresh(); Raise(); } }
    public IntentSettingsSession(PlacerSettings source, IEnumerable<Type> knownTypes, IReadOnlyList<IItem>? selection = null)
    {
        BaselineFingerprint = JsonSerializer.Serialize(source); working = PlacerSettingsStore.Copy(source);
        Presentation = new(source.Presentation); Presentation.Edited += (_, _) => MarkDirty();
        KnownTypes = knownTypes.Concat(CommonSettingsTypes).Distinct().ToDictionary(IntentSelectionContext.TypeKey, TypeLabel, StringComparer.Ordinal);
        foreach (var palette in source.IntentPalettes) AddDraft(palette);
        selectedPalette = Palettes.FirstOrDefault();
        foreach (var template in ItemSettings.Default.Templates.Where(x => x.Items.Count > 0).OrderBy(x => x.Name, StringComparer.Ordinal)) Sources.Add(new(template));
        VisibleSources = CollectionViewSource.GetDefaultView(Sources);
        VisibleSources.Filter = x => x is IntentSourceOption option && (sourceSearch.Length == 0 || option.Name.Contains(sourceSearch, StringComparison.OrdinalIgnoreCase));
        InitializeGenericSets();
        InitializeNavigation(selection ?? []);
        Palettes.CollectionChanged += (_, _) => { MarkDirty(); RefreshNavigation(); };
    }
    public static string TypeLabel(Type type) => ItemDisplayNames.For(type);
    private void MarkDirty() { HasChanges = true; Notify(nameof(HasChanges)); Raise(nameof(ChangeNotice)); }
    private IntentPaletteDraft AddDraft(IntentPalette palette)
    {
        var draft = new IntentPaletteDraft(palette, working, KnownTypes);
        draft.Edited += (_, _) => MarkDirty();
        draft.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IntentPaletteDraft.SelectedEntry)) NavigationChanged(nameof(SelectedPalette));
        };
        Palettes.Add(draft); return draft;
    }
    internal bool HasExpressionDependencyChanges(Guid paletteId, Guid sourceId, PlacerSettings saved, out string setName)
    {
        var savedPalette = saved.IntentPalettes.SingleOrDefault(x => x.Id == paletteId);
        var draftPalette = Palettes.SingleOrDefault(x => x.Id == paletteId);
        setName = draftPalette?.Name ?? savedPalette?.Name ?? "対象セット";
        if (!HasChanges) return false;
        if (savedPalette == null || draftPalette == null) return true;
        try
        {
            if (JsonSerializer.Serialize(savedPalette) != JsonSerializer.Serialize(draftPalette.Build())) return true;
        }
        catch (InvalidOperationException) { return true; }

        var savedLibrary = saved.Library.SingleOrDefault(x => x.Id == sourceId);
        var draftLibrary = working.Library.SingleOrDefault(x => x.Id == sourceId);
        if (savedLibrary != null || draftLibrary != null)
            return savedLibrary == null || draftLibrary == null ||
                JsonSerializer.Serialize(savedLibrary) != JsonSerializer.Serialize(draftLibrary);

        var savedPreset = saved.TachiePresetSources.SingleOrDefault(x => x.Id == sourceId);
        var draftPreset = working.TachiePresetSources.SingleOrDefault(x => x.Id == sourceId);
        return savedPreset == null || draftPreset == null ||
            JsonSerializer.Serialize(savedPreset) != JsonSerializer.Serialize(draftPreset);
    }

    public PlacerSettings Build()
    {
        var next = PlacerSettingsStore.Copy(working); next.IntentPalettes = Palettes.Select(x => x.Build()).ToList();
        ApplyGenericSets(next); next.Presentation = Presentation.Build();
        next.LegacyWorkspace = false; IntentPaletteSettings.Upgrade(next); PlacerSettingsStore.Validate(next); return next;
    }
    // Retained compatibility entry points now obey the Item-owned normal Set model.
    public void Create(IReadOnlyList<IItem> selection) => CreateSingleOwner(selection);
    public void Duplicate() => DuplicateOwned();
    public void RemoveSelected()
    {
        if (IsGenericContext) { RemoveGenericSet(); return; }
        if (SelectedPalette == null) return;
        Palettes.Remove(SelectedPalette);
        RefreshPaletteFilter();
        MarkDirty();
    }
    public void MovePalette(int delta) => MoveOwned(delta);
    public void MoveEntry(int delta)
    {
        if (IsGenericContext) { SelectedGenericSet?.MoveEntry(delta); return; }
        var palette = SelectedPalette; var entry = palette?.SelectedEntry;
        if (palette == null || entry == null) return;
        var index = palette.Entries.IndexOf(entry); var target = index + delta;
        if (target >= 0 && target < palette.Entries.Count) palette.Entries.Move(index, target);
    }
    public int AddSelectedSources()
    {
        if (IsGenericContext) return AddGenericSources();
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
            var character = palette.CharacterRestricted ? palette.CharacterName.Trim() : "";
            if (character.Length != 0 && bundle.Items.Any(x => ItemCharacters.Get(x) is { } c && c.Name != character))
                throw new InvalidOperationException($"「{option.Name}」のキャラクターがセットの条件と違います。全件追加していません。");
            if (palette.ExpressionCandidates && !bundle.HasFace) throw new InvalidOperationException("「表情をまとめて」用のセットには表情を含むテンプレートを追加してください。");
            if (matches.Length == 0) nextLibrary.Add(entry);
            if (!palette.Entries.Any(x => x.LibraryEntryId == entry.Id) && !additions.Any(x => x.Entry.LibraryEntryId == entry.Id))
                additions.Add((new IntentEntry(entry.Id) { UseTemplateDuration = bundle.Items.Count > 1 }, option.Locator));
        }
        // Publish the entire draft batch only after every selected source passes. No persistent write here.
        working.Library = nextLibrary;
        foreach (var addition in additions)
        {
            palette.AddEntry(addition.Entry, working);
            if (palette.ExpressionCandidates && !working.ImportedExpressionSources.Contains(addition.Locator)) working.ImportedExpressionSources.Add(addition.Locator);
        }
        foreach (var option in selected) option.Selected = false;
        MarkDirty(); return additions.Count;
    }
    public int ImportNewExpressions()
    {
        var scan = IntentPaletteBootstrap.Scan(Build(), true); working = scan.Settings;
        var selected = SelectedPalette?.Id; Palettes.Clear(); foreach (var palette in working.IntentPalettes) AddDraft(palette);
        SelectedPalette = Palettes.FirstOrDefault(x => x.Id == selected) ?? Palettes.FirstOrDefault();
        RefreshNavigation(); MarkDirty(); return scan.AddedEntries;
    }
}
