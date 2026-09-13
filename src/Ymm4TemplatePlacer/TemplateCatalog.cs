using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

public sealed record FaceTemplate(ItemTemplate Template, TachieFaceItem Face, string Name, string Character);

public static class TemplateCatalog
{
    public static IReadOnlyList<FaceTemplate> Read()
    {
        var result = new List<FaceTemplate>();
        foreach (var template in ItemSettings.Default.Templates)
        {
            var items = template.Items.ToArray();
            if (items.Length == 1 && items[0] is TachieFaceItem face && face.Character != null)
                result.Add(new FaceTemplate(template, face, template.Name, face.CharacterName));
        }
        return result.OrderBy(x => x.Character, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<FaceTemplate> ForVoice(VoiceItem voice, IReadOnlyList<FaceTemplate> catalog) =>
        catalog.Where(x => voice.Character != null && Equals(x.Face.Character, voice.Character)).ToArray();
}

public sealed record VoiceSnapshot(VoiceItem Voice, string Character, int Frame, int Length, string Serif, int Layer)
{
    public static IReadOnlyList<VoiceSnapshot> Capture(Timeline timeline) => timeline.Items.OfType<VoiceItem>()
        .OrderBy(x => x.Frame).ThenBy(x => x.Layer)
        .Select(x => new VoiceSnapshot(x, x.CharacterName, x.Frame, x.Length, x.Serif ?? "", x.Layer)).ToArray();
}
