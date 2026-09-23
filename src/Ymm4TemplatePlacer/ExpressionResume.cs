namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private void RestoreOrDeferExpressionWork(TransientWorkSnapshot snapshot, ref int skipped)
    {
        if (snapshot.Assignments.Count == 0) return;
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
}
