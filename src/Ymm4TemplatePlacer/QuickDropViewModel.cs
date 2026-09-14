using System.Globalization;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private Guid? layerEditorPaletteId;
    private LayerPolicy? layerEditorPolicy;
    private bool loadingLayerEditor, paletteUseTemplateLayer = true;
    private string paletteMinimumText = "0", paletteMaximumText = "99", palettePreferredText = "15";
    public IReadOnlyList<LayerModeChoice> QuickDropModes { get; } = [new(CharacterLayerMode.Base, "基準"), new(CharacterLayerMode.Front, "前面"), new(CharacterLayerMode.Back, "背面")];
    public CharacterLayerMode QuickDropMode
    {
        get => settings.CharacterQuickDropMode;
        set
        {
            if (value == settings.CharacterQuickDropMode) return;
            Guard(() => EditSettings(next => next.CharacterQuickDropMode = value));
        }
    }
    public bool PaletteUseTemplateLayer { get => paletteUseTemplateLayer; set { Set(ref paletteUseTemplateLayer, value); LayerEditorChanged(); } }
    public string PaletteMinimumText { get => paletteMinimumText; set { Set(ref paletteMinimumText, value); LayerEditorChanged(); } }
    public string PaletteMaximumText { get => paletteMaximumText; set { Set(ref paletteMaximumText, value); LayerEditorChanged(); } }
    public string PalettePreferredText { get => palettePreferredText; set { Set(ref palettePreferredText, value); LayerEditorChanged(); } }
    public bool PaletteLayerDirty => CurrentPalette != null && (layerEditorPolicy == null ||
        PaletteUseTemplateLayer != layerEditorPolicy.UseTemplateLayer || PaletteMinimumText != layerEditorPolicy.Minimum.ToString(CultureInfo.InvariantCulture) ||
        PaletteMaximumText != layerEditorPolicy.Maximum.ToString(CultureInfo.InvariantCulture) || PalettePreferredText != layerEditorPolicy.Preferred.ToString(CultureInfo.InvariantCulture));
    public string PaletteLayerSummary => CurrentPalette == null ? "" : CurrentPalette.Layer.UseTemplateLayer ? "基準：テンプレートのレイヤーを使用" : $"基準：レイヤー {CurrentPalette.Layer.Minimum}〜{CurrentPalette.Layer.Maximum} ／ 優先 {CurrentPalette.Layer.Preferred}";
    public string PaletteLayerNotice => PaletteLayerDirty ? "レイヤー設定は未保存です。保存するまで配置しません。" : "";
    public ActionCommand QuickDropCommand { get; private set; } = null!;
    public ActionCommand SavePaletteLayerCommand { get; private set; } = null!;
    partial void InitializeQuickDrop()
    {
        QuickDropCommand = new ActionCommand(_ => timeline != null && undo != null && CurrentPalette != null && SelectedPaletteEntry?.Entry != null && !PaletteLayerDirty, _ => Guard(() => QuickDrop()));
        SavePaletteLayerCommand = new ActionCommand(_ => settingsAvailable && CurrentPalette != null, _ => Guard(SavePaletteLayer));
        UpdateQuickDropCommands();
    }
    private static int ReadFrameNumber(string text, string label)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw new InvalidOperationException(label + "は整数で入力してください。");
        return value;
    }
    private void LayerEditorChanged()
    {
        if (loadingLayerEditor) return;
        OnPropertyChanged(nameof(PaletteLayerDirty)); OnPropertyChanged(nameof(PaletteLayerNotice)); OnPropertyChanged(nameof(QuickDropHint));
        QuickDropCommand?.RaiseCanExecuteChanged();
    }
    partial void UpdateQuickDropCommands()
    {
        var palette = CurrentPalette;
        if (palette?.Id != layerEditorPaletteId || palette?.Layer != layerEditorPolicy)
        {
            loadingLayerEditor = true;
            try
            {
                layerEditorPaletteId = palette?.Id; layerEditorPolicy = palette?.Layer;
                var policy = palette?.Layer ?? new LayerPolicy();
                PaletteUseTemplateLayer = policy.UseTemplateLayer;
                PaletteMinimumText = policy.Minimum.ToString(CultureInfo.InvariantCulture);
                PaletteMaximumText = policy.Maximum.ToString(CultureInfo.InvariantCulture);
                PalettePreferredText = policy.Preferred.ToString(CultureInfo.InvariantCulture);
            }
            finally { loadingLayerEditor = false; }
        }
        OnPropertyChanged(nameof(QuickDropMode)); OnPropertyChanged(nameof(PaletteLayerSummary));
        OnPropertyChanged(nameof(PaletteLayerDirty)); OnPropertyChanged(nameof(PaletteLayerNotice)); OnPropertyChanged(nameof(QuickDropHint));
        QuickDropCommand?.RaiseCanExecuteChanged(); SavePaletteLayerCommand?.RaiseCanExecuteChanged();
    }
    public void SavePaletteLayer()
    {
        var palette = CurrentPalette ?? throw new InvalidOperationException("パレットを選んでください。");
        var policy = new LayerPolicy { UseTemplateLayer = PaletteUseTemplateLayer,
            Minimum = ReadFrameNumber(PaletteMinimumText, "最小レイヤー"), Maximum = ReadFrameNumber(PaletteMaximumText, "最大レイヤー"), Preferred = ReadFrameNumber(PalettePreferredText, "優先レイヤー") };
        policy.Validate();
        EditSettings(next => { var index = next.Palettes.FindIndex(x => x.Id == palette.Id); next.Palettes[index] = next.Palettes[index] with { Layer = policy }; });
        // A successful save canonicalizes 015 to 15 even if the saved policy record is equal.
        layerEditorPolicy = null;
        UpdateQuickDropCommands();
        HasError = false; Status = "このパレットのレイヤー設定を保存しました。前面・背面も、保存した探索範囲内だけで配置します。";
    }
    public IItem QuickDrop()
    {
        if (PaletteLayerDirty) throw new InvalidOperationException("編集したレイヤー設定を保存してから配置してください。");
        var current = RequireTimeline();
        var palette = CurrentPalette ?? throw new InvalidOperationException("パレットを選んでください。");
        var id = SelectedPaletteEntry?.LibraryEntryId ?? throw new InvalidOperationException("配置するパレットのテンプレートを選んでください。");
        var entry = settings.Library.SingleOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("登録済みテンプレートが見つかりません。テンプレート管理を確認してください。");
        if (!palette.LibraryEntryIds.Contains(id)) throw new InvalidOperationException("このテンプレートは現在のパレットに含まれていません。選び直してください。");
        if (undo == null) throw new InvalidOperationException("YMM4の「元に戻す」に接続できません。ツールを開き直してください。");
        var planned = QuickDropPlanner.Create(current, entry, palette, QuickDropMode);
        planned.Plan.Commit(current, undo);
        HasError = false; Status = $"「{entry.DisplayName}」を 開始 {planned.Item.Frame} / レイヤー {planned.Item.Layer} に配置しました（長さ {planned.Item.Length}）。";
        return planned.Item;
    }
}
