using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    // CT is only the R4-C transaction prerequisite. It does NOT mark full C or
    // full Round 4 complete: root auto-commit/UI integration is a later checkpoint.
    private static void VerifyHandsOnRound4SettingsTransaction(Timeline timeline)
    {
        stage = "R4-C/CT settings transaction prerequisite (no autosave UI yet)";
        var directory = Path.Combine(output, "round4-c-transaction-fixtures");
        Directory.CreateDirectory(directory);
        var timelineBefore = Signature(timeline);
        var itemsBefore = timeline.Items.ToArray();
        var templatesBefore = ItemSettings.Default.Templates.ToArray();
        var sourceItemsBefore = templatesBefore.SelectMany(x => x.Items).ToArray();
        var sourceValuesBefore = sourceItemsBefore.Select(x => (x.Frame, x.Length, x.Layer, x.Remark)).ToArray();
        var liveSettingsBefore = JsonSerializer.Serialize(ViewModel!.GetRound4SettingsForTransactionProof());

        static string Json(PlacerSettings value) => JsonSerializer.Serialize(value);
        static bool Rejected(Action action)
        {
            try { action(); return false; }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
            { return true; }
        }
        (PlacerSettingsStore Store, PlacerSettings Live, string Path) Fixture(string name)
        {
            var path = Path.Combine(directory, name + ".json");
            if (File.Exists(path)) File.Delete(path);
            var store = new PlacerSettingsStore(path);
            var live = store.Load();
            live.ExpressionBootstrapComplete = true;
            live.LegacyWorkspace = false;
            var set = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "開始時のセット", null, [])
            { Layer = new() { UseTemplateLayer = false, Minimum = 2, Maximum = 20, Preferred = 8 } };
            live.Palettes = [set]; live.ManualStylePaletteId = set.Id;
            store.Save(live);
            return (store, live, path);
        }

        var fixture = Fixture("session");
        var initial = Json(fixture.Live);
        var bytes = File.ReadAllBytes(fixture.Path);
        var transaction = new SettingsEditTransaction(fixture.Store, fixture.Live);
        Round4Assert(!transaction.HasCommittedChanges && transaction.MatchesCurrent(fixture.Live) &&
            File.ReadAllBytes(fixture.Path).SequenceEqual(bytes), "CT1", "opening a transaction is clean and performs no settings write");

        fixture.Live.Palettes[0].LibraryEntryIds.Add(Guid.NewGuid());
        Round4Assert(Json(transaction.CreateRollbackCandidate()) == initial && !transaction.MatchesCurrent(fixture.Live),
            "CT2", "opening snapshot is immutable even when the original live object is later mutated");
        var current = transaction.CreateRollbackCandidate();
        var escaped = transaction.CreateRollbackCandidate(); escaped.Palettes.Clear();
        Round4Assert(Json(transaction.CreateRollbackCandidate()) == initial && transaction.MatchesCurrent(current),
            "CT3", "rollback candidates are detached copies and cannot alter the stored opening snapshot");

        var draft = new IntentSettingsSession(current, new[] { typeof(VoiceItem) });
        var setDraft = draft.GenericSets.Single();
        setDraft.Name = "今回の変更";
        var candidate = draft.Build();
        Round4Assert(draft.HasChanges && Json(current) == initial && File.ReadAllBytes(fixture.Path).SequenceEqual(bytes),
            "CT4", "the existing Settings working draft stays separate from live/opening state before commit");
        var first = transaction.Commit(current, candidate);
        Round4Assert(transaction.HasCommittedChanges && transaction.MatchesCurrent(first) &&
            Json(new PlacerSettingsStore(fixture.Path).Load()) == Json(first) && Json(current) == initial,
            "CT5", "a valid full draft is atomically persisted before the caller publishes its detached result");
        candidate.Palettes.Clear();
        Round4Assert(first.Palettes.Count == 1 && transaction.MatchesCurrent(first) &&
            Json(transaction.CreateRollbackCandidate()) == initial, "CT6", "mutating the submitted draft cannot mutate the committed result or the opening snapshot");

        var secondDraft = PlacerSettingsStore.Copy(first);
        secondDraft.Presentation = secondDraft.Presentation with { FixedColumns = 5, ExpressionRowHeight = 64 };
        var second = transaction.Commit(first, secondDraft);
        Round4Assert(transaction.MatchesCurrent(second) && Json(transaction.CreateRollbackCandidate()) == initial &&
            Json(new PlacerSettingsStore(fixture.Path).Load()) == Json(second),
            "CT7", "multiple successful commits advance only the latest fingerprint, not the session-open baseline");
        var stable = File.ReadAllBytes(fixture.Path); var committed = transaction.LastCommittedFingerprint;
        setDraft.Preferred = "入力途中";
        Round4Assert(Rejected(() => transaction.Commit(second, draft.Build())) && setDraft.Preferred == "入力途中" && draft.HasChanges &&
            File.ReadAllBytes(fixture.Path).SequenceEqual(stable) && transaction.LastCommittedFingerprint == committed,
            "CT8", "invalid existing draft text is retained and rejected before any settings write or fingerprint change");
        var invalid = PlacerSettingsStore.Copy(second);
        invalid.Presentation = invalid.Presentation with { FixedColumns = 0 };
        Round4Assert(Rejected(() => transaction.Commit(second, invalid)) && invalid.Presentation.FixedColumns == 0 &&
            File.ReadAllBytes(fixture.Path).SequenceEqual(stable) && transaction.LastCommittedFingerprint == committed,
            "CT9", "invalid complete settings are rejected without mutating caller input, disk or transaction state");
        var competingLive = PlacerSettingsStore.Copy(second); competingLive.NextAssociationId++;
        Round4Assert(Rejected(() => transaction.Commit(competingLive, secondDraft)) &&
            Rejected(() => transaction.Rollback(competingLive)) && File.ReadAllBytes(fixture.Path).SequenceEqual(stable) &&
            transaction.LastCommittedFingerprint == committed, "CT10", "competing in-process state blocks both commit and rollback without rebasing");

        var rolledBack = transaction.Rollback(second);
        Round4Assert(Json(rolledBack) == initial && Json(new PlacerSettingsStore(fixture.Path).Load()) == initial &&
            !transaction.HasCommittedChanges && transaction.MatchesCurrent(rolledBack),
            "CT11", "rollback restores the complete opening settings snapshot through the same protected store");
        var third = PlacerSettingsStore.Copy(rolledBack); third.Presentation = third.Presentation with { FixedColumns = 6 };
        third = transaction.Commit(rolledBack, third); third = transaction.Rollback(third);
        Round4Assert(Json(third) == initial && !transaction.HasCommittedChanges,
            "CT12", "editing again after rollback still returns to the same immutable session opening state");

        var diskCase = Fixture("external-conflict");
        var diskTransaction = new SettingsEditTransaction(diskCase.Store, diskCase.Live);
        var otherStore = new PlacerSettingsStore(diskCase.Path); var other = otherStore.Load();
        other.Presentation = other.Presentation with { FixedColumns = 7 }; otherStore.Save(other);
        var externalBytes = File.ReadAllBytes(diskCase.Path); var diskFingerprint = diskTransaction.LastCommittedFingerprint;
        Round4Assert(Rejected(() => diskTransaction.Commit(diskCase.Live, secondDraft)) &&
            Rejected(() => diskTransaction.Rollback(diskCase.Live)) && File.ReadAllBytes(diskCase.Path).SequenceEqual(externalBytes) &&
            diskTransaction.LastCommittedFingerprint == diskFingerprint, "CT13", "an independent store write blocks both commit and rollback through the existing digest guard");
        Round4Assert(Rejected(() => diskTransaction.Commit(diskCase.Live, PlacerSettingsStore.Copy(diskCase.Live))) &&
            File.ReadAllBytes(diskCase.Path).SequenceEqual(externalBytes),
            "CT14", "even a semantic no-op cannot bypass an external disk conflict");

        var locked = Fixture("locked-store"); var lockedTransaction = new SettingsEditTransaction(locked.Store, locked.Live);
        var lockedBytes = File.ReadAllBytes(locked.Path); var lockedFingerprint = lockedTransaction.LastCommittedFingerprint;
        using (var saveLock = new FileStream(locked.Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            Round4Assert(Rejected(() => lockedTransaction.Commit(locked.Live, secondDraft)) &&
                File.ReadAllBytes(locked.Path).SequenceEqual(lockedBytes) && lockedTransaction.LastCommittedFingerprint == lockedFingerprint,
                "CT15", "a real cross-instance save lock rejects the write without advancing session state");

        var corrupt = Fixture("corrupt-store"); var corruptTransaction = new SettingsEditTransaction(corrupt.Store, corrupt.Live);
        File.WriteAllText(corrupt.Path, "{not-valid-json"); var corruptBytes = File.ReadAllBytes(corrupt.Path);
        Round4Assert(Rejected(() => corruptTransaction.Rollback(corrupt.Live)) &&
            File.ReadAllBytes(corrupt.Path).SequenceEqual(corruptBytes) && corruptTransaction.MatchesCurrent(corrupt.Live),
            "CT16", "corrupt externally modified bytes are preserved instead of being overwritten by rollback");

        var unloadedPath = Path.Combine(directory, "not-loaded.json"); if (File.Exists(unloadedPath)) File.Delete(unloadedPath);
        var unloadedTransaction = new SettingsEditTransaction(new PlacerSettingsStore(unloadedPath), third);
        Round4Assert(Rejected(() => unloadedTransaction.Commit(third, secondDraft)) && !File.Exists(unloadedPath) &&
            !unloadedTransaction.HasCommittedChanges, "CT17", "transaction does not silently load or authorize an uninitialized settings store");

        var stale = Fixture("stale-second-session");
        var txA = new SettingsEditTransaction(stale.Store, stale.Live); var txB = new SettingsEditTransaction(stale.Store, stale.Live);
        var updated = txA.Commit(stale.Live, secondDraft); var updatedBytes = File.ReadAllBytes(stale.Path);
        Round4Assert(Rejected(() => txB.Rollback(updated)) && File.ReadAllBytes(stale.Path).SequenceEqual(updatedBytes) &&
            !txB.HasCommittedChanges, "CT18", "a stale session cannot undo another session's newly published live settings");
        Round4Assert(Signature(timeline) == timelineBefore && timeline.Items.SequenceEqual(itemsBefore) &&
            JsonSerializer.Serialize(ViewModel!.GetRound4SettingsForTransactionProof()) == liveSettingsBefore,
            "CT19", "transaction tests mutate neither Timeline items nor the live product root settings");
        Round4Assert(ItemSettings.Default.Templates.SequenceEqual(templatesBefore) &&
            templatesBefore.SelectMany(x => x.Items).SequenceEqual(sourceItemsBefore) &&
            sourceItemsBefore.Select(x => (x.Frame, x.Length, x.Layer, x.Remark)).SequenceEqual(sourceValuesBefore),
            "CT20", "transaction tests preserve original host templates and source item identities/properties");
        Round4Phase("CT");
    }
}

public sealed partial class PlacerViewModel
{
    // Compiled only in the isolated proof assembly alongside all tests/*.cs.
    internal PlacerSettings GetRound4SettingsForTransactionProof() => settings;
}
