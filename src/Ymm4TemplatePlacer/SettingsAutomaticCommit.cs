using System.IO;
using System.Windows;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    private enum SettingsCommitPhase { Idle, Pending, Invalid, Conflict, Committing, Disposed }
    private SettingsCommitPhase settingsCommitPhase;
    private DispatcherOperation? settingsCommitOperation;
    private long settingsCommitEpoch;
    private bool settingsSessionClosed;
    private int settingsCommitRevision;
    private string settingsCommitNotice = "変更は自動で反映されます。";
    private ActionCommand? rollbackIntentSettingsCommand;
    public string SettingsCommitNotice => settingsCommitNotice;
    public ActionCommand RollbackIntentSettingsCommand => rollbackIntentSettingsCommand ??= new(
        _ => settingsAvailable && IntentSettings != null && HasSessionSettingsChanges && settingsCommitPhase != SettingsCommitPhase.Committing,
        _ => Guard(() =>
        {
            CancelScheduledSettingsCommit();
            RollbackSessionSettings();
            SettingsCommitCompleted();
        }));

    private void SetSettingsCommitState(SettingsCommitPhase phase, string notice)
    {
        settingsCommitPhase = phase; settingsCommitNotice = notice;
        OnPropertyChanged(nameof(SettingsCommitNotice)); rollbackIntentSettingsCommand?.RaiseCanExecuteChanged();
    }
    private void CancelScheduledSettingsCommit()
    {
        settingsCommitEpoch++;
        settingsCommitOperation?.Abort(); settingsCommitOperation = null;
    }
    private void ResetAutomaticSettingsSession()
    {
        CancelScheduledSettingsCommit(); settingsSessionClosed = false;
        SetSettingsCommitState(SettingsCommitPhase.Idle, "変更は自動で反映されます。");
    }
    private void SettingsCommitCompleted()
    {
        CancelScheduledSettingsCommit(); settingsCommitRevision++;
        SetSettingsCommitState(SettingsCommitPhase.Idle, "変更を反映しました。今回の変更は元に戻せます。");
    }
    // One Dispatcher operation coalesces an edit burst; there is no polling timer.
    // Focused CI keeps this Settings path in the stable-core native suite.
    private void RequestSettingsAutoCommit()
    {
        rollbackIntentSettingsCommand?.RaiseCanExecuteChanged();
        if (activeTask != "intent-settings" || UseLegacyWorkspace || IntentSettings?.HasChanges != true ||
            settingsCommitPhase is SettingsCommitPhase.Committing or SettingsCommitPhase.Conflict or SettingsCommitPhase.Disposed ||
            settingsCommitOperation != null) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;
        var session = IntentSettings; var epoch = settingsCommitEpoch;
        SetSettingsCommitState(SettingsCommitPhase.Pending, "変更を反映しています…");
        settingsCommitOperation = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (epoch != settingsCommitEpoch || !ReferenceEquals(session, IntentSettings)) return;
            settingsCommitOperation = null;
            if (activeTask == "intent-settings") TryCommitSettingsAutomatically();
        }));
    }
    private bool TryCommitSettingsAutomatically()
    {
        if (settingsCommitPhase is SettingsCommitPhase.Conflict or SettingsCommitPhase.Disposed) return false;
        if (IntentSettings?.HasChanges != true) return true;
        // Validate before persistence. Incomplete input is recoverable by editing;
        // persistence/conflict failure is not retried or silently rebased.
        try { _ = IntentSettings.Build(); }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException)
        {
            SetSettingsCommitState(SettingsCommitPhase.Invalid, "反映待ち: " + ex.Message);
            return false;
        }
        try
        {
            SetSettingsCommitState(SettingsCommitPhase.Committing, "変更を反映しています…");
            SaveIntentSettings();
            return true;
        }
        catch (Exception ex)
        {
            SetSettingsCommitState(SettingsCommitPhase.Conflict, "反映できません: " + ex.GetBaseException().Message);
            HasError = true; Status = settingsCommitNotice;
            return false;
        }
    }
    private void FinishSettingsSession()
    {
        if (settingsCommitPhase == SettingsCommitPhase.Disposed) return;
        CancelScheduledSettingsCommit();
        if (!TryCommitSettingsAutomatically())
        {
            // Leaving a tab/hiding the Tool retains the exact incomplete session.
            settingsSessionClosed = false;
            HasError = true; Status = SettingsCommitNotice + " 設定へ戻ると今回の入力を続けられます。";
        }
        else settingsSessionClosed = true;
    }
    private void DisposeAutomaticSettingsSession()
    {
        CancelScheduledSettingsCommit(); settingsCommitPhase = SettingsCommitPhase.Disposed;
    }
}
