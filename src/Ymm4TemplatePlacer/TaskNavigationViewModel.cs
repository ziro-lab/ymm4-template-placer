using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    private bool isManagingTemplates;
    public bool IsManagingTemplates { get => isManagingTemplates; private set => Set(ref isManagingTemplates, value); }
    public ActionCommand OpenTemplateManagementCommand { get; private set; } = null!;
    public ActionCommand CloseTemplateManagementCommand { get; private set; } = null!;
    private void InitializeTaskNavigation()
    {
        // A nested management visit does not discard an in-progress source-add draft.
        OpenTemplateManagementCommand = new ActionCommand(_ => true, _ => IsManagingTemplates = true);
        CloseTemplateManagementCommand = new ActionCommand(_ => true, _ => { IsManagingTemplates = false; ReturnToTemplateAddition(); });
    }
}
