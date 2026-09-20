using System.Globalization;

namespace Ymm4TemplatePlacer;
public sealed partial class PlacerViewModel
{
    internal void StepGenericLayer(GenericLayerTargetDraft draft, int steps) => Guard(() =>
    {
        if (steps == 0) return;
        if (!ReferenceEquals(draft, GenericLayerTarget) || selectedIntentSet is not { Generic: not null } set ||
            set.Id != draft.SetId || !CanEditIntentSet(set))
            throw new InvalidOperationException("入力途中の設定を確定または戻してから、レイヤーを変更してください。");
        if (!int.TryParse(draft.Target, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
            value < draft.Saved.Minimum || value > draft.Saved.Maximum)
            throw new InvalidOperationException("先に範囲内のレイヤー番号を入力してください。入力途中の値は変更していません。");
        var next = (int)Math.Clamp((long)value + steps, draft.Saved.Minimum, draft.Saved.Maximum);
        if (next == value) return;
        draft.Target = next.ToString(CultureInfo.InvariantCulture);
        ApplyGenericLayerTarget(); // The same protected apply as Enter; no alternate planner/store.
    });
}
