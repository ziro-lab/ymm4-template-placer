using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public enum PlacementQuickAction
{
    AlignStart,
    AlignCenter,
    AlignEnd,
    DurationTargetSpan,
    DurationTemplate,
    LayerUpOne,
    LayerDownOne,
    LayerSearchUp,
    LayerSearchDown
}

public sealed partial class PlacerViewModel
{
    private bool placementQuickSettingsOpen;
    private bool placementQuickSettingsBlockedByExistingDraft;
    private bool placementQuickSettingsDirty;
    private IntentPaletteDraft? placementQuickDraft;
    private string placementQuickSettingsNotice = "";

    public IntentPaletteDraft? PlacementQuickDraft
    {
        get => placementQuickDraft;
        private set
        {
            if (ReferenceEquals(placementQuickDraft, value)) return;
            placementQuickDraft = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPlacementQuickDraft));
        }
    }

    public bool HasPlacementQuickDraft => PlacementQuickDraft != null;
    public string PlacementQuickSettingsNotice
    {
        get => placementQuickSettingsNotice;
        private set
        {
            if (placementQuickSettingsNotice == value) return;
            placementQuickSettingsNotice = value;
            OnPropertyChanged();
        }
    }

    public bool CanEditPlacementQuickSettings =>
        placementQuickSettingsOpen &&
        !placementQuickSettingsBlockedByExistingDraft &&
        PlacementQuickDraft != null &&
        selectedIntentSet is { Targeted: not null } set &&
        set.Id == PlacementQuickDraft.Id &&
        IsCurrentIntentSet(set) &&
        settingsAvailable &&
        !intentExecuting &&
        tileEditState == IntentTileEditState.Idle;

    internal bool PlacementQuickSettingsBlocksPlacement => placementQuickSettingsDirty;

    public ActionCommand ApplyPlacementQuickActionCommand { get; private set; } = null!;

    private void InitializePlacementQuickSettings()
    {
        ApplyPlacementQuickActionCommand = new ActionCommand(
            x => x is PlacementQuickAction action && Enum.IsDefined(action) && CanEditPlacementQuickSettings,
            x => Guard(() => ApplyPlacementQuickAction((PlacementQuickAction)x!)));
        OnPropertyChanged(nameof(ApplyPlacementQuickActionCommand));
    }

    internal void BeginPlacementQuickSettings()
    {
        placementQuickSettingsOpen = false;
        placementQuickSettingsBlockedByExistingDraft = false;
        PlacementQuickDraft = null;

        if (selectedIntentSet is not { Targeted: not null } current)
        {
            PlacementQuickSettingsNotice = "対象アイテム用Setではないため、配置のクイック設定はありません。";
            RefreshPlacementQuickSettingsAdmission();
            return;
        }

        BeginIntentSettings();
        if (IntentSettings?.HasChanges == true)
        {
            placementQuickSettingsBlockedByExistingDraft = true;
            PlacementQuickSettingsNotice = "設定に反映待ちの入力があります。通常の設定で確定または「今回の変更を戻す」を行ってください。";
            RefreshPlacementQuickSettingsAdmission();
            return;
        }

        SelectSettingsForSet(current);
        var draft = IntentSettings?.SelectedPalette;
        if (draft == null || draft.Id != current.Id)
            throw new InvalidOperationException("現在のSetの設定を開けません。Setを選び直してください。");

        placementQuickSettingsOpen = true;
        PlacementQuickDraft = draft;
        PlacementQuickSettingsNotice = "よく使う配置結果だけを変更します。変更は同じ設定に自動反映されます。";
        RefreshPlacementQuickSettingsAdmission();
    }

    internal void EndPlacementQuickSettings()
    {
        if (placementQuickSettingsOpen)
            FinishSettingsSession();

        placementQuickSettingsOpen = false;
        placementQuickSettingsBlockedByExistingDraft = false;
        PlacementQuickDraft = null;
        RefreshPlacementQuickSettingsAdmission();
    }

    private void RebindPlacementQuickDraftAfterSettingsReset()
    {
        if (!placementQuickSettingsOpen || selectedIntentSet is not { Targeted: not null } current)
            return;

        SelectSettingsForSet(current);
        PlacementQuickDraft = IntentSettings?.SelectedPalette;
        if (PlacementQuickDraft?.Id != current.Id)
        {
            PlacementQuickDraft = null;
            placementQuickSettingsBlockedByExistingDraft = true;
            PlacementQuickSettingsNotice = "現在のSetの設定を再接続できません。クイック設定を開き直してください。";
        }
        RefreshPlacementQuickSettingsAdmission();
    }

    private void ApplyPlacementQuickAction(PlacementQuickAction action)
    {
        if (!CanEditPlacementQuickSettings || PlacementQuickDraft is not { } draft)
            throw new InvalidOperationException("現在のSetでは配置のクイック設定を変更できません。");

        switch (action)
        {
            case PlacementQuickAction.AlignStart:
                if (draft.Anchor != IntentAnchor.SelectedStart) draft.Anchor = IntentAnchor.SelectedStart;
                if (draft.Alignment != IntentAlignment.StartAtAnchor) draft.Alignment = IntentAlignment.StartAtAnchor;
                break;
            case PlacementQuickAction.AlignCenter:
                if (draft.Anchor != IntentAnchor.SelectedCenter) draft.Anchor = IntentAnchor.SelectedCenter;
                if (draft.Alignment != IntentAlignment.CenterAtAnchor) draft.Alignment = IntentAlignment.CenterAtAnchor;
                break;
            case PlacementQuickAction.AlignEnd:
                if (draft.Anchor != IntentAnchor.SelectedEnd) draft.Anchor = IntentAnchor.SelectedEnd;
                if (draft.Alignment != IntentAlignment.EndAtAnchor) draft.Alignment = IntentAlignment.EndAtAnchor;
                break;
            case PlacementQuickAction.DurationTargetSpan:
                if (draft.Duration != IntentDuration.TargetSpan) draft.Duration = IntentDuration.TargetSpan;
                break;
            case PlacementQuickAction.DurationTemplate:
                if (draft.Duration != IntentDuration.Template) draft.Duration = IntentDuration.Template;
                break;
            case PlacementQuickAction.LayerUpOne:
                if (draft.LayerMode != LayerPlacementMode.RelativeToTarget) draft.LayerMode = LayerPlacementMode.RelativeToTarget;
                if (draft.Direction != RelativeLayerDirection.Up) draft.Direction = RelativeLayerDirection.Up;
                if (draft.LayerOffset != "1") draft.LayerOffset = "1";
                break;
            case PlacementQuickAction.LayerDownOne:
                if (draft.LayerMode != LayerPlacementMode.RelativeToTarget) draft.LayerMode = LayerPlacementMode.RelativeToTarget;
                if (draft.Direction != RelativeLayerDirection.Down) draft.Direction = RelativeLayerDirection.Down;
                if (draft.LayerOffset != "1") draft.LayerOffset = "1";
                break;
            case PlacementQuickAction.LayerSearchUp:
                if (draft.LayerMode != LayerPlacementMode.Absolute) draft.LayerMode = LayerPlacementMode.Absolute;
                if (draft.Direction != RelativeLayerDirection.Up) draft.Direction = RelativeLayerDirection.Up;
                break;
            case PlacementQuickAction.LayerSearchDown:
                if (draft.LayerMode != LayerPlacementMode.Absolute) draft.LayerMode = LayerPlacementMode.Absolute;
                if (draft.Direction != RelativeLayerDirection.Down) draft.Direction = RelativeLayerDirection.Down;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        RefreshPlacementQuickSettingsAdmission();
    }

    private void ObservePlacementQuickSettingsEdited()
    {
        if (!placementQuickSettingsOpen || IntentSettings?.HasChanges != true) return;
        placementQuickSettingsDirty = true;
        PlacementQuickSettingsNotice = "変更を反映しています…";
        UpdateIntentTileEditingCommands();
    }

    private void PlacementQuickSettingsCommitCompleted()
    {
        if (!placementQuickSettingsDirty) return;
        placementQuickSettingsDirty = false;
        if (placementQuickSettingsOpen)
            PlacementQuickSettingsNotice = "変更を反映しました。次の配置からこの設定を使います。";
        UpdateIntentTileEditingCommands();
    }

    private void RefreshPlacementQuickSettingsAdmission()
    {
        OnPropertyChanged(nameof(CanEditPlacementQuickSettings));
        ApplyPlacementQuickActionCommand?.RaiseCanExecuteChanged();
        UpdateIntentTileEditingCommandsCore();
    }

    // Keep this small to avoid recursive refresh between the quick-settings and tile-edit admission paths.
    private void UpdateIntentTileEditingCommandsCore()
    {
        ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
    }
}
