namespace Ymm4TemplatePlacer;

public enum ExpressionSourceMode { Template, TachiePreset }

public sealed partial class PlacerSettings
{
    public ExpressionSourceMode ExpressionSourceMode { get; set; } = ExpressionSourceMode.Template;
}

public sealed partial class PlacerViewModel
{
    private ExpressionSourceMode expressionSourceMode = ExpressionSourceMode.Template;
    internal ExpressionSourceMode CurrentExpressionSourceMode => expressionSourceMode;
    public bool IsTemplateExpressionSource
    {
        get => expressionSourceMode == ExpressionSourceMode.Template;
        set { if (value) TrySetExpressionSourceMode(ExpressionSourceMode.Template); }
    }
    public bool IsTachiePresetExpressionSource
    {
        get => expressionSourceMode == ExpressionSourceMode.TachiePreset;
        set { if (value) TrySetExpressionSourceMode(ExpressionSourceMode.TachiePreset); }
    }
    public bool ExpressionRowsMatchSource => expressionRowsSource == expressionSourceMode;
    public string ExpressionChoiceColumnTitle => IsTemplateExpressionSource ? "テンプレート" : "表情プリセット";
    public string ExpressionSourceNotice => IsTachiePresetExpressionSource
        ? "候補は選択すると安全なfresh表情アイテム経路で即時反映を試します。実験候補は失敗する場合がありますが、配置できればPreviewですぐ確認できます。Excelはテンプレート表示で利用できます。"
        : "";

    private bool HasProtectedExpressionSourceWork() => HasProtectedPendingVoiceWork() ||
        (IsTemplateExpressionSource && (!UsesRelativeExpressions ? SelectedExpressionCount > 0 : PendingRelativeExpressionCount > 0));

    private void RestoreExpressionSourceModePreference()
    {
        expressionSourceMode = settingsAvailable && Enum.IsDefined(settings.ExpressionSourceMode)
            ? settings.ExpressionSourceMode
            : ExpressionSourceMode.Template;
    }

    private bool PersistExpressionSourceModePreference(ExpressionSourceMode next)
    {
        if (!settingsAvailable) return true;
        try
        {
            var saved = PlacerSettingsStore.Copy(settings);
            saved.ExpressionSourceMode = next;
            settingsStore.Save(saved);
            settings.ExpressionSourceMode = next;
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            Status = "表情の表示モードを保存できませんでした: " + ex.GetBaseException().Message;
            return false;
        }
    }

    private bool TrySetExpressionSourceMode(ExpressionSourceMode next)
    {
        if (expressionSourceMode == next) return true;
        if (next == ExpressionSourceMode.TachiePreset && HasProtectedExpressionSourceWork())
        {
            HasError = false;
            Status = "未配置のテンプレート / Excel割り当てが残っています。配置するか一覧を読み直してから、表情プリセットへ切り替えてください。";
            PublishExpressionSourceProperties();
            return false;
        }
        CancelExpressionNavigation();
        CancelTachiePresetApply();
        CancelTachiePresetCalibration();
        CloseExpressionTrialSession();
        CancelExpressionLoad();
        SetVoiceFreshnessActive(false);
        ClearPresetContextWatchers();
        if (!PersistExpressionSourceModePreference(next))
        {
            PublishExpressionSourceProperties();
            UpdateCommands();
            return false;
        }
        expressionSourceMode = next;
        expressionCacheDirty = true;
        HasError = false; Status = "";
        PublishExpressionSourceProperties();
        if (activeTask == "expression")
        {
            SetVoiceFreshnessActive(true);
            if (UsesRelativeExpressions || IsTachiePresetExpressionSource) RequestExpressionLoad(true);
            else RefreshExpressionSynchronously(false);
        }
        UpdateCommands();
        return true;
    }

    private void PublishExpressionSourceProperties()
    {
        OnPropertyChanged(nameof(IsTemplateExpressionSource));
        OnPropertyChanged(nameof(IsTachiePresetExpressionSource));
        OnPropertyChanged(nameof(ExpressionRowsMatchSource));
        OnPropertyChanged(nameof(ExpressionChoiceColumnTitle));
        OnPropertyChanged(nameof(ExpressionSourceNotice));
        OnPropertyChanged(nameof(CanEditExpressionRows));
        OnPropertyChanged(nameof(ShowExpressionBatchPlace));
        OnPropertyChanged(nameof(ResyncHint));
        NavigateExpressionRowCommand?.RaiseCanExecuteChanged();
        AddExpressionTemplateCommand?.RaiseCanExecuteChanged();
    }
    private void RequireTemplateExpressionSource()
    {
        if (!IsTemplateExpressionSource)
            throw new InvalidOperationException("この操作はテンプレート表示でのみ利用できます。");
    }
}
