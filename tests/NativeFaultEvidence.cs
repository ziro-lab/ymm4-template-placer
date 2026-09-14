using System.IO;
using System.Windows;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static bool nativeFaultOccurred;
    static NativeProof()
    {
        // Evidence only; never mark an exception handled or let a failed run continue as PASS.
        Application.Current.DispatcherUnhandledException += (_, e) => RecordNativeFault("dispatcher", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => RecordNativeFault("domain", e.ExceptionObject);
    }
    private static void RecordNativeFault(string origin, object exception)
    {
        nativeFaultOccurred = true;
        if (string.IsNullOrEmpty(output)) return;
        try
        {
            var report = "FAIL " + stage + " / " + origin + "\n" + exception;
            File.WriteAllText(Path.Combine(output, "native-unhandled.txt"), report);
            File.WriteAllText(Path.Combine(output, "proof-result.txt"), report);
        }
        catch (IOException) { /* Never replace the original native exception with an evidence I/O failure. */ }
    }
}
