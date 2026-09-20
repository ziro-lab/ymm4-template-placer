using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Ymm4TemplatePlacer;

// Only public members on known live WPF DataContexts are admitted. No assembly scan,
// private state, input synthesis, focus manipulation or host-version gate.
internal sealed record ExpressionNavigationHost(object? PreviewOwner, Func<int, Task>? SeekAsync,
    Func<int, bool>? ContainFrameInViewport, Action<int>? ScrollFrame)
{
    internal const string PreviewName = "YukkuriMovieMaker.ViewModels.PreviewViewModel";
    internal const string ViewportName = "YukkuriMovieMaker.ViewModels.TimelineViewModel";
    internal static ExpressionNavigationHost Missing { get; } = new(null, null, null, null);
    internal bool CanSeek => SeekAsync != null;
    internal bool CanFollow => ContainFrameInViewport != null && ScrollFrame != null;
    internal object? ViewportOwner { get; init; }
    internal static ExpressionNavigationHost Resolve()
    {
        try
        {
            var app = Application.Current;
            if (app == null) return Missing;
            var main = app.Windows.Cast<Window>().Where(x => x.IsVisible &&
                x.DataContext?.GetType().FullName == "YukkuriMovieMaker.ViewModels.MainViewModel").ToArray();
            if (main.Length != 1) return Missing;
            var previews = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var viewports = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var windows = app.Windows.Cast<Window>().Where(x => x.IsVisible && OwnedBy(x, main[0])).Take(33).ToArray();
            if (windows.Length > 32) return Missing;
            var pending = new Stack<(DependencyObject Node, int Depth)>();
            foreach (var window in windows) pending.Push((window, 0));
            var count = 0;
            while (pending.TryPop(out var next))
            {
                if (++count > 20000 || next.Depth > 96) return Missing;
                if (next.Node is FrameworkElement { IsLoaded: true, IsVisible: true, DataContext: { } context })
                {
                    if (context.GetType().FullName == PreviewName) previews.Add(context);
                    if (context.GetType().FullName == ViewportName) viewports.Add(context);
                }
                for (var i = VisualTreeHelper.GetChildrenCount(next.Node) - 1; i >= 0; i--)
                    pending.Push((VisualTreeHelper.GetChild(next.Node, i), next.Depth + 1));
            }
            // Ambiguity in one surface does not disable the other surface.
            return FromKnownInstances(previews.Count == 1 ? previews.Single() : null, viewports.Count == 1 ? viewports.Single() : null);
        }
        catch (Exception) { return Missing; }
    }
    private static bool OwnedBy(Window window, Window main)
    {
        Window? current = window;
        for (var depth = 0; current != null && depth < 16; depth++, current = current.Owner)
            if (ReferenceEquals(current, main)) return true;
        return false;
    }
    internal static ExpressionNavigationHost FromKnownInstances(object? preview, object? viewport)
    {
        var seek = Bind<Func<int, Task>>(preview, PreviewName, "SeekAsync", typeof(Task));
        var contains = Bind<Func<int, bool>>(viewport, ViewportName, "ContainFrameInViewport", typeof(bool));
        var scroll = Bind<Action<int>>(viewport, ViewportName, "ScrollFrame", typeof(void));
        return new(seek == null ? null : preview, seek, contains, scroll) { ViewportOwner = viewport };
    }
    private static T? Bind<T>(object? instance, string knownName, string member, Type returnType) where T : Delegate
    {
        try
        {
            if (instance?.GetType().FullName != knownName) return null;
            var method = instance.GetType().GetMethod(member, BindingFlags.Instance | BindingFlags.Public, null, [typeof(int)], null);
            return method is { IsGenericMethod: false } && method.ReturnType == returnType
                ? method.CreateDelegate(typeof(T), instance) as T : null;
        }
        catch (Exception) { return null; }
    }
}
