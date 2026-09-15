using System.Collections.ObjectModel;
using System.ComponentModel;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

public sealed record IntentTabChoice(string Name);
public sealed record IntentSetChoice(IntentPalette Palette, string Label);
public sealed record IntentTileChoice(Guid PaletteId, IntentEntry Entry, string Label, string Detail, bool Available);

public sealed partial class PlacerViewModel
{
    private Timeline? intentTimeline;
    private bool intentInitialized, refreshingIntent, intentExecuting, useLegacyWorkspace;
    private string intentNotice = "", intentContextKey = "";
    private IntentTabChoice? selectedIntentTab;
    private IntentSetChoice? selectedIntentSet;
    private readonly Dictionary<string, Guid> lastIntentSets = new(StringComparer.Ordinal);
    public ObservableCollection<IntentTabChoice> IntentTabs { get; } = [];
    public ObservableCollection<IntentSetChoice> IntentSets { get; } = [];
    public ObservableCollection<IntentTileChoice> IntentTiles { get; } = [];
    public bool UseLegacyWorkspace => useLegacyWorkspace;
    public bool HasIntentSets => IntentSets.Count > 1;
    public string IntentNotice { get => intentNotice; private set => Set(ref intentNotice, value); }
    public IntentTabChoice? SelectedIntentTab
    {
        get => selectedIntentTab;
        set
        {
            if (refreshingIntent || value == null || !IntentTabs.Contains(value) || value == selectedIntentTab) return;
            selectedIntentTab = value; OnPropertyChanged(); RefreshIntentSets(null);
        }
    }
    public IntentSetChoice? SelectedIntentSet
    {
        get => selectedIntentSet;
        set
        {
            if (refreshingIntent || value == null || !IntentSets.Contains(value) || value == selectedIntentSet) return;
            selectedIntentSet = value; OnPropertyChanged(); RememberIntentSet(); RefreshIntentTiles();
        }
    }
    public ActionCommand ExecuteIntentTileCommand { get; private set; } = null!;
    public ActionCommand OpenIntentSettingsCommand { get; private set; } = null!;
    public ActionCommand OpenLegacyWorkspaceCommand { get; private set; } = null!;
    public ActionCommand CloseLegacyWorkspaceCommand { get; private set; } = null!;
    public ActionCommand ImportNewExpressionsCommand { get; private set; } = null!;
    public event EventHandler? IntentSettingsRequested;

