using System.Globalization;
using System.IO;
using System.Text;
using YukkuriMovieMaker.Plugin;

namespace Ymm4TemplatePlacer;

public sealed class PluginEntry : ILocalizePlugin
{
    public string Name => "YMM4 Template Placer";
    public void SetCulture(CultureInfo cultureInfo)
    {
        var marker = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CI_MARKER");
        if (!string.IsNullOrWhiteSpace(marker))
        {
            var path = Path.GetFullPath(marker);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"YMM4 Template Placer\nculture={cultureInfo.Name}\nassembly={typeof(PluginEntry).Assembly.FullName}\n", new UTF8Encoding(false));
        }
#if YMM4_PROOF
        NativeProof.Schedule();
#endif
    }
}
