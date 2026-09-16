using System.Reflection;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;
namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyV04(Timeline timeline, UndoRedoManager undo)
    {
        var settingsField = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var bootstrapped = (PlacerSettings)settingsField.GetValue(ViewModel!)!;
        Assert(bootstrapped.Palettes.Count == 0 && bootstrapped.Library.All(x => bootstrapped.ImportedExpressionSources.Contains(x.Source)),
            "R1 historical first-use fixture contains only new automatic-bootstrap references before isolation");
        var legacyFixture = PlacerSettingsStore.Copy(bootstrapped);
        legacyFixture.Library.Clear(); legacyFixture.IntentPalettes.Clear(); legacyFixture.ImportedExpressionSources.Clear();
        legacyFixture.ExpressionBootstrapComplete = true; legacyFixture.LegacyWorkspace = true;
        settingsField.SetValue(ViewModel!, legacyFixture);
        ViewModel!.ActivateIntentWorkspace(); ViewModel.SetLegacyWorkspace(true); ViewModel.Refresh();
        await VerifyDirectTemplateAddition(timeline, undo);
        await VerifyLibrary(timeline);
        await VerifyPalettes(timeline);
        await VerifyPaletteTask(timeline);
        await VerifyExpressionRecovery(timeline);
        await VerifyPalettePickerRefresh(timeline);
        await VerifyExpressionTask(timeline);
        await VerifyQuickDrop(timeline, undo);
        await VerifyExpressionPresets(timeline, undo);
        await VerifyAssociations(timeline, undo);
        await VerifySelectionProfiles(timeline, undo);
        await VerifySelectionRange(timeline, undo);
        await VerifyBoundary(timeline, undo);
        await VerifySelectionTask(timeline, undo);
        await VerifyAutomaticPreview(timeline, undo);
        await VerifyFinalUi(timeline);
        await VerifyPresetSelectorRefresh(timeline);
        await VerifyTaskUxFinal(timeline, undo);
        await VerifyResumeContinuity(timeline, undo);
        await VerifyBulkPaletteAdd(timeline);
        await VerifyPaletteOrdering(timeline);
        await VerifySafetyIntent(timeline);
        await VerifyIdentityClarity(timeline);
        VerifyFinalAcceptance(); VerifyTaskUxAcceptance(); VerifyWorkflowAcceptance();
        await VerifyRelativeFoundations(timeline, undo);
        await VerifyIntentCore(timeline, undo);
        await VerifyIntentSurface(timeline, undo);
        await VerifyIntentSettings(timeline, undo);
        await VerifyRelativeExpressions(timeline, undo);
        await VerifyTemplateFidelity(timeline, undo);
        await VerifyRelativeUiUx(timeline, undo);
        await VerifyRelativeFinal(timeline, undo);
        VerifyRelativeAcceptance();
    }
}
