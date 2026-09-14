using System.IO;

namespace Ymm4TemplatePlacer;

public enum SelectionProfile { TargetCompanion, PointEmphasis, SelectionRange, Boundary }
public sealed record SelectionProfileChoice(SelectionProfile Value, string Label);
public sealed record AnchorChoice(int Value, string Label);

public sealed record SelectionPreset(Guid Id, string Name, SelectionProfile Profile,
    int StartOffset, int EndOffset, int AnchorPercent, int Duration, LayerPolicy Layer)
{
    public int HeadPadding { get; init; }
    public int TailPadding { get; init; }
    public int Tolerance { get; init; }
    public static SelectionPreset Companion { get; } = new(Guid.Parse("42da693e-9b63-470c-bf7b-af47bf0c3a01"),
        "対象と同じ範囲", SelectionProfile.TargetCompanion, 0, 0, 0, 30, new());
    public static SelectionPreset Emphasis { get; } = new(Guid.Parse("42da693e-9b63-470c-bf7b-af47bf0c3a02"),
        "中間を30フレーム強調", SelectionProfile.PointEmphasis, 0, 0, 50, 30, new());
    public static SelectionPreset Range { get; } = new(Guid.Parse("42da693e-9b63-470c-bf7b-af47bf0c3a03"),
        "選択範囲を覆う", SelectionProfile.SelectionRange, 0, 0, 0, 30, new());
    public static SelectionPreset Cut { get; } = new(Guid.Parse("42da693e-9b63-470c-bf7b-af47bf0c3a04"),
        "境界の前後15フレーム", SelectionProfile.Boundary, -15, 0, 0, 30, new());
    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length > 128 || !Enum.IsDefined(Profile) ||
            AnchorPercent is not (0 or 25 or 50 or 75 or 100) || Duration < 1 || HeadPadding < 0 || TailPadding < 0 || Tolerance < 0 || Layer == null)
            throw new InvalidOperationException("選択配置プリセットが不正です。長さは1フレーム以上、前後の余白・境界の許容差は0以上にしてください。");
        Layer.Validate();
    }
}

public sealed partial class PlacerSettings
{
    // Absent in older settings: zero. Upgrade changes only the in-memory copy until an explicit save.
    public int SelectionPresetRevision { get; set; }
    public List<SelectionPreset> SelectionPresets { get; set; } = [SelectionPreset.Companion, SelectionPreset.Emphasis, SelectionPreset.Range, SelectionPreset.Cut];
    public Guid CurrentSelectionPresetId { get; set; } = SelectionPreset.Companion.Id;
}

public static class SelectionPresetSettings
{
    public static void Upgrade(PlacerSettings settings)
    {
        if (settings.SelectionPresets == null || settings.SelectionPresets.Any(x => x == null)) return;
        if (settings.SelectionPresetRevision == 0)
        {
            if (!settings.SelectionPresets.Any(x => x.Profile == SelectionProfile.SelectionRange)) settings.SelectionPresets.Add(SelectionPreset.Range);
            settings.SelectionPresetRevision = 1;
        }
        if (settings.SelectionPresetRevision == 1)
        {
            if (!settings.SelectionPresets.Any(x => x.Profile == SelectionProfile.Boundary)) settings.SelectionPresets.Add(SelectionPreset.Cut);
            settings.SelectionPresetRevision = 2;
        }
    }
    public static void Validate(PlacerSettings settings)
    {
        if (settings.SelectionPresetRevision is < 0 or > 2 || settings.SelectionPresets == null || settings.SelectionPresets.Count is < 4 or > 128 ||
            settings.SelectionPresets.Any(x => x == null) ||
            settings.SelectionPresets.Select(x => x.Id).Distinct().Count() != settings.SelectionPresets.Count ||
            !settings.SelectionPresets.Any(x => x.Id == settings.CurrentSelectionPresetId))
            throw new InvalidDataException("選択配置プリセットの版・一覧・ID・選択状態が不正です。元の設定は保持しています。");
        foreach (var preset in settings.SelectionPresets) preset.Validate();
        foreach (var profile in Enum.GetValues<SelectionProfile>())
            if (!settings.SelectionPresets.Any(x => x.Profile == profile))
                throw new InvalidDataException("各配置方法には少なくとも1つのプリセットが必要です。");
    }
}
