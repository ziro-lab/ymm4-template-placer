using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using YukkuriMovieMaker.Plugin;

namespace Ymm4TemplatePlacer;

public sealed class PlacerToolPlugin : IToolPlugin
{
    public PlacerToolPlugin()
    {
        var marker = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CI_MARKER");
        if (!string.IsNullOrWhiteSpace(marker))
        {
#if YMM4_PROOF
            const string build = "proof";
#else
            const string build = "distribution";
#endif
            var assembly = typeof(PlacerToolPlugin).Assembly;
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))).ToLowerInvariant();
            var path = Path.GetFullPath(marker);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"YMM4 Template Placer\nculture={CultureInfo.CurrentUICulture.Name}\nassembly={assembly.FullName}\nbuild={build}\nsha256={hash}\n", new UTF8Encoding(false));
        }
#if YMM4_PROOF
        NativeProof.Schedule();
#endif
    }

    public string Name => "Template Placer";
    public Type ViewModelType => typeof(PlacerViewModel);
    public Type ViewType => typeof(PlacerView);
    public bool AllowMultipleInstances => false;
    public string DefaultGroupName => YukkuriMovieMaker.Resources.Localization.Texts.ToolGroupUtilityName;
    public int DefaultOrder => 550;
}
