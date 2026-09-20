using System.Text.Json;

namespace Ymm4TemplatePlacer;

/// <summary>
/// One Settings editing session's immutable opening state and latest successful
/// commit. The root still owns the working draft, publication and UI lifetime.
/// This is a settings-only transaction, not a Timeline Undo implementation.
/// </summary>
internal sealed class SettingsEditTransaction
{
    private readonly PlacerSettingsStore store;
    private readonly string openingSnapshot;
    public string LastCommittedFingerprint { get; private set; }
    public bool HasCommittedChanges => LastCommittedFingerprint != openingSnapshot;

    public SettingsEditTransaction(PlacerSettingsStore store, PlacerSettings current)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(current);
        PlacerSettingsStore.Validate(current);
        this.store = store;
        // Keep an immutable value, never a reference to live settings or the draft.
        openingSnapshot = Fingerprint(PlacerSettingsStore.Copy(current));
        LastCommittedFingerprint = openingSnapshot;
    }

    public bool MatchesCurrent(PlacerSettings current) => Fingerprint(current) == LastCommittedFingerprint;

    public PlacerSettings CreateRollbackCandidate() =>
        JsonSerializer.Deserialize<PlacerSettings>(openingSnapshot)
        ?? throw new InvalidOperationException("設定の開始時点を復元できません。現在の設定は変更していません。");

    public PlacerSettings Commit(PlacerSettings current, PlacerSettings candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!MatchesCurrent(current))
            throw new InvalidOperationException("編集中に別の操作で設定が変更されました。今回の編集は保持しています。設定を上書きせず停止しました。");

        // Prepare everything before persistence. A rejected write must not advance
        // the session fingerprint or mutate either caller-owned object.
        var prepared = PlacerSettingsStore.Copy(candidate);
        PlacerSettingsStore.Validate(prepared);
        var fingerprint = Fingerprint(prepared);
        // Even a semantic no-op must pass the existing disk-digest/lock guard.
        // Never reload, retry or rebase a conflict inside this transaction.
        store.Save(prepared);
        LastCommittedFingerprint = fingerprint;
        return prepared;
    }

    public PlacerSettings Rollback(PlacerSettings current) => Commit(current, CreateRollbackCandidate());

    private static string Fingerprint(PlacerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        // Match the existing IntentSettingsSession fingerprint convention.
        return JsonSerializer.Serialize(settings);
    }
}
