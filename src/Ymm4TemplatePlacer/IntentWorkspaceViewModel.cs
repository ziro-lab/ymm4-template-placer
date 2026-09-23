using System.Collections.ObjectModel;
using System.ComponentModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private Timeline? intentTimeline;
    private bool intentInitialized, refreshingIntent, intentExecuting;
    private string intentNotice = "", intentContextKey = "", intentContextTitle = "", intentContextDetail = "", intentEmptyActionLabel = "新しく設定する";
    private IntentSetChoice? selectedIntentSet;
    private readonly Dictionary<string, Guid> lastIntentSets = new(StringComparer.Ordinal);
    public ObservableCollection<IntentSetChoice> IntentSets { get; } = [];
    public ObservableCollection<IntentTileChoice> IntentTiles { get; } = [];
    public bool HasIntentSets => IntentSets.Count > 1;
    public bool ShowSingleSetName => IntentSets.Count == 1;
    public bool UseSegmentedIntentSets => IntentSets.Count is > 1 and <= 4;
    public bool UseIntentSetPicker => IntentSets.Count > 4;
    public bool ShowIntentEmptyAction => timeline != null && (PlacementContext == PlacementContext.Generic || timeline.SelectedItems.Count > 0) && IntentTiles.Count == 0;
    public string IntentNotice { get => intentNotice; private set => Set(ref intentNotice, value); }
    public string IntentContextTitle { get => intentContextTitle; private set => Set(ref intentContextTitle, value); }
    public string IntentContextDetail { get => intentContextDetail; private set => Set(ref intentContextDetail, value); }
    public string IntentEmptyActionLabel { get => intentEmptyActionLabel; private set => Set(ref intentEmptyActionLabel, value); }
    public IntentSetChoice? SelectedIntentSet
    {
        get => selectedIntentSet;
        set
        {
            if (refreshingIntent || value == null || !IntentSets.Any(x => ReferenceEquals(x, value)) || value == selectedIntentSet) return;
            selectedIntentSet = value; OnPropertyChanged(); RememberIntentSet(); RefreshIntentTiles();
        }
    }
    public ActionCommand ExecuteIntentTileCommand { get; private set; } = null!;
    public ActionCommand OpenIntentSettingsCommand { get; private set; } = null!;
    public ActionCommand ImportNewExpressionsCommand { get; private set; } = null!;
    public event EventHandler? IntentSettingsRequested;

    // The View calls this on attachment and scene changes; it is idempotent and also usable by native tests.
    public void ActivateIntentWorkspace()
    {
        if (!intentInitialized)
        {
            intentInitialized = true;
            InitializeIntentTileOrdering(); InitializeIntentTileEditing(); InitializeGenericLayerTarget();
            ExecuteIntentTileCommand = new ActionCommand(x => !intentExecuting && tileEditState == IntentTileEditState.Idle && settingsAvailable && undo != null &&
                x is IntentTileChoice tile && tile.Available && (!tile.IsGeneric || GenericLayerReadyForExecution) && IntentTiles.Any(x => ReferenceEquals(x, tile)),
                x => ExecuteIntentTileFromCommand((IntentTileChoice)x!));
            OpenIntentSettingsCommand = new ActionCommand(_ => true, _ => IntentSettingsRequested?.Invoke(this, EventArgs.Empty));
            ImportNewExpressionsCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(ImportNewIntentExpressions));
        }
        if (!ReferenceEquals(intentTimeline, timeline))
        {
            DeactivateIntentWorkspace(); intentTimeline = timeline; InitializePlacementContext();
            if (intentTimeline != null) intentTimeline.PropertyChanged += IntentTimelineChanged;
        }
        RefreshIntentWorkspace();
    }
    public void DeactivateIntentWorkspace()
    {
        if (intentTimeline != null) intentTimeline.PropertyChanged -= IntentTimelineChanged;
        intentTimeline = null; EndTimelinePointer();
        IntentWorkspaceDeactivated?.Invoke(this, EventArgs.Empty);
    }
    private void IntentTimelineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, intentTimeline)) return;
        if (e.PropertyName is nameof(Timeline.SelectedItems) or nameof(Timeline.SelectedItem)) ObserveContextSelection();
        if (e.PropertyName is nameof(Timeline.SelectedItems) or nameof(Timeline.SelectedItem) or nameof(Timeline.Items))
        {
            RefreshIntentWorkspace();
            IntentSettings?.UpdateSelectionContext(timeline?.SelectedItems.ToArray() ?? []);
        }
    }
    public void RefreshIntentWorkspace()
    {
        if (!intentInitialized || refreshingIntent) return;
        refreshingIntent = true;
        try
        {
            if (settingsAvailable && !settings.ExpressionBootstrapComplete && timeline != null)
            {
                var initial = IntentPaletteBootstrap.Scan(settings);
                settings = initial.Settings; // In-memory only, until an explicit successful settings operation.
                if (initial.Diagnostics.Count > 0) IntentNotice = string.Join("\n", initial.Diagnostics);
            }
            var preferred = selectedIntentSet?.Id;
            IntentSets.Clear(); IntentTiles.Clear(); selectedIntentSet = null;
            UpdateIntentContext(null);
            if (!settingsAvailable) { IntentNotice = LibraryNotice; return; }
            if (timeline != null && PlacementContext == PlacementContext.Generic)
            {
                if (intentContextKey != "generic") preferred = lastIntentSets.GetValueOrDefault("generic");
                intentContextKey = "generic";
                IntentContextTitle = "汎用 · 時間位置";
                IntentContextDetail = "再生位置に、テンプレートの長さで配置";
                var styles = settings.Palettes.Where(x => x.Kind == PaletteKind.Style).ToArray();
                foreach (var palette in styles)
                {
                    var peers = styles.Where(x => x.Name == palette.Name).ToArray();
                    var label = peers.Length > 1 ? $"{palette.Name} ({Array.IndexOf(peers, palette) + 1})" : palette.Name;
                    IntentSets.Add(new(palette, label));
                }
                ChooseIntentSet(preferred);
                if (IntentSets.Count == 0) IntentNotice = "汎用セットがまだありません。設定から追加してください。";
                return;
            }
            if (timeline == null || timeline.SelectedItems.Count == 0)
            {
                IntentNotice = "タイムラインで編集したいアイテムを選択してください。";
                IntentEmptyActionLabel = "新しく設定する";
                return;
            }
            var context = IntentSelectionContext.Capture(timeline); UpdateIntentContext(context);
            var nextKey = string.Join("|", context.Selected.Select(x => x.TypeKey + ":" + x.CharacterName)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)) + ":" + context.Selected.Count;
            if (nextKey != intentContextKey) preferred = lastIntentSets.GetValueOrDefault(nextKey);
            intentContextKey = nextKey;
            var applicable = settings.IntentPalettes.Where(x => x.Target.Matches(context)).ToArray();
            var chosen = applicable.FirstOrDefault(x => x.Id == preferred) ?? applicable.FirstOrDefault();
            IntentNotice = chosen == null ? NoIntentMessage(context) : "";
            IntentEmptyActionLabel = chosen == null ? "新しく設定する" : "演出を追加する";
            FillIntentSets(applicable, chosen?.Id);
        }
        catch (Exception ex)
        {
            IntentSets.Clear(); IntentTiles.Clear(); selectedIntentSet = null;
            IntentNotice = ex.GetBaseException().Message;
        }
        finally
        {
            refreshingIntent = false;
            OnPropertyChanged(nameof(SelectedIntentSet));
            RaiseIntentSurfaceState(); ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
        }
    }
    private void FillIntentSets(IReadOnlyList<IntentPalette> applicable, Guid? preferred)
    {
        IntentSets.Clear();
        var sets = applicable.ToArray(); // Set names carry the purpose; Intent remains serialized compatibility metadata.
        foreach (var palette in sets)
        {
            var peers = sets.Where(x => x.Name == palette.Name).ToArray();
            var label = peers.Length > 1 ? $"{palette.Name} ({Array.IndexOf(peers, palette) + 1})" : palette.Name;
            IntentSets.Add(new(palette, label));
        }
        ChooseIntentSet(preferred);
    }
    private void ChooseIntentSet(Guid? preferred)
    {
        selectedIntentSet = IntentSets.FirstOrDefault(x => x.Id == preferred) ?? IntentSets.FirstOrDefault();
        RememberIntentSet(); RefreshIntentTiles(); RaiseIntentSurfaceState();
    }
    private void RememberIntentSet()
    {
        if (selectedIntentSet == null) return;
        if (lastIntentSets.Count >= 256 && !lastIntentSets.ContainsKey(intentContextKey)) lastIntentSets.Remove(lastIntentSets.Keys.First());
        lastIntentSets[intentContextKey] = selectedIntentSet.Id;
    }
    private void RefreshIntentTiles()
    {
        IntentTiles.Clear();
        if (selectedIntentSet == null) { RaiseIntentSurfaceState(); ExecuteIntentTileCommand?.RaiseCanExecuteChanged(); return; }
        if (selectedIntentSet.Targeted is { } palette)
        {
            PlacementSourceRegistration? Resolve(Guid id)
            {
                try { return PlacementSourceRegistry.Resolve(settings, id); }
                catch (InvalidOperationException) { return null; }
            }

            var sources = palette.Entries.Select(x => Resolve(x.SourceId)).ToArray();
            var labels = IntentTileAppearance.Distinguish(
                palette.Entries.Select((entry, index) => IntentTileAppearance.Label(entry, sources[index])).ToArray());
            for (var index = 0; index < palette.Entries.Count; index++)
            {
                var tile = palette.Entries[index];
                var source = sources[index];
                if (source == null)
                {
                    IntentTiles.Add(new(palette.Id, tile, labels[index], "設定で配置Sourceの登録を確認してください。", false));
                    continue;
                }

                if (source.Kind == PlacementSourceKind.Template)
                {
                    var resolution = TemplateResolver.ResolveBundle(source.Template!);
                    IntentTiles.Add(new(palette.Id, tile, labels[index],
                        resolution.Bundle == null ? resolution.Message : source.Template!.Source.Name,
                        resolution.Bundle != null));
                    continue;
                }

                var preset = source.TachiePreset!;
                var characterBound = palette.Target.CharacterName != null &&
                    string.Equals(palette.Target.CharacterName, preset.CharacterName, StringComparison.Ordinal);
                IntentTiles.Add(new(palette.Id, tile, labels[index],
                    characterBound
                        ? $"立ち絵プリセット · {preset.CharacterName}"
                        : "立ち絵プリセットはキャラクターを指定したSetで使用してください。",
                    characterBound));
            }
        }
        else if (selectedIntentSet?.Generic is { } style)
        {
            var entries = style.LibraryEntryIds.Select(id => (Id: id, Source: settings.Library.SingleOrDefault(x => x.Id == id))).ToArray();
            var labels = IntentTileAppearance.Distinguish(entries.Select(x => style.AppearanceFor(x.Id).DisplayAlias is { } alias && !string.IsNullOrWhiteSpace(alias) ? alias.Trim() : IntentTileAppearance.ShortName(x.Source?.DisplayName ?? "参照切れ")).ToArray());
            for (var index = 0; index < entries.Length; index++)
            {
                var item = entries[index]; var source = item.Source;
                if (source == null) { IntentTiles.Add(new(style.Id, item.Id, labels[index], "元テンプレートの登録を確認してください。", false, style.AppearanceFor(item.Id))); continue; }
                var resolution = TemplateResolver.Resolve(source);
                IntentTiles.Add(new(style.Id, source.Id, labels[index], resolution.Item == null ? resolution.Message : source.Source.Name, resolution.Item != null, style.AppearanceFor(source.Id)));
            }
        }
        IntentNotice = IntentTiles.Count == 0 ? $"「{selectedIntentSet?.Label ?? "このセット"}」には演出がありません。" : "";
        IntentEmptyActionLabel = IntentTiles.Count == 0 && selectedIntentSet != null ? "演出を追加する" : "新しく設定する";
        RaiseIntentSurfaceState(); ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
    }
    private void RaiseIntentSurfaceState()
    {
        UpdateGenericLayerTarget();
        OnPropertyChanged(nameof(PaletteLayout)); OnPropertyChanged(nameof(PaletteFixedColumns));
        OnPropertyChanged(nameof(HasIntentSets)); OnPropertyChanged(nameof(ShowSingleSetName)); OnPropertyChanged(nameof(UseSegmentedIntentSets)); OnPropertyChanged(nameof(UseIntentSetPicker));
        OnPropertyChanged(nameof(ShowIntentEmptyAction)); UpdateIntentTileEditingCommands();
    }
    private void UpdateIntentContext(IntentSelectionContext? context)
    {
        if (context == null || context.Selected.Count == 0)
        {
            IntentContextTitle = ""; IntentContextDetail = ""; return;
        }
        if (context.Selected.Count == 1)
        {
            var selected = context.Selected[0]; var item = selected.Item;
            var type = IntentSettingsSession.TypeLabel(item.GetType());
            IntentContextTitle = string.IsNullOrWhiteSpace(selected.CharacterName) ? type : $"{selected.CharacterName} {type}";
            IntentContextDetail = item is VoiceItem voice && !string.IsNullOrWhiteSpace(voice.Serif) ? voice.Serif : $"開始 {selected.Frame} / 長さ {selected.Length}";
            return;
        }
        var types = context.Selected.Select(x => IntentSettingsSession.TypeLabel(x.Item.GetType())).Distinct(StringComparer.Ordinal).ToArray();
        var character = context.UniformCharacter;
        IntentContextTitle = string.IsNullOrWhiteSpace(character) ? $"{context.Selected.Count}アイテムを選択" : $"{character} / {context.Selected.Count}アイテムを選択";
        IntentContextDetail = types.Length == 1 ? types[0] : string.Join("・", types);
    }
    private static string NoIntentMessage(IntentSelectionContext context)
    {
        if (context.Selected.Count == 1 && context.Selected[0].Item is VoiceItem && !string.IsNullOrWhiteSpace(context.Selected[0].CharacterName))
            return $"「{context.Selected[0].CharacterName}」のこのボイスで使える操作はまだありません。";
        return "この種類・選択で使える操作はまだありません。";
    }
    private async void ExecuteIntentTileFromCommand(IntentTileChoice tile)
    {
        if (tile.IsGeneric)
        {
            Guard(() => ExecuteIntentTile(tile));
            return;
        }
        try { await ExecuteIntentTileAsync(tile); }
        catch (Exception ex)
        {
            HasError = true;
            Status = "操作を完了できませんでした: " + ex.GetBaseException().Message;
        }
    }

    internal async Task<int> ExecuteIntentTileAsync(IntentTileChoice tile, CancellationToken token = default)
    {
        if (tile.IsGeneric)
            throw new InvalidOperationException("非同期配置は対象アイテム用Setにだけ使用します。");
        if (intentExecuting || tileEditState != IntentTileEditState.Idle ||
            !IntentTiles.Any(x => ReferenceEquals(x, tile)) || selectedIntentSet?.Id != tile.PaletteId)
            throw new InvalidOperationException("表示しているセットが変わりました。演出を選び直してください。");
        if (undo == null || !settingsAvailable)
            throw new InvalidOperationException("現在は配置できません。Toolと設定を確認してください。");
        if (PlacementContext != PlacementContext.Selection || selectedIntentSet?.Targeted == null)
            throw new InvalidOperationException("対象アイテム用のセットを選び直してください。");

        intentExecuting = true;
        ExecuteIntentTileCommand.RaiseCanExecuteChanged();
        try
        {
            var current = RequireTimeline();
            var manager = undo;
            var settingsSnapshot = settings;
            var palette = settingsSnapshot.IntentPalettes.Single(x => x.Id == tile.PaletteId);
            var entry = palette.Entries.Single(x => x.SourceId == tile.LibraryEntryId);
            var plan = await IntentExecutionPlan.CreateAsync(
                current, palette, entry, settingsSnapshot, PresetTargetResolver, token);

            if (!ReferenceEquals(settings, settingsSnapshot))
                throw new InvalidOperationException("配置の準備中に設定が変更されました。配置していません。もう一度選んでください。");
            if (!ReferenceEquals(current, timeline) || selectedIntentSet?.Id != tile.PaletteId)
                throw new InvalidOperationException("配置の準備中に対象シーンまたはSetが変わりました。配置していません。");

            var count = plan.Commit(current, manager, settings);
            HasError = false;
            Status = plan.Skipped
                ? "必要な周囲アイテムが見つからないため、この設定では配置しません。"
                : $"「{tile.Label}」を配置しました。YMM4の「元に戻す」1回で戻せます。";
            return count;
        }
        finally
        {
            intentExecuting = false;
            RefreshIntentWorkspace();
        }
    }

    public int ExecuteIntentTile(IntentTileChoice tile)
    {
        if (intentExecuting || tileEditState != IntentTileEditState.Idle || !IntentTiles.Any(x => ReferenceEquals(x, tile)) || selectedIntentSet?.Id != tile.PaletteId)
            throw new InvalidOperationException("表示しているセットが変わりました。演出を選び直してください。");
        if (undo == null || !settingsAvailable) throw new InvalidOperationException("現在は配置できません。Toolと設定を確認してください。");
        intentExecuting = true; ExecuteIntentTileCommand.RaiseCanExecuteChanged();
        try
        {
            int count; bool skipped = false;
            if (tile.IsGeneric)
            {
                if (PlacementContext != PlacementContext.Generic || selectedIntentSet?.Generic == null)
                    throw new InvalidOperationException("時間位置用のセットを選び直してください。");
                if (!GenericLayerReadyForExecution) throw new InvalidOperationException("レイヤーの入力をEnterで適用するか、Escで戻してから配置してください。");
                var palette = settings.Palettes.Single(x => x.Id == tile.PaletteId && x.Kind == PaletteKind.Style);
                if (!palette.LibraryEntryIds.Contains(tile.LibraryEntryId)) throw new InvalidOperationException("セットの演出が変更されました。");
                var source = settings.Library.Single(x => x.Id == tile.LibraryEntryId);
                var plan = QuickDropPlanner.Create(RequireTimeline(), source, palette, CharacterLayerMode.Base);
                count = plan.Plan.Commit(RequireTimeline(), undo);
            }
            else
            {
                if (PlacementContext != PlacementContext.Selection || selectedIntentSet?.Targeted == null)
                    throw new InvalidOperationException("対象アイテム用のセットを選び直してください。");
                var palette = settings.IntentPalettes.Single(x => x.Id == tile.PaletteId);
                var entry = palette.Entries.Single(x => x.LibraryEntryId == tile.LibraryEntryId);
                var plan = IntentExecutionPlan.Create(RequireTimeline(), palette, entry, settings.Library);
                count = plan.Commit(RequireTimeline(), undo); skipped = plan.Skipped;
            }
            HasError = false;
            Status = skipped ? "必要な周囲アイテムが見つからないため、この設定では配置しません。" : $"「{tile.Label}」を配置しました。YMM4の「元に戻す」1回で戻せます。";
            return count;
        }
        finally { intentExecuting = false; RefreshIntentWorkspace(); }
    }
    private void ImportNewIntentExpressions()
    {
        var scan = IntentPaletteBootstrap.Scan(settings, true);
        // Save the complete validated result; failure leaves both live settings and Timeline untouched.
        settingsStore.Save(scan.Settings); settings = scan.Settings;
        RefreshV04(); RefreshIntentWorkspace();
        HasError = false;
        Status = $"新しい表情を{scan.AddedEntries}件取り込みました。" + (scan.Diagnostics.Count > 0 ? "\n" + string.Join("\n", scan.Diagnostics) : "");
    }
}
