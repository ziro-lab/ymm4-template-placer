using System.IO;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyAssociations(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W8 association";
        Assert(AssociationTag.Voice("user prose CWT_TPL:V=23", out _) == AssociationTagState.None, "W8 inline user prose is not an association tag");
        Assert(AssociationTag.Voice("note\r\nCWT_TPL:V=23\r\nend", out var parsed) == AssociationTagState.Valid && parsed == 23, "W8 exact CRLF target tag is readable without rewriting prose");
        Assert(AssociationTag.Voice("CWT_TPL:V=023", out _) == AssociationTagState.Invalid && AssociationTag.Voice("CWT_TPL:V=0", out _) == AssociationTagState.Invalid,
            "W8 noncanonical and zero IDs are rejected rather than guessed");
        Assert(AssociationTag.Voice("CWT_TPL:V=23\nCWT_TPL:V=23", out _) == AssociationTagState.Invalid &&
            AssociationTag.Source("CWT_TPL:S=23;P=expression\nCWT_TPL:S=23;P=expression", out _) == AssociationTagState.Invalid, "W8 duplicate target and source tags are not silently repaired");
        Assert(AssociationTag.Source("CWT_TPL:S=23;P=unknown", out _) == AssociationTagState.Invalid &&
            AssociationTag.Source("CWT_TPL:S=999999999999999999999999;P=expression", out _) == AssociationTagState.Invalid, "W8 unknown profiles and overflowing IDs are bounded");
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var originalItems = timeline.Items; var original = Signature(timeline); var originalPlayhead = timeline.CurrentFrame;
        var ca = new Character { Name = "ResyncA" }; var cb = new Character { Name = "ResyncB" };
        var a = new VoiceItem(ca) { Frame = 2000, Length = 40, Layer = 1, Serif = "associated A", Remark = "user target A\r\nkeep this" };
        var b = new VoiceItem(ca) { Frame = 2100, Length = 30, Layer = 1, Serif = "associated B", Remark = "user target B" };
        var other = new VoiceItem(cb) { Frame = 2200, Length = 20, Layer = 1, Serif = "other Character", Remark = "user other" };
        var sourceA = new TachieFaceItem(ca) { Length = 17, Layer = 80, Remark = "user face\nCWT_TPL:S=999;P=expression\ninline CWT_TPL:V=77 is prose" };
        var sourceB = new TachieFaceItem(cb) { Length = 13, Layer = 80, Remark = "other face note" };
        var ta = Template("W8/FaceA", [sourceA]); var tb = Template("W8/FaceB", [sourceB]);
        ItemSettings.Default.Templates.Add(ta); ItemSettings.Default.Templates.Add(tb);
        undo.Record(); foreach (var voice in new[] { a, b, other }) Assert(timeline.TryAddItems([voice], voice.Frame, voice.Layer), "W8 native Voice fixture insertion"); undo.Record();
        vm.Refresh(); view.MainTabs.SelectedIndex = 0; await Idle();
        foreach (var row in vm.Rows.Where(x => ReferenceEquals(x.Target.Voice, a) || ReferenceEquals(x.Target.Voice, b) || ReferenceEquals(x.Target.Voice, other)))
            row.SelectedChoice = row.Choices.Single(x => ReferenceEquals(x.Template?.Template, ReferenceEquals(row.Target.Voice, other) ? tb : ta));
        vm.CopyExpressionPreset(); vm.ExpressionDraft.Name = "再同期試験"; vm.ExpressionDraft.UseTemplateLayer = false;
        vm.ExpressionDraft.Minimum = "80"; vm.ExpressionDraft.Maximum = "82"; vm.ExpressionDraft.Preferred = "80"; vm.SaveExpressionPreset();
        var before = Signature(timeline); var beforeList = timeline.Items;
        var nextId = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId;
        var staged = AssociatedExpressionPlacement.Create(timeline, vm.Rows.ToArray(), vm.SelectedExpressionPreset!, nextId);
        Assert(staged.Plan.Count == 3 && staged.Plan.UpdateCount == 3 && staged.NextSerial > nextId && Signature(timeline) == before && ReferenceEquals(timeline.Items, beforeList),
            "W8 all clones and target tags are staged before Timeline mutation");
        Assert(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId == nextId, "W8 planning alone never consumes persisted IDs");
        Assert(vm.PlaceCommand.CanExecute(null), "W8 saved preset and selected rows make the placement command ready");
        await Idle();
        await ClickPlace(view);
        Assert(!vm.HasError && AssociationTag.Voice(a.Remark, out _) == AssociationTagState.Valid && AssociationTag.Voice(b.Remark, out _) == AssociationTagState.Valid,
            "W8 actual expression Place command creates weak target tags");
        AssociationTag.Voice(a.Remark, out var aid); AssociationTag.Voice(b.Remark, out var bid); AssociationTag.Voice(other.Remark, out var oid);
        Assert(aid < bid && bid < oid, "W8 serials are plugin-wide rather than restarted for another Character");
        var additions = timeline.Items.Except(beforeList).OfType<TachieFaceItem>().ToArray();
        TachieFaceItem Linked(long id) => additions.Single(x => AssociationTag.Source(x.Remark, out var source) == AssociationTagState.Valid && source!.Serial == id);
        var faceA = Linked(aid); var faceB = Linked(bid); var faceOther = Linked(oid);
        Assert(additions.Length == 3 && a.Remark.StartsWith("user target A\r\nkeep this\n", StringComparison.Ordinal) &&
            faceA.Remark.Contains("user face", StringComparison.Ordinal) && faceA.Remark.Contains("inline CWT_TPL:V=77 is prose", StringComparison.Ordinal) &&
            !faceA.Remark.Contains("S=999", StringComparison.Ordinal), "W8 user remarks survive while copied Template association is replaced on detached clones only");
        Assert(sourceA.Length == 17 && sourceA.Layer == 80 && sourceA.Remark.Contains("S=999", StringComparison.Ordinal), "W8 source Template is not rewritten by association placement");
        var placed = Signature(timeline); var reserved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId;
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "W8 one native Undo removes additions and restores original Voice remarks together");
        Assert(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().NextAssociationId == reserved && reserved > oid,
            "W8 native Undo does not rewind the persisted serial reservation");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "W8 one native Redo restores identical association IDs and additions");
        RejectWithoutMutation(timeline, () => vm.Place(), "W8 repeated associated placement stops instead of replacing or duplicating even with free band Layers");
        await VerifyResyncScenarios(timeline, undo, a, b, other, faceA, faceB, faceOther, ta);
        timeline.SelectedItems = []; vm.DeleteExpressionPreset();
        Assert(originalItems.All(timeline.Items.Contains), "W8 every unrelated original Timeline Item survives all operations");
        undo.Record(); timeline.Items = originalItems; timeline.RefreshTimelineLengthAndMaxLayer(); timeline.CurrentFrame = originalPlayhead; undo.Record();
        ItemSettings.Default.Templates.Remove(ta); ItemSettings.Default.Templates.Remove(tb); vm.Refresh(); await Idle();
        Assert(Signature(timeline) == original, "W8 fixture cleanup restores original native state without masking unrelated Item changes");
        Log("W8=PASS");
    }
}
