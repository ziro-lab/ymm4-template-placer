using System.ComponentModel;
using System.Windows;

namespace Ymm4TemplatePlacer;

// Native-proof-only observation. No trial boundary, host state or event is changed.
public sealed partial class PlacerViewModel
{
    private Action? removeRound4TrialDiagnostics;
    internal void InstallRound4TrialDiagnostics(Action<string> log)
    {
        if (removeRound4TrialDiagnostics != null || timeline == null || undo == null) return;
        var current = timeline; var manager = undo; var view = NativeProof.View;
        void Trace(string reason)
        {
            log($"R4-A boundary observation: {reason}; trial={expressionTrialSession.IsOpen}; frame={current.CurrentFrame}; selected={current.SelectedItems.Count}; focus={System.Windows.Input.Keyboard.FocusedElement?.GetType().FullName}");
            foreach (var frame in Environment.StackTrace.Split('\n').Take(16)) log("R4-A boundary stack: " + frame.TrimEnd());
        }
        void Changing(object? sender, PropertyChangingEventArgs e) => Trace("Timeline.PropertyChanging(" + e.PropertyName + ")");
        void Recorded(object? sender, EventArgs e) => Trace("UndoRedoManager.Recorded");
        void FocusChanged(object sender, DependencyPropertyChangedEventArgs e) => Trace("View.IsKeyboardFocusWithin=" + e.NewValue);
        current.PropertyChanging += Changing;
        manager.Recorded += Recorded;
        if (view != null) view.IsKeyboardFocusWithinChanged += FocusChanged;
        removeRound4TrialDiagnostics = () =>
        {
            current.PropertyChanging -= Changing;
            manager.Recorded -= Recorded;
            if (view != null) view.IsKeyboardFocusWithinChanged -= FocusChanged;
        };
    }
    internal void RemoveRound4TrialDiagnostics()
    {
        removeRound4TrialDiagnostics?.Invoke(); removeRound4TrialDiagnostics = null;
    }
}
