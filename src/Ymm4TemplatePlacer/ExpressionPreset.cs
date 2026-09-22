using System.IO;

namespace Ymm4TemplatePlacer;

public enum ExpressionDuration { VoiceSpan, NextSameCharacter }
public sealed record ExpressionDurationChoice(ExpressionDuration Value, string Label);

public sealed record ExpressionPreset(Guid Id, string Name, ExpressionDuration Duration,
    int MaxGap, int StartOffset, int EndOffset, LayerPolicy Layer)
{
    public ExpressionLayerRule? LayerRule { get; init; }

    public static ExpressionPreset Default { get; } = new(
        Guid.Parse("62677098-74d1-4b55-8667-62b3f7bd81d1"), "音声と同じ",
        ExpressionDuration.VoiceSpan, 90, 0, 0, new LayerPolicy())
    {
        LayerRule = new ExpressionLayerRule()
    };

    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length > 128 ||
            !Enum.IsDefined(Duration) || MaxGap < 0 || Layer == null)
            throw new InvalidOperationException("表情プリセットの名前・期間・最大間隔が不正です。最大間隔は0以上にしてください。");
        Layer.Validate();
        LayerRule?.Validate();
    }

    public string Describe() =>
        (Duration == ExpressionDuration.VoiceSpan ? "音声と同じ" : $"次の同じキャラクターの音声まで（最大間隔 {MaxGap}）") +
        $" ／ 開始 {StartOffset:+0;-0;0}・終了 {EndOffset:+0;-0;0}フレーム ／ " +
        (LayerRule?.Describe() ?? (Layer.UseTemplateLayer ? "旧設定: テンプレートのレイヤー" : $"旧設定: レイヤー {Layer.Minimum}〜{Layer.Maximum}・優先 {Layer.Preferred}"));
}

public sealed partial class PlacerSettings
{
    public List<ExpressionPreset> ExpressionPresets { get; set; } = [ExpressionPreset.Default];
    public Guid CurrentExpressionPresetId { get; set; } = ExpressionPreset.Default.Id;
}

public static class ExpressionPresetSettings
{
    public static void Upgrade(PlacerSettings settings)
    {
        if (settings.ExpressionPresets == null) return;
        var historicalDefaultLayer = new LayerPolicy();
        for (var i = 0; i < settings.ExpressionPresets.Count; i++)
        {
            var preset = settings.ExpressionPresets[i];
            if (preset?.LayerRule != null) continue;
            var untouchedHistoricalDefault =
                preset!.Id == ExpressionPreset.Default.Id &&
                preset.Name == "音声と同じ" &&
                preset.Duration == ExpressionDuration.VoiceSpan &&
                preset.MaxGap == 90 && preset.StartOffset == 0 && preset.EndOffset == 0 &&
                preset.Layer == historicalDefaultLayer;
            settings.ExpressionPresets[i] = preset with
            {
                LayerRule = untouchedHistoricalDefault
                    ? new ExpressionLayerRule()
                    : ExpressionLayerRule.FromLegacy(preset.Layer)
            };
        }
    }

    public static void Validate(PlacerSettings settings)
    {
        if (settings.ExpressionPresets == null || settings.ExpressionPresets.Count is < 1 or > 128 ||
            settings.ExpressionPresets.Any(x => x == null) ||
            settings.ExpressionPresets.Select(x => x.Id).Distinct().Count() != settings.ExpressionPresets.Count ||
            !settings.ExpressionPresets.Any(x => x.Id == settings.CurrentExpressionPresetId))
            throw new InvalidDataException("表情プリセットの一覧・ID・選択状態が不正です。元の設定は保持しています。");
        foreach (var preset in settings.ExpressionPresets) preset.Validate();
    }
}
