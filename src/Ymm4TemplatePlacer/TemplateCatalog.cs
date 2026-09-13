using System.Globalization;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed record FaceTemplate(ItemTemplate Template, TachieFaceItem Face, string Name, string Character);

public static class TemplateCatalog
{
    public static string CharacterName(object? character) => Convert.ToString(character, CultureInfo.InvariantCulture) ?? "";

    public static IReadOnlyList<FaceTemplate> Read()
    {
        var result = new List<FaceTemplate>();
        foreach (var template in ItemSettings.Default.Templates)
        {
            var items = template.Items.ToArray();
            if (items.Length == 1 && items[0] is TachieFaceItem face)
                result.Add(new FaceTemplate(template, face, template.Name, CharacterName(face.Character)));
        }
        return result.OrderBy(x => x.Character, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<FaceTemplate> ForVoice(VoiceItem voice, IReadOnlyList<FaceTemplate> catalog) =>
        catalog.Where(x => Equals(x.Face.Character, voice.Character)).ToArray();
}

public sealed record VoiceSnapshot(VoiceItem Voice, string Character, int Frame, int Length, string Serif)
{
    public static IReadOnlyList<VoiceSnapshot> Capture(Timeline timeline) => timeline.Items.OfType<VoiceItem>()
        .OrderBy(x => x.Frame).ThenBy(x => x.Layer)
        .Select(x => new VoiceSnapshot(x, TemplateCatalog.CharacterName(x.Character), x.Frame, x.Length, x.Serif ?? ""))
        .ToArray();
}
