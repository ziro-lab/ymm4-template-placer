using System.Reflection;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;
namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyV04(Timeline timeline, UndoRedoManager undo)
    {
        VerifyPortableSettingsStorage();
        VerifyPlacementSourceModel(timeline);
        VerifyPlacementSourceGeometry(timeline);
        await VerifyPlacementSourcePresetMaterialization(timeline);
        await VerifyPlacementSourceNormalTiles(timeline, undo);
        await VerifyPlacementSourceExpressions(timeline, undo);
        await VerifyPlacementSourceResync(timeline, undo);
        await VerifyPlacementSourceRegistration(timeline, undo);
        var settingsField = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var bootstrapped = (PlacerSettings)settingsField.GetValue(ViewModel!)!;
        Assert(bootstrapped.Palettes.Count == 0 && bootstrapped.Library.All(x => bootstrapped.ImportedExpressionSources.Contains(x.Source)),
            "R1 historical first-use fixture contains only new automatic-bootstrap references before isolation");
        var legacyFixture = PlacerSettingsStore.Copy(bootstrapped);
        legacyFixture.Library.Clear(); legacyFixture.IntentPalettes.Clear(); legacyFixture.ImportedExpressionSources.Clear();
        legacyFixture.ExpressionBootstrapComplete = true;
        settingsField.SetValue(ViewModel!, legacyFixture);
        ViewModel!.ActivateIntentWorkspace(); ViewModel.Refresh();
        await VerifyTachiePresetSourceModeFoundation(timeline, undo);
        await VerifyTachiePresetCapability(timeline, undo);
        await VerifyTachiePresetGuards(timeline);
        await VerifyTachiePresetRowIntegration(timeline, undo);
        await VerifyTachiePresetAssociationSeam(timeline, undo);
        await VerifyTachiePresetMutationPlanning(timeline, undo);
        await VerifyTachiePresetImmediateReplacement(timeline, undo);
        await VerifyTachiePresetFailureUx(timeline, undo);
        await VerifyTachiePresetPerformanceCheckpoint(timeline, undo);
        await VerifyTachiePresetCalibration(timeline, undo);

        var profile = (Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_NATIVE_PROFILE") ?? "checkpoint").ToLowerInvariant();
        if (profile == "focused")
        {
            await VerifyLibrary(timeline);
            await VerifyAssociations(timeline, undo);
                await VerifyIntentCore(timeline, undo);
            await VerifyIntentSettings(timeline, undo);
            await VerifyRelativeExpressions(timeline, undo);
            await VerifyTemplateFidelity(timeline, undo);
            await VerifyHandsOnRound3Layers(timeline, undo);
            await VerifyHandsOnRound3Navigation(timeline, undo);
            await VerifyHandsOnRound4Navigation(timeline, undo);
            await VerifyHandsOnRound4Presentation(timeline, undo);
            VerifyHandsOnRound4SettingsTransaction(timeline);
            VerifyHandsOnRound4SettingsSession(timeline, undo);
            await VerifyHandsOnRound4Settings(timeline, undo);
            await VerifyFinalHandsOnPolish(timeline, undo);
            Log("FOCUSED_NATIVE=PASS");
            return;
        }
        await VerifyLibrary(timeline);
        await VerifyAssociations(timeline, undo);
        await VerifyFinalUi(timeline);
        await VerifySafetyIntent(timeline);
        VerifyFinalAcceptance(); VerifyWorkflowAcceptance();
        await VerifyRelativeFoundations(timeline, undo);
        await VerifyIntentCore(timeline, undo);
        await VerifyIntentSurface(timeline, undo);
        await VerifyIntentSettings(timeline, undo);
        await VerifyRelativeExpressions(timeline, undo);
        await VerifyTemplateFidelity(timeline, undo);
        await VerifyRelativeUiUx(timeline, undo);
        await VerifyHandsOnAppearance(timeline, undo);
        await VerifyHandsOnWorkflow(timeline, undo);
        await VerifyHandsOnRound2Input(timeline, undo);
        await VerifyHandsOnRound2Sets(timeline, undo);
        await VerifyHandsOnRound2Settings(timeline, undo);
        await VerifyHandsOnRound2Tiles(timeline, undo);
        await VerifyHandsOnRound2Expression(timeline, undo);
        await VerifyRelativeFinal(timeline, undo);
        VerifyRelativeAcceptance();
        VerifyHandsOnRound2Final();
        await VerifyHandsOnRound3Appearance(timeline, undo);
        await VerifyHandsOnRound3Shortcuts(timeline, undo);
        await VerifyHandsOnRound3Settings(timeline, undo);
        await VerifyHandsOnRound3Layers(timeline, undo);
        await VerifyHandsOnRound3Freshness(timeline, undo);
        await VerifyHandsOnRound3Navigation(timeline, undo);
        await VerifyHandsOnRound3Final(timeline, undo);
        await VerifyHandsOnRound4Navigation(timeline, undo);
        await VerifyHandsOnRound4Presentation(timeline, undo);
        VerifyHandsOnRound4SettingsTransaction(timeline);
        VerifyHandsOnRound4SettingsSession(timeline, undo);
        await VerifyHandsOnRound4Settings(timeline, undo);
        await VerifyFinalHandsOnPolish(timeline, undo);
        VerifyTaskUxAcceptance();
        await VerifyExpressionPerformance(timeline, undo);
    }
}
