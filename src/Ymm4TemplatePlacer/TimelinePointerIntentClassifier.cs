using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

public enum TimelinePointerOrigin { Unknown, Item, TimelineBackground, Ruler }
public enum PlacementContext { Generic, Selection }

// This is a bounded host-input adapter, not a semantic Timeline API. It is verified on
// YMM4 4.55.1.1 but intentionally not version-gated: compatible newer hosts keep working.
// No property lookup, private API, selection mutation or geometry belongs here. Unknown routes stay unknown.
internal static class TimelinePointerIntentClassifier
{
    internal const string TimelineViewName = "YukkuriMovieMaker.Views.TimelineView";
    internal const string TimelineModelName = "YukkuriMovieMaker.ViewModels.TimelineViewModel";
    internal const string ItemViewName = "YukkuriMovieMaker.Views.TimelineItemView";
    internal const string ItemModelName = "YukkuriMovieMaker.ViewModels.TimelineItemViewModel";
    internal const string ScaleViewName = "YukkuriMovieMaker.Views.TimelineScaleView";
    internal const string ScaleModelName = "YukkuriMovieMaker.ViewModels.TimelineScaleViewModel";
    private const int MaximumDepth = 32;
    internal static Version HostVersion => typeof(Timeline).Assembly.GetName().Version ?? new Version(0, 0);
    internal static bool IsPinnedHost => HostVersion == new Version(4, 55, 1, 1);
    private static readonly string[] RequiredHostTypes =
        [TimelineViewName, TimelineModelName, ItemViewName, ItemModelName, ScaleViewName, ScaleModelName];
    internal static bool DependencySurfaceAvailable => DependencySurfaceAvailableFor(name => typeof(Timeline).Assembly.GetType(name, false) != null);
    internal static bool DependencySurfaceAvailableFor(Func<string, bool> typeExists) => RequiredHostTypes.All(typeExists);
    internal static string DependencyNotice => DependencySurfaceAvailable ? "" :
        $"このYMM4 {HostVersion}ではタイムライン連動の依存関係が変更されたため、一部の自動Set切替は使えません。配置・設定・表情操作は引き続き利用できます。";
    internal sealed record Node(string TypeName, string? DataContextName, bool HostType, bool HostDataContext);

    public static TimelinePointerOrigin Classify(DependencyObject? source)
    {
        // Do not version-gate the adapter. If a newer YMM4 still exposes the same
        // public WPF route, keep using it. Unknown or changed routes fail safe.
        if (source == null) return TimelinePointerOrigin.Unknown;
        var route = new List<Node>();
        for (var current = source; current != null && route.Count < MaximumDepth; current = Parent(current))
        {
            var type = current.GetType(); var context = (current as FrameworkElement)?.DataContext?.GetType();
            route.Add(new(type.FullName ?? "", context?.FullName, type.Assembly == typeof(Timeline).Assembly,
                context?.Assembly == typeof(Timeline).Assembly));
            if (type.FullName == TimelineViewName) break;
        }
        return ClassifyRoute(route);
    }
    internal static TimelinePointerOrigin ClassifyRoute(IReadOnlyList<Node> route)
    {
        if (route.Count == 0 || route.Count > MaximumDepth || !Host(route[^1], TimelineViewName, TimelineModelName))
            return TimelinePointerOrigin.Unknown;
        if (route.Any(x => Host(x, ItemViewName, ItemModelName))) return TimelinePointerOrigin.Item;
        if (route.Any(x => Host(x, ScaleViewName, ScaleModelName))) return TimelinePointerOrigin.Ruler;
        // Pinned blank-canvas prefix. Merely inheriting TimelineViewModel is not enough:
        // toolbar grids, scrollbars, layer controls and unknown descendants are excluded.
        string[] prefix = ["System.Windows.Controls.Grid", "System.Windows.Controls.Grid",
            "System.Windows.Controls.ScrollContentPresenter", "System.Windows.Controls.Grid", "System.Windows.Controls.ScrollViewer"];
        if (route.Count >= prefix.Length && prefix.Select((name, i) => route[i].TypeName == name &&
            route[i].HostDataContext && route[i].DataContextName == TimelineModelName).All(x => x))
            return TimelinePointerOrigin.TimelineBackground;
        return TimelinePointerOrigin.Unknown;
    }
    private static bool Host(Node node, string type, string context) =>
        node.HostType && node.HostDataContext && node.TypeName == type && node.DataContextName == context;
    internal static DependencyObject? Parent(DependencyObject current) => current switch
    {
        Visual or Visual3D => VisualTreeHelper.GetParent(current),
        FrameworkContentElement content => content.Parent,
        _ => null
    };
}

// The View owns subscription lifetime. This class forwards input only; the root owns
// all admission/context decisions. PostProcess MouseUp is after YMM4's re-click selection.
internal sealed class TimelinePointerInputRouter(Action<TimelinePointerOrigin> begin, Action end) : IDisposable
{
    private InputManager? manager;
    public bool IsAttached => manager != null;
    public void Attach()
    {
        if (manager != null) return;
        manager = InputManager.Current;
        manager.PreProcessInput += BeforeInput;
        manager.PostProcessInput += AfterInput;
    }
    public void Detach()
    {
        if (manager == null) return;
        manager.PreProcessInput -= BeforeInput;
        manager.PostProcessInput -= AfterInput;
        manager = null; end();
    }
    private void BeforeInput(object sender, PreProcessInputEventArgs e)
    {
        if (e.StagingItem.Input is MouseButtonEventArgs { ChangedButton: MouseButton.Left } mouse && mouse.RoutedEvent == Mouse.PreviewMouseDownEvent)
            begin(TimelinePointerIntentClassifier.Classify(mouse.MouseDevice.DirectlyOver as DependencyObject));
    }
    private void AfterInput(object sender, ProcessInputEventArgs e)
    {
        if (e.StagingItem.Input is MouseButtonEventArgs { ChangedButton: MouseButton.Left } mouse && mouse.RoutedEvent == Mouse.MouseUpEvent)
            end();
    }
    public void Dispose() => Detach();
}
