namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    public string QuickDropHint => PaletteLayerDirty ? "レイヤー設定を保存してから配置してください。" :
        CurrentPalette == null ? "パレットを選ぶか、［＋ テンプレートを追加］で始めてください。" :
        SelectedPaletteEntry?.Entry == null ? "パレット内のテンプレートを選んでください。" :
        "再生位置へ追加します。項目の左ダブルクリックも同じ操作です。";
    public string ExpressionPlaceHint => ExpressionPresetDirty ? "配置範囲を保存するか、編集を戻してください。" :
        Rows.Count == 0 ? "YMM4で音声を追加して［シーン更新］してください。" :
        !Rows.Any(x => x.SelectedChoice.Template != null) ? "表情一覧のテンプレート列で表情を選んでください。" :
        "選んだ表情を追加します。未選択の行は何もしません。既存の配置は削除・移動・短縮しません。";
    public string SelectionPlaceHint => SelectionPresetDirty ? "配置条件を保存するか、編集を戻してください。" :
        timeline?.SelectedItems.Count is not > 0 ? "タイムラインで基準にするアイテムを選んでください。" :
        SelectionTemplate == null ? "配置するテンプレートを選んでください。" :
        SelectedSelectionProfile == null ? "［どこに置く？］で配置方法を選んでください。" :
        "最新状態を再検証して追加します。対象を移動・削除・短縮しません。";
}
