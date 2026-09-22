using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public enum ExpressionLayerMode { VoiceSet, Relative, Absolute }
public sealed record ExpressionLayerModeChoice(ExpressionLayerMode Value, string Label);

public sealed record ExpressionLayerRule
{
    public ExpressionLayerMode Mode { get; init; } = ExpressionLayerMode.VoiceSet;
    public RelativeLayerPolicy Relative { get; init; } = new();
    public LayerPolicy Absolute { get; init; } = new()
    {
        UseTemplateLayer = false,
        Minimum = 0,
        Maximum = 99,
        Preferred = 15,
        SearchMode = LayerSearchMode.DoNotPlace
    };

    public void Validate()
    {
        if (!Enum.IsDefined(Mode) || Relative == null || Absolute == null)
            throw new InvalidOperationException("表情のレイヤー配置設定が不正です。");
        Relative.Validate();
        Absolute.Validate();
        if (Absolute.UseTemplateLayer)
            throw new InvalidOperationException("表情の絶対レイヤー指定ではテンプレートレイヤーを使用できません。");
    }

    public static ExpressionLayerRule FromLegacy(LayerPolicy legacy)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        legacy.Validate();
        return new()
        {
            Mode = ExpressionLayerMode.Absolute,
            Absolute = legacy with { UseTemplateLayer = false }
        };
    }

    public string Describe() => Mode switch
    {
        ExpressionLayerMode.VoiceSet => "Voice用の表情Setと同じ上下配置",
        ExpressionLayerMode.Relative => $"Voiceの{(Relative.Direction == RelativeLayerDirection.Up ? "上" : "下")}へ{Relative.Offset}レイヤー",
        ExpressionLayerMode.Absolute => $"レイヤー {Absolute.Preferred}" + (Absolute.SearchMode switch
        {
            LayerSearchMode.SearchUp => "（使用中なら上を探索）",
            LayerSearchMode.SearchDown => "（使用中なら下を探索）",
            LayerSearchMode.DoNotPlace => "（使用中なら配置しない）",
            _ => "（旧互換の範囲探索）"
        }),
        _ => throw new InvalidOperationException("表情のレイヤー配置設定が不正です。")
    };
}

internal static class ExpressionLayerPlanner
{
    public static int Find(Timeline timeline, VoiceItem voice, int frame, int length,
        ExpressionPreset preset, PlacerSettings settings, IEnumerable<IItem> occupancy)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(voice);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(settings);
        var rule = preset.LayerRule ?? ExpressionLayerRule.FromLegacy(preset.Layer);
        rule.Validate();

        return rule.Mode switch
        {
            ExpressionLayerMode.VoiceSet => FindRelative(frame, length, voice.Layer,
                ResolveVoiceSetLayer(timeline, voice, settings), occupancy),
            ExpressionLayerMode.Relative => FindRelative(frame, length, voice.Layer, rule.Relative, occupancy),
            ExpressionLayerMode.Absolute => LayerPlanner.Find(frame, length, 0, rule.Absolute,
                CharacterLayerMode.Base, voice.Character, occupancy),
            _ => throw new InvalidOperationException("表情のレイヤー配置設定が不正です。")
        };
    }

    private static RelativeLayerPolicy ResolveVoiceSetLayer(Timeline timeline, VoiceItem voice, PlacerSettings settings)
    {
        var context = IntentSelectionContext.ForItems(timeline, [voice]);
        // Expression Set order is already authoritative for the expression candidate list.
        // Inherit the first applicable expression Set, matching the normal list's default Set choice.
        return settings.IntentPalettes.FirstOrDefault(x => x.ExpressionCandidates && x.Target.Matches(context))?.Relation.Layer
            ?? new RelativeLayerPolicy();
    }

    private static int FindRelative(int frame, int length, int voiceLayer,
        RelativeLayerPolicy policy, IEnumerable<IItem> occupancy)
    {
        PlacementMath.ValidateSpan(frame, length);
        policy.Validate();
        var items = occupancy.ToArray();
        bool Free(int layer) => !items.Any(x => x.Layer == layer &&
            PlacementMath.Overlaps(frame, length, x.Frame, x.Length));

        long first = policy.Direction == RelativeLayerDirection.Up
            ? (long)voiceLayer - policy.Offset
            : (long)voiceLayer + policy.Offset;
        var step = policy.Direction == RelativeLayerDirection.Up ? -1 : 1;
        first = step < 0 ? Math.Min(first, policy.Maximum) : Math.Max(first, policy.Minimum);
        for (var layer = first; layer >= policy.Minimum && layer <= policy.Maximum; layer += step)
            if (Free((int)layer)) return (int)layer;

        throw new InvalidOperationException(
            $"Voiceの{(policy.Direction == RelativeLayerDirection.Up ? "上" : "下")}方向の保存済み範囲に空きレイヤーがありません。反対方向へは配置していません。");
    }
}
