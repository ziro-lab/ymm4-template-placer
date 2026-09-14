using System.IO;

namespace Ymm4TemplatePlacer;

public enum SelectionProfile { TargetCompanion, PointEmphasis }
public sealed record SelectionProfileChoice(SelectionProfile Value, string Label);
public sealed record AnchorChoice(int Value, string Label);

public sealed record SelectionPreset(Guid Id, string Name, SelectionProfile Profile,
    int StartOffset, int EndOffset, int AnchorPercent, int Duration, LayerPolicy Layer)
{
    public static SelectionPreset Companion { get; } = new(Guid.Parse("42da693e-9b63-470c-bf7b-af47bf0c3a01"),
        "対象と同じ範囲", SelectionProfile.TargetCompanion, 0, 0, 0, 30, new());
    public static SelectionPreset Emphasis { get; } = new(Guid.Parse("42da693e-9b63-470c-bf7b-af47bf0c3a02"),
        "中間を30frame強調", SelectionProfile.PointEmphasis, 0, 0, 50, 30, new());
    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length > 128 || !Enum.IsDefined(Profile) ||
            AnchorPercent is not (0 or 25 or 50 or 75 or 100) || Duration < 1 || Layer == null)
            throw new InvalidOperationException("選択配置Presetの名前・Profile・基準点・長さが不正です。長さは1frame以上にしてください。");
        Layer.Validate();
    }
}

public sealed partial class PlacerSettings
{
    public List<SelectionPreset> SelectionPresets { get; set; } = [SelectionPreset.Companion, SelectionPreset.Emphasis];
    public Guid CurrentSelectionPresetId { get; set; } = SelectionPreset.Companion.Id;
}

public static class SelectionPresetSettings
{
    public static void Validate(PlacerSettings settings)
    {
        if (settings.SelectionPresets == null || settings.SelectionPresets.Count is < 2 or > 128 ||
            settings.SelectionPresets.Any(x => x == null) ||
            settings.SelectionPresets.Select(x => x.Id).Distinct().Count() != settings.SelectionPresets.Count ||
            !settings.SelectionPresets.Any(x => x.Id == settings.CurrentSelectionPresetId))
            throw new InvalidDataException("選択配置Presetの一覧・ID・選択状態が不正です。元の設定は保持しています。");
        foreach (var preset in settings.SelectionPresets) preset.Validate();
        foreach (var profile in Enum.GetValues<SelectionProfile>())
            if (!settings.SelectionPresets.Any(x => x.Profile == profile))
                throw new InvalidDataException("各選択配置Profileには少なくとも1つのPresetが必要です。");
    }
}
