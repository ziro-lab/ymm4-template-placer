namespace Ymm4TemplatePlacer;

internal enum ExpressionSourceMode
{
    Template,
    TachiePreset
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
    public string ExpressionSourceNotice => IsTachiePresetExpressionSource
        ? "立ち絵プリセットの候補表示は準備中です。切り替えだけではタイムラインや設定を変更しません。"
        : "";

    private bool HasProtectedExpressionSourceWork() =>
        HasProtectedPendingVoiceWork() || (!UsesRelativeExpressions && SelectedExpressionCount > 0);

    private bool TrySetExpressionSourceMode(ExpressionSourceMode next)
    {
        if (expressionSourceMode == next) return true;

        if (next == ExpressionSourceMode.TachiePreset && HasProtectedExpressionSourceWork())
        {
            HasError = false;
            Status = "未配置のテンプレート / Excel割り当てが残っています。配置するか一覧を読み直してから、立ち絵プリセットへ切り替えてください。";
            PublishExpressionSourceProperties();
            return false;
        }

        CancelExpressionNavigation();
        CloseExpressionTrialSession();
        CancelExpressionLoad();
        SetVoiceFreshnessActive(false);

        expressionSourceMode = next;
        expressionCacheDirty = true;
        HasError = false;
        Status = "";
        PublishExpressionSourceProperties();

        if (activeTask == "expression" && IsTemplateExpressionSource)
        {
            SetVoiceFreshnessActive(true);
            if (UsesRelativeExpressions) RequestExpressionLoad(false, false);
            else RefreshExpressionSynchronously(false);
        }

        UpdateCommands();
        return true;
    }

    private void PublishExpressionSourceProperties()
    {
        OnPropertyChanged(nameof(IsTemplateExpressionSource));
        OnPropertyChanged(nameof(IsTachiePresetExpressionSource));
        OnPropertyChanged(nameof(ExpressionSourceNotice));
        OnPropertyChanged(nameof(CanEditExpressionRows));
        OnPropertyChanged(nameof(ShowExpressionBatchPlace));
        OnPropertyChanged(nameof(ResyncHint));
    }

    private void RequireTemplateExpressionSource()
    {
        if (!IsTemplateExpressionSource)
            throw new InvalidOperationException("この操作はテンプレート表示でのみ利用できます。");
    }
}
