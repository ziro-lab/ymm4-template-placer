using System.IO;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPanelQuickSettingsLifecycle()
    {
        stage = "AUDIT B02 panel quick settings disposal lifecycle";
        var primary = ViewModel!;
        var path = PlacerSettingsStore.DefaultPath;
        var before = File.Exists(path) ? File.ReadAllBytes(path) : null;
        PlacerViewModel? secondary = null;
        PalettePresentationDraft? detachedDraft = null;

        try
        {
            secondary = new PlacerViewModel();
            secondary.BeginPanelQuickSettings();
            detachedDraft = secondary.PanelQuickPresentation ??
                throw new InvalidOperationException("B02 fixture could not open presentation quick settings.");

            var current = int.Parse(detachedDraft.FixedColumns, System.Globalization.CultureInfo.InvariantCulture);
            detachedDraft.FixedColumns = (current == 16 ? 15 : current + 1)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);

            secondary.Dispose();
            ViewModel = primary;
            await Idle();

            var afterDispose = File.Exists(path) ? File.ReadAllBytes(path) : null;
            Assert((before == null && afterDispose == null) ||
                (before != null && afterDispose != null && before.SequenceEqual(afterDispose)),
                "AUDIT_B02 queued presentation quick save is cancelled by root Dispose before Dispatcher execution");

            detachedDraft.FixedColumns = (current == 1 ? 2 : 1)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            await Idle();

            var afterDetachedEdit = File.Exists(path) ? File.ReadAllBytes(path) : null;
            Assert((before == null && afterDetachedEdit == null) ||
                (before != null && afterDetachedEdit != null && before.SequenceEqual(afterDetachedEdit)),
                "AUDIT_B02 disposed root detaches presentation quick Draft edits and cannot schedule a later save/session rebuild");

            Log("AUDIT_B02=PASS");
        }
        finally
        {
            if (secondary != null)
            {
                try { secondary.Dispose(); } catch { }
            }
            ViewModel = primary;
            if (before == null)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            else if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(before))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, before);
            }
        }
    }
}
