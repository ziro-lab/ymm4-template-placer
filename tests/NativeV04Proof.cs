using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;
namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyV04(Timeline timeline, UndoRedoManager undo)
    {
        await VerifyDirectTemplateAddition(timeline, undo);
        await VerifyLibrary(timeline);
        await VerifyPalettes(timeline);
        await VerifyPaletteTask(timeline);
        await VerifyQuickDrop(timeline, undo);
        await VerifyExpressionPresets(timeline, undo);
        await VerifyAssociations(timeline, undo);
        await VerifySelectionProfiles(timeline, undo);
        await VerifySelectionRange(timeline, undo);
        await VerifyBoundary(timeline, undo);
        await VerifyFinalUi(timeline);
        await VerifyPresetSelectorRefresh(timeline);
        VerifyFinalAcceptance();
    }
}