    // The View calls this on attachment and scene changes; it is idempotent and also usable by native tests.
    public void ActivateIntentWorkspace()
    {
        if (!intentInitialized)
        {
            intentInitialized = true;
            useLegacyWorkspace = settings.LegacyWorkspace;
            ExecuteIntentTileCommand = new ActionCommand(x => !intentExecuting && settingsAvailable && undo != null &&
                x is IntentTileChoice tile && tile.Available && IntentTiles.Contains(tile),
                x => Guard(() => ExecuteIntentTile((IntentTileChoice)x!)));
            OpenIntentSettingsCommand = new ActionCommand(_ => true, _ => IntentSettingsRequested?.Invoke(this, EventArgs.Empty));
            OpenLegacyWorkspaceCommand = new ActionCommand(_ => true, _ => SetLegacyWorkspace(true));
            CloseLegacyWorkspaceCommand = new ActionCommand(_ => true, _ => SetLegacyWorkspace(false));
            ImportNewExpressionsCommand = new ActionCommand(_ => settingsAvailable, _ => Guard(ImportNewIntentExpressions));
        }
        if (!ReferenceEquals(intentTimeline, timeline))
        {
            DeactivateIntentWorkspace(); intentTimeline = timeline;
            if (intentTimeline != null) intentTimeline.PropertyChanged += IntentTimelineChanged;
        }
        RefreshIntentWorkspace();
    }
    public void DeactivateIntentWorkspace()
    {
        if (intentTimeline != null) intentTimeline.PropertyChanged -= IntentTimelineChanged;
        intentTimeline = null;
    }
    public void SetLegacyWorkspace(bool value)
    {
        if (useLegacyWorkspace == value) return;
        useLegacyWorkspace = value; OnPropertyChanged(nameof(UseLegacyWorkspace));
        // Workspace navigation is session-only. Switching tabs/modes must not save settings or mutate Timeline.
        RefreshIntentWorkspace();
    }
    private void IntentTimelineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Timeline.SelectedItems) or nameof(Timeline.SelectedItem) or nameof(Timeline.Items))
            RefreshIntentWorkspace();
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
            var preferred = selectedIntentSet?.Palette.Id;
            IntentTabs.Clear(); IntentSets.Clear(); IntentTiles.Clear(); selectedIntentTab = null; selectedIntentSet = null;
            if (!settingsAvailable) { IntentNotice = LibraryNotice; return; }
            if (timeline == null || timeline.SelectedItems.Count == 0)
            { IntentNotice = "タイムラインで基準にするアイテムを選択してください。"; return; }
            var context = IntentSelectionContext.Capture(timeline);
            var nextKey = string.Join("|", context.Selected.Select(x => x.TypeKey + ":" + x.CharacterName)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)) + ":" + context.Selected.Count;
            if (nextKey != intentContextKey) preferred = lastIntentSets.GetValueOrDefault(nextKey);
            intentContextKey = nextKey;
            var applicable = settings.IntentPalettes.Where(x => x.Target.Matches(context)).ToArray();
            foreach (var name in applicable.Select(x => x.Intent).Distinct(StringComparer.Ordinal)) IntentTabs.Add(new(name));
            var chosen = applicable.FirstOrDefault(x => x.Id == preferred) ?? applicable.FirstOrDefault();
            selectedIntentTab = IntentTabs.FirstOrDefault(x => x.Name == chosen?.Intent);
            IntentNotice = chosen == null ? "この種類・選択の組み合わせ用のパレットはまだありません。" : "";
            FillIntentSets(applicable, chosen?.Id);
        }
        catch (Exception ex)
        {
            IntentTabs.Clear(); IntentSets.Clear(); IntentTiles.Clear(); selectedIntentTab = null; selectedIntentSet = null;
            IntentNotice = ex.GetBaseException().Message;
        }
        finally
        {
            refreshingIntent = false;
            OnPropertyChanged(nameof(SelectedIntentTab)); OnPropertyChanged(nameof(SelectedIntentSet));
            OnPropertyChanged(nameof(HasIntentSets)); ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
        }
    }
    private void RefreshIntentSets(Guid? preferred)
    {
        if (timeline == null) return;
        refreshingIntent = true;
        try
        {
            var context = IntentSelectionContext.Capture(timeline);
            FillIntentSets(settings.IntentPalettes.Where(x => x.Target.Matches(context)).ToArray(), preferred);
        }
        finally { refreshingIntent = false; }
        OnPropertyChanged(nameof(SelectedIntentSet)); OnPropertyChanged(nameof(HasIntentSets));
    }
    private void FillIntentSets(IReadOnlyList<IntentPalette> applicable, Guid? preferred)
    {
        IntentSets.Clear();
        var sets = applicable.Where(x => x.Intent == selectedIntentTab?.Name).ToArray();
        foreach (var palette in sets)
        {
            var peers = sets.Where(x => x.Name == palette.Name).ToArray();
            var label = peers.Length > 1 ? $"{palette.Name} ({Array.IndexOf(peers, palette) + 1})" : palette.Name;
            IntentSets.Add(new(palette, label));
        }
        selectedIntentSet = IntentSets.FirstOrDefault(x => x.Palette.Id == preferred) ?? IntentSets.FirstOrDefault();
        RememberIntentSet(); RefreshIntentTiles();
    }
    private void RememberIntentSet()
    {
        if (selectedIntentSet == null) return;
        if (lastIntentSets.Count >= 256 && !lastIntentSets.ContainsKey(intentContextKey)) lastIntentSets.Remove(lastIntentSets.Keys.First());
        lastIntentSets[intentContextKey] = selectedIntentSet.Palette.Id;
    }
    private void RefreshIntentTiles()
    {
        IntentTiles.Clear();
        var palette = selectedIntentSet?.Palette;
        if (palette == null) return;
        var duplicateNames = palette.Entries.Select(x => settings.Library.SingleOrDefault(e => e.Id == x.LibraryEntryId))
            .OfType<LibraryEntry>().GroupBy(x => x.DisplayName, StringComparer.Ordinal).Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var tile in palette.Entries)
        {
            var source = settings.Library.SingleOrDefault(x => x.Id == tile.LibraryEntryId);
            if (source == null) { IntentTiles.Add(new(palette.Id, tile, "参照切れ", "設定で元テンプレートの登録を確認してください。", false)); continue; }
            var resolution = TemplateResolver.ResolveBundle(source);
            var label = duplicateNames.Contains(source.DisplayName) ? $"{source.DisplayName} — {source.Source.Name}" : source.DisplayName;
            IntentTiles.Add(new(palette.Id, tile, label, resolution.Bundle == null ? resolution.Message : source.Source.Name, resolution.Bundle != null));
        }
        IntentNotice = IntentTiles.Count == 0 ? "このセットには演出がありません。設定でテンプレートを追加してください。" : "";
        ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
    }
    public int ExecuteIntentTile(IntentTileChoice tile)
    {
        if (intentExecuting || !IntentTiles.Contains(tile) || selectedIntentSet?.Palette.Id != tile.PaletteId)
            throw new InvalidOperationException("表示しているパレットが変わりました。演出を選び直してください。");
        if (undo == null || !settingsAvailable) throw new InvalidOperationException("現在は配置できません。Toolと設定を確認してください。");
        intentExecuting = true; ExecuteIntentTileCommand.RaiseCanExecuteChanged();
        try
        {
            var palette = settings.IntentPalettes.Single(x => x.Id == tile.PaletteId);
            var plan = IntentExecutionPlan.Create(RequireTimeline(), palette, tile.Entry, settings.Library);
            var count = plan.Commit(RequireTimeline(), undo);
            HasError = false;
            Status = plan.Skipped ? "参照するアイテムがないため、設定どおり配置しませんでした。" : $"「{tile.Label}」を配置しました。YMM4の「元に戻す」1回で戻せます。";
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
