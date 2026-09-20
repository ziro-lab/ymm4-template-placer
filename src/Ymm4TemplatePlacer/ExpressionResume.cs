using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

// In-memory work only. No compatibility settings migration and no workspace switch.
internal sealed record ExpressionResumeWork(Timeline Timeline, bool LegacyWorkspace, IReadOnlyList<AssignmentWork> Assignments);

public sealed partial class PlacerViewModel
{
    private ExpressionResumeWork? deferredExpressionResume;

    private void RestoreOrDeferExpressionWork(TransientWorkSnapshot snapshot, ref int skipped)
    {
        deferredExpressionResume = snapshot.DeferredExpressions;
        if (deferredExpressionResume != null && !ReferenceEquals(deferredExpressionResume.Timeline, timeline))
            deferredExpressionResume = null;
        if (snapshot.Assignments.Count == 0) return;
        if (snapshot.LegacyWorkspace != UseLegacyWorkspace)
        {
            // Normal R3 startup must not enter the hidden compatibility workspace.
            // Keep its exact assignment snapshot until that vocabulary is explicitly
            // requested by the retained internal compatibility route. Never reinterpret
            // legacy Template choices as relative Intent choices or replay Timeline writes.
            deferredExpressionResume = new(RequireTimeline(), snapshot.LegacyWorkspace, snapshot.Assignments);
            return;
        }
        RestoreExactExpressionAssignments(snapshot.Assignments, ref skipped);
    }

    private void RestoreExactExpressionAssignments(IReadOnlyList<AssignmentWork> assignments, ref int skipped)
    {
        var previous = suppressExpressionApply; suppressExpressionApply = true;
        try
        {
            foreach (var assignment in assignments)
            {
                var row = Rows.SingleOrDefault(x => SameVoice(x.Target, assignment.Target));
                if (row == null) { skipped++; continue; }
                if (UsesRelativeExpressions && ExpressionAssociationOwnsSelection(row)) continue;
                var choice = row.Choices.SingleOrDefault(x => x.Template != null &&
                    ReferenceEquals(x.Template.Template, assignment.SourceTemplate) && ReferenceEquals(x.Template.Face, assignment.SourceFace) &&
                    x.Template.Name == assignment.SourceName && x.Template.Character == assignment.SourceCharacter);
                if (choice == null) { skipped++; continue; }
                row.SelectedChoice = choice;
            }
        }
        finally { suppressExpressionApply = previous; }
    }

    private void TryRestoreDeferredExpressionWork()
    {
        var work = deferredExpressionResume;
        if (work == null || timeline == null || work.LegacyWorkspace != UseLegacyWorkspace) return;
        deferredExpressionResume = null;
        var skipped = 0;
        if (ReferenceEquals(work.Timeline, timeline)) RestoreExactExpressionAssignments(work.Assignments, ref skipped);
        else skipped = work.Assignments.Count;
        OnPropertyChanged(nameof(Summary)); UpdateCommands();
        if (skipped > 0)
        {
            HasError = false;
            Status = $"途中作業の一部を復元できませんでした（{skipped}件）。変更された対象は推測せず復元していません。";
            keepPartialStatus = true;
        }
        else if (Status == "旧workspaceの未配置選択を保持しています。現在の配置へ自動変換していません。")
            Status = "";
    }
}
