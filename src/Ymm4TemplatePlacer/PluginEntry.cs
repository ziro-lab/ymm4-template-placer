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
        // CI-only probe. Normal YMM4 launches do not create any marker file.
        var markerPath = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CI_MARKER");
        if (string.IsNullOrWhiteSpace(markerPath))
            return;

        var fullPath = Path.GetFullPath(markerPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var body = new StringBuilder()
            .AppendLine("YMM4 Template Placer")
            .Append("culture=").AppendLine(cultureInfo.Name)
            .Append("assembly=").AppendLine(typeof(PluginEntry).Assembly.FullName)
            .Append("base_dir=").AppendLine(AppContext.BaseDirectory)
            .ToString();

        File.WriteAllText(fullPath, body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
