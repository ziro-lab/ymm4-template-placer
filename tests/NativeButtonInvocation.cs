using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
namespace Ymm4TemplatePlacer;
internal static class NativeButtonInvocation
{
    // Exercise the native WPF command path rather than calling the product method directly.
    internal static void Invoke(this ButtonAutomationPeer peer)
    {
        var provider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider
            ?? throw new InvalidOperationException("The native button does not expose its Invoke pattern.");
        provider.Invoke();
    }
}
