using System.Globalization;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed class SelectionPresetDraft : Bindable
{
    private string name = "", startOffset = "0", endOffset = "0", duration = "30";
    private string minimum = "0", maximum = "99", preferred = "15";
    private int anchorPercent;
    private bool useTemplateLayer = true;
    public string Name { get => name; set => Set(ref name, value); }
    public string StartOffset { get => startOffset; set => Set(ref startOffset, value); }
    public string EndOffset { get => endOffset; set => Set(ref endOffset, value); }
    public string Duration { get => duration; set => Set(ref duration, value); }
    public int AnchorPercent { get => anchorPercent; set => Set(ref anchorPercent, value); }
    public bool UseTemplateLayer { get => useTemplateLayer; set => Set(ref useTemplateLayer, value); }
    public string Minimum { get => minimum; set => Set(ref minimum, value); }
    public string Maximum { get => maximum; set => Set(ref maximum, value); }
    public string Preferred { get => preferred; set => Set(ref preferred, value); }
    private static int Number(string text, string label) => int.TryParse(text, NumberStyles.Integer,
        CultureInfo.InvariantCulture, out var value) ? value : throw new InvalidOperationException(label + "は整数で入力してください。");
    public SelectionPreset Read(SelectionPreset current)
    {
        var result = current with { Name = Name.Trim(), StartOffset = Number(StartOffset, "開始offset"),
            EndOffset = Number(EndOffset, "終了offset"), Duration = Number(Duration, "固定長"), AnchorPercent = AnchorPercent,
            Layer = new LayerPolicy { UseTemplateLayer = UseTemplateLayer, Minimum = Number(Minimum, "Layer最小"),
                Maximum = Number(Maximum, "Layer最大"), Preferred = Number(Preferred, "優先Layer") } };
        result.Validate(); return result;
    }
    public bool Matches(SelectionPreset current)
    {
        try { return Read(current) == current; } catch (InvalidOperationException) { return false; }
    }
    public void Load(SelectionPreset preset)
    {
        Name = preset.Name; StartOffset = Text(preset.StartOffset); EndOffset = Text(preset.EndOffset);
        Duration = Text(preset.Duration); AnchorPercent = preset.AnchorPercent; UseTemplateLayer = preset.Layer.UseTemplateLayer;
        Minimum = Text(preset.Layer.Minimum); Maximum = Text(preset.Layer.Maximum); Preferred = Text(preset.Layer.Preferred);
    }
    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
