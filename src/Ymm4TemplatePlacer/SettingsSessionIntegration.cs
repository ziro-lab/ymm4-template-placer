using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed partial class IntentSettingsSession
{
    // Accept persistence without replacing any bound draft, selection or filter.
    // BaselineFingerprint remains the opening snapshot for compatibility callers;
    // SettingsEditTransaction alone tracks the latest successfully committed state.
    internal void AcceptCommitted(PlacerSettings committed)
    {
        working = PlacerSettingsStore.Copy(committed);
        HasChanges = false;
        Raise(nameof(HasChanges)); Raise(nameof(ChangeNotice));
    }
}

public sealed partial class PlacerViewModel
{
    private SettingsEditTransaction? settingsTransaction;
    private void ResetSettingsTransaction()
    {
        ResetAutomaticSettingsSession();
        settingsTransaction = settingsAvailable ? new SettingsEditTransaction(settingsStore, settings) : null;
    }
    private SettingsEditTransaction RequireSettingsTransaction() => settingsTransaction
        ?? throw new InvalidOperationException("設定の編集を開始できません。設定を確認して開き直してください。");
    internal bool HasSessionSettingsChanges => IntentSettings?.HasChanges == true || settingsTransaction?.HasCommittedChanges == true;

    internal void RollbackSessionSettings()
    {
        if (!settingsAvailable || IntentSettings == null) throw new InvalidOperationException("設定を戻せません。");
        var session = IntentSettings;
        var contextKey = session.SelectedItemContext?.Key;
        var targetedId = session.SelectedPalette?.Id;
        var genericId = session.SelectedGenericSet?.Id;
        var targetedEntry = session.SelectedPalette?.SelectedEntry?.LibraryEntryId;
        var genericEntry = session.SelectedGenericSet?.SelectedEntry?.LibraryEntryId;
        var search = session.SourceSearch; var showAll = session.ShowAllSets;
        // Rebuild the rollback draft before any write. A bad source/constructor
        // cannot leave the UI claiming failure after partially committing.
        var transaction = RequireSettingsTransaction();
        var candidate = transaction.CreateRollbackCandidate();
        var types = (timeline?.Items.Select(x => x.GetType()) ?? [])
            .Concat(ItemSettings.Default.Templates.SelectMany(x => x.Items).Select(x => x.GetType()));
        var restored = new IntentSettingsSession(candidate, types, timeline?.SelectedItems.ToArray() ?? []);
        restored.ShowAllSets = showAll;
        restored.SelectedItemContext = restored.ItemContexts.FirstOrDefault(x => x.Key == contextKey);
        if (targetedId.HasValue) restored.SelectedPalette = restored.Palettes.FirstOrDefault(x => x.Id == targetedId) ?? restored.SelectedPalette;
        if (genericId.HasValue) restored.SelectedGenericSet = restored.GenericSets.FirstOrDefault(x => x.Id == genericId) ?? restored.SelectedGenericSet;
        if (restored.SelectedPalette is { } p) p.SelectedEntry = p.Entries.FirstOrDefault(x => x.LibraryEntryId == targetedEntry);
        if (restored.SelectedGenericSet is { } g) g.SelectedEntry = g.Entries.FirstOrDefault(x => x.LibraryEntryId == genericEntry);
        restored.SourceSearch = search;
        settings = transaction.Commit(settings, candidate);
        session.Edited -= IntentSettingsEdited;
        IntentSettings = restored; restored.Edited += IntentSettingsEdited;
        RefreshV04(); RefreshIntentWorkspace(); RefreshExpressionVocabulary();
        UpdateIntentSettingsCommands();
        HasError = false; Status = "今回の設定変更を戻しました。タイムラインと元テンプレートは変更していません。";
    }
}
