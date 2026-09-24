using System.Globalization;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;

public sealed class GenericLayerTargetDraft : IntentEditable
{
    private string target;
    private LayerSearchMode? behavior;
    private readonly string initialTarget;
    private readonly LayerSearchMode? initialBehavior;
    public Guid SetId { get; }
    internal LayerPolicy Saved { get; }
    public static IReadOnlyList<IntentOption<LayerSearchMode>> Behaviors { get; } =
        [new(LayerSearchMode.DoNotPlace, "配置しない"), new(LayerSearchMode.SearchUp, "上の空きを探す"), new(LayerSearchMode.SearchDown, "下の空きを探す")];
    public string Target
    {
        get => target;
        set
        {
            if (target == value) return;
            target = value;
            if (behavior == null && !string.IsNullOrWhiteSpace(value)) { behavior = LayerSearchMode.DoNotPlace; Raise(nameof(OccupiedBehavior)); }
            Changed();
        }
    }
    public LayerSearchMode? OccupiedBehavior { get => behavior; set { if (behavior == value) return; behavior = value; Changed(); } }
    public bool HasChanges => target != initialTarget || behavior != initialBehavior;
    public string LegacyNotice => Saved.SearchMode != LayerSearchMode.Legacy ? "" : Saved.UseTemplateLayer
        ? "現在はテンプレートのレイヤー。数値入力→Enterで指定に切り替えます。"
        : "現在は以前の範囲探索。変更する場合は、塞がった時の動作を選んで適用してください。";
    public string BoundsHint => $"保存済みの範囲: {Saved.Minimum}～{Saved.Maximum}。上は小さい番号、下は大きい番号。Enterで適用、Escで戻す。";
    public string Error => !HasChanges ? "" : !int.TryParse(Target, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < Saved.Minimum || n > Saved.Maximum
        ? $"レイヤーは{Saved.Minimum}～{Saved.Maximum}の整数で指定してください。"
        : behavior is not (LayerSearchMode.DoNotPlace or LayerSearchMode.SearchUp or LayerSearchMode.SearchDown) ? "塞がった時の動作を選んでください。" : "";
    public GenericLayerTargetDraft(Guid setId, LayerPolicy saved)
    {
        SetId = setId; Saved = saved;
        initialTarget = target = saved.UseTemplateLayer ? "" : saved.Preferred.ToString(CultureInfo.InvariantCulture);
        initialBehavior = behavior = saved.SearchMode == LayerSearchMode.Legacy ? null : saved.SearchMode;
    }
    private void Changed() { Notify(nameof(Target)); Raise(nameof(OccupiedBehavior)); Raise(nameof(HasChanges)); Raise(nameof(Error)); }
    internal LayerPolicy Build()
    {
        if (!int.TryParse(Target, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ||
            behavior is not (LayerSearchMode.DoNotPlace or LayerSearchMode.SearchUp or LayerSearchMode.SearchDown))
            throw new InvalidOperationException("レイヤー番号と、塞がった時の動作を指定してください。");
        var result = Saved with { UseTemplateLayer = false, Preferred = n, SearchMode = behavior.Value };
        result.Validate(); return result;
    }
}

public sealed partial class PlacerViewModel
{
    public GenericLayerTargetDraft? GenericLayerTarget { get; private set; }
    public string GenericLayerButtonLabel => GenericLayerTarget is { } draft
        ? $"レイヤー {(draft.Saved.UseTemplateLayer ? "元の位置" : draft.Saved.Preferred.ToString(CultureInfo.InvariantCulture))}{(draft.HasChanges ? " *" : "")} ▾" : "レイヤー ▾";
    public ActionCommand ApplyGenericLayerTargetCommand { get; private set; } = null!;
    public ActionCommand ResetGenericLayerTargetCommand { get; private set; } = null!;
    private bool GenericLayerReadyForExecution => GenericLayerTarget?.HasChanges != true;
    private void InitializeGenericLayerTarget()
    {
        ApplyGenericLayerTargetCommand = new(_ => GenericLayerTarget is { HasChanges: true, Error.Length: 0 } &&
            selectedIntentSet is { Generic: not null } set && CanEditIntentSet(set), _ => Guard(ApplyGenericLayerTarget));
        ResetGenericLayerTargetCommand = new(_ => GenericLayerTarget?.HasChanges == true, _ => UpdateGenericLayerTarget(true));
        OnPropertyChanged(nameof(ApplyGenericLayerTargetCommand)); OnPropertyChanged(nameof(ResetGenericLayerTargetCommand));
    }
    private void GenericLayerTargetEdited(object? sender, EventArgs e) => UpdateGenericLayerCommands();
    private void UpdateGenericLayerCommands()
    {
        ApplyGenericLayerTargetCommand?.RaiseCanExecuteChanged(); ResetGenericLayerTargetCommand?.RaiseCanExecuteChanged();
        ExecuteIntentTileCommand?.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(GenericLayerButtonLabel));
    }
    private void UpdateGenericLayerTarget(bool force = false)
    {
        var palette = PlacementContext == PlacementContext.Generic ? selectedIntentSet?.Generic : null;
        if (!force && GenericLayerTarget?.SetId == palette?.Id && GenericLayerTarget?.Saved == palette?.Layer) return;
        if (GenericLayerTarget != null) GenericLayerTarget.Edited -= GenericLayerTargetEdited;
        GenericLayerTarget = palette == null ? null : new(palette.Id, palette.Layer);
        if (GenericLayerTarget != null) GenericLayerTarget.Edited += GenericLayerTargetEdited;
        OnPropertyChanged(nameof(GenericLayerTarget)); UpdateGenericLayerCommands();
    }
    public void ApplyGenericLayerTarget() => ApplyGenericLayerTarget(true);

    internal void ApplyGenericLayerTarget(bool announceSuccess)
    {
        var draft = GenericLayerTarget ?? throw new InvalidOperationException("汎用Setを選んでください。");
        var set = selectedIntentSet;
        if (set?.Generic == null || set.Id != draft.SetId || !CanEditIntentSet(set))
            throw new InvalidOperationException("Setまたは下書きが変わりました。保存・破棄してからレイヤーを変更してください。");
        var palette = settings.Palettes.Single(x => x.Id == draft.SetId && x.Kind == PaletteKind.Style);
        if (palette.Layer != draft.Saved) throw new InvalidOperationException("レイヤー設定が変更されました。指定し直してください。");
        var layer = draft.Build();
        if (layer == palette.Layer) { UpdateGenericLayerTarget(true); return; }
        var next = PlacerSettingsStore.Copy(settings);
        var index = next.Palettes.FindIndex(x => x.Id == palette.Id);
        next.Palettes[index] = next.Palettes[index] with { Layer = layer };
        // Use the same protected store as Set appearance; no Timeline write and no planner here.
        CloseExpressionTrialSession();
        settingsStore.Save(next); settings = next;
        RefreshV04(); RefreshIntentWorkspace(); ResetIntentSettings();
        if (announceSuccess)
        {
            HasError = false;
            Status = $"汎用Setの指定レイヤーを{layer.Preferred}に保存しました。";
        }
        else if (HasError)
        {
            // A corrected wheel step may clear a preceding wheel/apply error, but
            // routine success never replaces the bottom status with per-step noise.
            HasError = false;
            Status = "";
        }
    }
}
