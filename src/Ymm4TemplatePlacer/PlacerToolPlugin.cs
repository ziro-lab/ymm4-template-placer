using YukkuriMovieMaker.Plugin;

namespace Ymm4TemplatePlacer;

public sealed class PlacerToolPlugin : IToolPlugin
{
    public string Name => "YMM4 Template Placer";
    public Type ViewModelType => typeof(PlacerViewModel);
    public Type ViewType => typeof(PlacerView);
    public bool AllowMultipleInstances => false;
}
