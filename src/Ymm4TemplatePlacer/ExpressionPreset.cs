using System.IO;

namespace Ymm4TemplatePlacer;

public enum ExpressionDuration { VoiceSpan, NextSameCharacter }
public sealed record ExpressionDurationChoice(ExpressionDuration Value, string Label);

public sealed record ExpressionPreset(Guid Id, string Name, ExpressionDuration Duration,
    int MaxGap, int StartOffset, int EndOffset, LayerPolicy Layer)
{
    public static ExpressionPreset Default { get; } = new(
        Guid.Parse("62677098-74d1-4b55-8667-62b3f7bd81d1"), "Voiceと同じ",
        ExpressionDuration.VoiceSpan, 90, 0, 0, new LayerPolicy());

    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length > 128 ||
            !Enum.IsDefined(Duration) || MaxGap < 0 || Layer == null)
            throw new InvalidOperationException("表情Presetの名前・期間・MaxGapが不正です。MaxGapは0以上にしてください。");
        Layer.Validate();
    }

    public string Describe() =>
        (Duration == ExpressionDuration.VoiceSpan ? "Voiceと同じ" : $"次の同Character Voiceまで（MaxGap {MaxGap}）") +
        $" ／ 開始 {StartOffset:+0;-0;0}・終了 {EndOffset:+0;-0;0} frame ／ " +
        (Layer.UseTemplateLayer ? "TemplateのLayer" : $"Layer {Layer.Minimum}〜{Layer.Maximum}・優先 {Layer.Preferred}");
}

public sealed partial class PlacerSettings
{
    public List<ExpressionPreset> ExpressionPresets { get; set; } = [ExpressionPreset.Default];
    public Guid CurrentExpressionPresetId { get; set; } = ExpressionPreset.Default.Id;
}

public static class ExpressionPresetSettings
{
    public static void Validate(PlacerSettings settings)
    {
        if (settings.ExpressionPresets == null || settings.ExpressionPresets.Count is < 1 or > 128 ||
            settings.ExpressionPresets.Any(x => x == null) ||
            settings.ExpressionPresets.Select(x => x.Id).Distinct().Count() != settings.ExpressionPresets.Count ||
            !settings.ExpressionPresets.Any(x => x.Id == settings.CurrentExpressionPresetId))
            throw new InvalidDataException("表情Presetの一覧・ID・選択状態が不正です。元の設定は保持しています。");
        foreach (var preset in settings.ExpressionPresets) preset.Validate();
    }
}
