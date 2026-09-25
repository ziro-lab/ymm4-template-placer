using System.Text.Json;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private sealed record PendingIntentAmbiguity(
        Guid PaletteId,
        Guid SourceId,
        string SettingsSnapshot,
        string SourceSemanticHash,
        string ResultSignature,
        IntentSelectionContext Context);

    private PendingIntentAmbiguity? pendingIntentAmbiguity;

    private bool AdmitIntentExecutionPlan(
        IntentExecutionPlan plan,
        IntentTileChoice tile,
        Timeline current,
        PlacerSettings currentSettings)
    {
        if (!plan.HasMultipleResults)
        {
            pendingIntentAmbiguity = null;
            return true;
        }

        var settingsSnapshot = JsonSerializer.Serialize(currentSettings);
        var pending = pendingIntentAmbiguity;
        var same = pending != null &&
            pending.PaletteId == plan.PaletteId &&
            pending.SourceId == plan.SourceId &&
            pending.SettingsSnapshot == settingsSnapshot &&
            pending.SourceSemanticHash == plan.SourceSemanticHash &&
            pending.ResultSignature == plan.ResultSignature;

        if (same)
        {
            try { pending!.Context.ValidateCurrent(current); }
            catch (InvalidOperationException) { same = false; }
        }

        if (same)
        {
            pendingIntentAmbiguity = null;
            return true;
        }

        pendingIntentAmbiguity = new(
            plan.PaletteId,
            plan.SourceId,
            settingsSnapshot,
            plan.SourceSemanticHash,
            plan.ResultSignature,
            plan.Context);

        HasError = false;
        Status = $"配置候補が{plan.ResultCount}通りあります。もう一度「{tile.Label}」を押すと全部配置します。不要な方は削除するか、YMM4の「元に戻す」1回で戻せます。";
        return false;
    }

    private void ClearIntentAmbiguityConfirmation()
    {
        pendingIntentAmbiguity = null;
    }
}
