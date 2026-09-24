using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static Task VerifyAssociations(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W8 historical association reader compatibility";
        var before = Signature(timeline);

        Assert(AssociationTag.Voice("user prose CWT_TPL:V=23", out _) == AssociationTagState.None,
            "W8 inline user prose is not mistaken for a historical association tag");
        Assert(AssociationTag.Voice("note\r\nCWT_TPL:V=23\r\nend", out var parsed) == AssociationTagState.Valid && parsed == 23,
            "W8 exact historical CRLF target tag remains readable without rewriting prose");
        Assert(AssociationTag.Voice("CWT_TPL:V=023", out _) == AssociationTagState.Invalid &&
            AssociationTag.Voice("CWT_TPL:V=0", out _) == AssociationTagState.Invalid,
            "W8 noncanonical and zero historical IDs remain fail-closed");
        Assert(AssociationTag.Voice("CWT_TPL:V=23\nCWT_TPL:V=23", out _) == AssociationTagState.Invalid &&
            AssociationTag.Source("CWT_TPL:S=23;P=expression\nCWT_TPL:S=23;P=expression", out _) == AssociationTagState.Invalid,
            "W8 duplicate historical target/source tags are never silently repaired");
        Assert(AssociationTag.Source("CWT_TPL:S=23;P=unknown", out _) == AssociationTagState.Invalid &&
            AssociationTag.Source("CWT_TPL:S=999999999999999999999999;P=expression", out _) == AssociationTagState.Invalid,
            "W8 unknown profiles and overflowing historical IDs remain bounded");
        Assert(AssociationTag.Source("memo\nCWT_TPL:S=42;P=expression", out var source) == AssociationTagState.Valid &&
            source is { Serial: 42, Profile: "expression" },
            "W8 canonical historical source association remains readable for existing Timeline material");

        Assert(Signature(timeline) == before,
            "W8 historical association compatibility checks are read-only and never mutate Timeline state");
        Log("W8=PASS");
        return Task.CompletedTask;
    }
}
