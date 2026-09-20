using System.Windows;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private PalettePresentationDraft? panelQuickPresentation;
    private DispatcherOperation? panelQuickCommitOperation;
    private long panelQuickCommitEpoch;
    private bool panelQuickDraftDirty;
    private string panelQuickSettingsNotice = "配置画面の見た目・操作だけをすばやく調整します。";
    private ActionCommand? shapeCurrentIntentSetCommand;

    public PalettePresentationDraft? PanelQuickPresentation
    {
        get => panelQuickPresentation;
        private set
        {
            if (ReferenceEquals(panelQuickPresentation, value)) return;
            if (panelQuickPresentation != null) panelQuickPresentation.Edited -= PanelQuickPresentationEdited;
            panelQuickPresentation = value;
            if (panelQuickPresentation != null) panelQuickPresentation.Edited += PanelQuickPresentationEdited;
            OnPropertyChanged();
        }
    }

    public string PanelQuickSettingsNotice
    {
        get => panelQuickSettingsNotice;
        private set
        {
            if (panelQuickSettingsNotice == value) return;
            panelQuickSettingsNotice = value;
            OnPropertyChanged();
        }
    }

    public bool CanEditPanelQuickSettings => settingsAvailable && !intentExecuting &&
        tileEditState == IntentTileEditState.Idle && IntentSettings?.HasChanges != true;

    public ActionCommand ShapeCurrentIntentSetCommand => shapeCurrentIntentSetCommand ??= new(
        x => x is IntentTileShape shape && Enum.IsDefined(shape) && selectedIntentSet is { } set && CanEditIntentSet(set),
        x => Guard(() =>
        {
            var set = selectedIntentSet ?? throw new InvalidOperationException("表示中のSetがありません。");
            ChangeIntentSetShape(set, (IntentTileShape)x!);
            PanelQuickSettingsNotice = "現在のSetの形を変更しました。";
        }));

    public void BeginPanelQuickSettings()
    {
        if (PanelQuickPresentation == null || !panelQuickDraftDirty)
        {
            PanelQuickPresentation = new(settings.Presentation);
            panelQuickDraftDirty = false;
        }
        PanelQuickSettingsNotice = CanEditPanelQuickSettings
            ? "変更は有効な値になった時点で自動反映されます。"
            : "設定タブに反映待ちの入力があります。確定または「今回の変更を戻す」後に変更できます。";
        OnPropertyChanged(nameof(CanEditPanelQuickSettings));
        ShapeCurrentIntentSetCommand.RaiseCanExecuteChanged();
    }

    public void EndPanelQuickSettings()
    {
        // Popup lifetime is presentation-only. Edits schedule their own commit;
        // opening/closing the popup itself never writes settings.
    }

    private void PanelQuickPresentationEdited(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, PanelQuickPresentation)) return;
        panelQuickDraftDirty = true;
        if (!CanEditPanelQuickSettings)
        {
            PanelQuickSettingsNotice = "反映待ち: 設定タブの入力を先に確定または戻してください。";
            return;
        }
        if (panelQuickCommitOperation != null) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;
        var draft = PanelQuickPresentation;
        var epoch = panelQuickCommitEpoch;
        PanelQuickSettingsNotice = "変更を反映しています…";
        panelQuickCommitOperation = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (epoch != panelQuickCommitEpoch || !ReferenceEquals(draft, PanelQuickPresentation)) return;
            panelQuickCommitOperation = null;
            CommitPanelQuickPresentation(draft!);
        }));
    }

    private void CommitPanelQuickPresentation(PalettePresentationDraft draft)
    {
        if (!CanEditPanelQuickSettings)
        {
            PanelQuickSettingsNotice = "反映待ち: 設定タブの入力を先に確定または戻してください。";
            return;
        }

        PalettePresentationSettings edited;
        try { edited = draft.Build(); }
        catch (Exception ex) when (ex is InvalidOperationException or System.IO.InvalidDataException)
        {
            PanelQuickSettingsNotice = "反映待ち: " + ex.Message;
            return;
        }

        var current = settings.Presentation;
        var merged = current with
        {
            LayoutMode = edited.LayoutMode,
            FixedColumns = edited.FixedColumns,
            ShortcutsEnabled = edited.ShortcutsEnabled,
            PositionShortcuts = edited.PositionShortcuts
        };

        var unchanged = merged.LayoutMode == current.LayoutMode &&
            merged.FixedColumns == current.FixedColumns &&
            merged.ShortcutsEnabled == current.ShortcutsEnabled &&
            merged.PositionShortcuts.SequenceEqual(current.PositionShortcuts);
        if (unchanged)
        {
            panelQuickDraftDirty = false;
            PanelQuickSettingsNotice = "変更は反映済みです。";
            return;
        }

        try
        {
            var next = PlacerSettingsStore.Copy(settings);
            next.Presentation = merged;
            settingsStore.Save(next);
            settings = next;
            panelQuickDraftDirty = false;

            // A clean full Settings session may safely be rebuilt from the newly
            // committed presentation. A dirty session is blocked above.
            if (IntentSettings != null) ResetIntentSettings();

            OnPropertyChanged(nameof(PaletteLayout));
            OnPropertyChanged(nameof(PaletteFixedColumns));
            UpdateIntentTileEditingCommands();
            HasError = false;
            PanelQuickSettingsNotice = "変更を反映しました。";
        }
        catch (Exception ex)
        {
            HasError = true;
            PanelQuickSettingsNotice = "反映できません: " + ex.GetBaseException().Message;
            Status = PanelQuickSettingsNotice;
        }
    }

    private void CancelPanelQuickCommit()
    {
        panelQuickCommitEpoch++;
        panelQuickCommitOperation?.Abort();
        panelQuickCommitOperation = null;
    }

    private void RefreshPanelQuickSettingsAdmission()
    {
        OnPropertyChanged(nameof(CanEditPanelQuickSettings));
        shapeCurrentIntentSetCommand?.RaiseCanExecuteChanged();
    }
}
