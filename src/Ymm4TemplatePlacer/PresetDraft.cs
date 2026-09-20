using System.Globalization;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

/// <summary>UI text is a draft. Only a validated saved preset can drive placement.</summary>
public sealed class PresetDraft : Bindable
{
    private string name = "", maxGap = "90", startOffset = "0", endOffset = "0";
    private string minimum = "0", maximum = "99", preferred = "15";
    private ExpressionDuration duration;
    private bool useTemplateLayer = true;
    public string Name { get => name; set => Set(ref name, value); }
    public ExpressionDuration Duration { get => duration; set => Set(ref duration, value); }
    public string MaxGap { get => maxGap; set => Set(ref maxGap, value); }
    public string StartOffset { get => startOffset; set => Set(ref startOffset, value); }
    public string EndOffset { get => endOffset; set => Set(ref endOffset, value); }
    public bool UseTemplateLayer { get => useTemplateLayer; set => Set(ref useTemplateLayer, value); }
    public string Minimum { get => minimum; set => Set(ref minimum, value); }
    public string Maximum { get => maximum; set => Set(ref maximum, value); }
    public string Preferred { get => preferred; set => Set(ref preferred, value); }

    private static int Number(string value, string label) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result : throw new InvalidOperationException(label + "は整数で入力してください。");
    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    public ExpressionPreset Read(Guid id)
    {
        var preset = new ExpressionPreset(id, Name.Trim(), Duration, Number(MaxGap, "最大間隔"),
            Number(StartOffset, "開始位置の調整"), Number(EndOffset, "終了位置の調整"),
            new LayerPolicy { UseTemplateLayer = UseTemplateLayer, Minimum = Number(Minimum, "最小レイヤー"),
                Maximum = Number(Maximum, "最大レイヤー"), Preferred = Number(Preferred, "優先レイヤー") });
        preset.Validate();
        return preset;
    }
    public bool Matches(ExpressionPreset preset)
    {
        try { return Read(preset.Id) == preset; }
        catch (InvalidOperationException) { return false; }
    }
    public void Load(ExpressionPreset preset)
    {
        Name = preset.Name; Duration = preset.Duration;
        MaxGap = Text(preset.MaxGap); StartOffset = Text(preset.StartOffset); EndOffset = Text(preset.EndOffset);
        UseTemplateLayer = preset.Layer.UseTemplateLayer;
        Minimum = Text(preset.Layer.Minimum); Maximum = Text(preset.Layer.Maximum); Preferred = Text(preset.Layer.Preferred);
    }
}
