using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;
namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyV04(Timeline timeline, UndoRedoManager undo)
    {
        await VerifyLibrary(timeline);
        await VerifyPalettes(timeline);
    }
}
