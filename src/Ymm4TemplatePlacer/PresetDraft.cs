using System.Globalization;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

/// <summary>UI text is a draft. Only a validated saved preset can drive placement.</summary>
public sealed class PresetDraft : Bindable
{
    private string name = "", maxGap = "90", startOffset = "0", endOffset = "0";
    private string minimum = "0", maximum = "99", relativeOffset = "1", absoluteLayer = "15";
    private ExpressionDuration duration;
    private ExpressionLayerMode layerMode = ExpressionLayerMode.VoiceSet;
    private RelativeLayerDirection direction = RelativeLayerDirection.Up;
    private LayerSearchMode occupiedBehavior = LayerSearchMode.DoNotPlace;
    private LayerPolicy legacyLayer = new();

    public string Name { get => name; set => Set(ref name, value); }
    public ExpressionDuration Duration { get => duration; set => Set(ref duration, value); }
    public string MaxGap { get => maxGap; set => Set(ref maxGap, value); }
    public string StartOffset { get => startOffset; set => Set(ref startOffset, value); }
    public string EndOffset { get => endOffset; set => Set(ref endOffset, value); }
    public ExpressionLayerMode LayerMode { get => layerMode; set => Set(ref layerMode, value); }
    public RelativeLayerDirection Direction { get => direction; set => Set(ref direction, value); }
    public string RelativeOffset { get => relativeOffset; set => Set(ref relativeOffset, value); }
    public string Minimum { get => minimum; set => Set(ref minimum, value); }
    public string Maximum { get => maximum; set => Set(ref maximum, value); }
    public string AbsoluteLayer { get => absoluteLayer; set => Set(ref absoluteLayer, value); }
    public LayerSearchMode OccupiedBehavior { get => occupiedBehavior; set => Set(ref occupiedBehavior, value); }

    // Compatibility aliases for the retained legacy expression batch path and its native proof.
    // New UI uses LayerMode/AbsoluteLayer directly.
    public bool UseTemplateLayer
    {
        get => LayerMode == ExpressionLayerMode.VoiceSet;
        set
        {
            if (value) LayerMode = ExpressionLayerMode.VoiceSet;
            else if (LayerMode == ExpressionLayerMode.VoiceSet) LayerMode = ExpressionLayerMode.Absolute;
        }
    }
    public string Preferred { get => AbsoluteLayer; set => AbsoluteLayer = value; }

    private static int Number(string value, string label) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result : throw new InvalidOperationException(label + "は整数で入力してください。");
    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    public ExpressionPreset Read(Guid id)
    {
        var relative = LayerMode == ExpressionLayerMode.Relative
            ? new RelativeLayerPolicy
            {
                Direction = Direction,
                Offset = Number(RelativeOffset, "Voiceから離すレイヤー数"),
                Minimum = Number(Minimum, "最小レイヤー"),
                Maximum = Number(Maximum, "最大レイヤー")
            }
            : new RelativeLayerPolicy();
        var absolute = LayerMode == ExpressionLayerMode.Absolute
            ? new LayerPolicy
            {
                UseTemplateLayer = false,
                Minimum = Number(Minimum, "最小レイヤー"),
                Maximum = Number(Maximum, "最大レイヤー"),
                Preferred = Number(AbsoluteLayer, "指定レイヤー"),
                SearchMode = OccupiedBehavior
            }
            : new LayerPolicy
            {
                UseTemplateLayer = false,
                Minimum = 0,
                Maximum = 99,
                Preferred = 15,
                SearchMode = LayerSearchMode.DoNotPlace
            };
        var legacyForCompatibility = LayerMode == ExpressionLayerMode.Absolute ? absolute : legacyLayer;
        var preset = new ExpressionPreset(id, Name.Trim(), Duration, Number(MaxGap, "最大間隔"),
            Number(StartOffset, "開始位置の調整"), Number(EndOffset, "終了位置の調整"), legacyForCompatibility)
        {
            LayerRule = new ExpressionLayerRule
            {
                Mode = LayerMode,
                Relative = relative,
                Absolute = absolute
            }
        };
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
        legacyLayer = preset.Layer;

        var rule = preset.LayerRule ?? ExpressionLayerRule.FromLegacy(preset.Layer);
        LayerMode = rule.Mode;
        Direction = rule.Relative.Direction;
        RelativeOffset = Text(rule.Relative.Offset);
        Minimum = Text(rule.Mode == ExpressionLayerMode.Absolute ? rule.Absolute.Minimum : rule.Relative.Minimum);
        Maximum = Text(rule.Mode == ExpressionLayerMode.Absolute ? rule.Absolute.Maximum : rule.Relative.Maximum);
        AbsoluteLayer = Text(rule.Absolute.Preferred);
        OccupiedBehavior = rule.Absolute.SearchMode;
    }
}
