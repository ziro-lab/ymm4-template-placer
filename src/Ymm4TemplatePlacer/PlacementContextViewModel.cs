namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    internal bool HasIntentTimeline => intentTimeline != null;
    internal event EventHandler? IntentWorkspaceDeactivated;
    private PlacementContext placementContext;
    private TimelinePointerOrigin? pointerGesture;
    private bool pendingItemPointer;
    private YukkuriMovieMaker.Project.Timeline? placementContextTimeline;
    public PlacementContext PlacementContext => placementContext;

    private void InitializePlacementContext()
    {
        EndTimelinePointer();
        if (ReferenceEquals(placementContextTimeline, timeline)) return;
        placementContextTimeline = timeline;
        ChangePlacementContext(timeline?.SelectedItems.Count > 0 ? PlacementContext.Selection : PlacementContext.Generic, false);
    }
    internal void ObserveTimelinePointer(TimelinePointerOrigin origin)
    {
        if (intentTimeline == null || UseLegacyWorkspace) return;
        pointerGesture = origin;
        pendingItemPointer = origin == TimelinePointerOrigin.Item;
        // An Item hit records intent only: the old Selection is not the clicked Selection.
        if (origin is TimelinePointerOrigin.TimelineBackground or TimelinePointerOrigin.Ruler)
            ChangePlacementContext(PlacementContext.Generic);
    }
    internal void EndTimelinePointer() { pointerGesture = null; pendingItemPointer = false; }
    private void ObserveContextSelection()
    {
        if (timeline?.SelectedItems.Count is not > 0) return;
        if (pointerGesture == null || (pointerGesture == TimelinePointerOrigin.Item && pendingItemPointer))
        {
            pendingItemPointer = false;
            ChangePlacementContext(PlacementContext.Selection, false);
        }
        // Background/ruler/unknown gestures suppress fallback until the host finishes input.
        // Independent selection notifications still supply the documented non-pointer fallback.
    }
    private void ChangePlacementContext(PlacementContext next, bool refresh = true)
    {
        if (placementContext == next) return;
        placementContext = next; OnPropertyChanged(nameof(PlacementContext));
        if (refresh) RefreshIntentWorkspace();
    }
}
