namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    public string SelectionPresetNotice => SelectionPresetDirty
        ? "配置条件は未保存です。保存するか［編集を戻す］を押してください。"
        : "";
}
