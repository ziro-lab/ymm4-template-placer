using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    private bool isManagingTemplates, keepPartialStatus;
    private string activeTask = "";
    public bool IsManagingTemplates { get => isManagingTemplates; private set => Set(ref isManagingTemplates, value); }
    public ActionCommand OpenTemplateManagementCommand { get; private set; } = null!;
    public ActionCommand CloseTemplateManagementCommand { get; private set; } = null!;
    private void InitializeTaskNavigation()
    {
        OpenTemplateManagementCommand = new ActionCommand(_ => true, _ => IsManagingTemplates = true);
        CloseTemplateManagementCommand = new ActionCommand(_ => true, _ => { IsManagingTemplates = false; ReturnToTemplateAddition(); });
    }
    public void SetActiveTask(string task)
    {
        var changed = activeTask != task;
        if (changed)
        {
            if (activeTask == "intent-settings") FinishSettingsSession();
            if (task != "expression") { CancelExpressionNavigation(); CloseExpressionTrialSession(); }
            activeTask = task;
            if (!HasError && !keepPartialStatus) Status = "";
        }
        if (changed && task == "intent-settings") { BeginIntentSettings(); RequestSettingsAutoCommit(); }
        SetSelectionPreviewActive(task == "selection");
        SetVoiceFreshnessActive(task.Length > 0);
        if (changed && task == "expression") RequestVoiceFreshnessCheck();
    }
}
