using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyResyncScenarios(Timeline timeline, UndoRedoManager undo, VoiceItem a, VoiceItem b,
        VoiceItem other, TachieFaceItem faceA, TachieFaceItem faceB, TachieFaceItem faceOther, ItemTemplate template)
    {
        stage = "W8 resync";
        var vm = ViewModel!; var view = View!;
        var remarkA = faceA.Remark; var remarkB = faceB.Remark;
        var otherState = (faceOther.Frame, faceOther.Length, faceOther.Layer, faceOther.Remark);
        undo.Record(); a.Frame = 2020; a.Length = 50; b.Frame = 2130; undo.Record(); await Idle();
        Assert(faceA.Frame == 2000 && faceB.Frame == 2100, "W8 Voice moves never trigger continuous resync");
        vm.ExpressionDraft.Duration = ExpressionDuration.NextSameCharacter; vm.ExpressionDraft.MaxGap = "90";
        vm.ExpressionDraft.StartOffset = "-2"; vm.ExpressionDraft.EndOffset = "3";
        vm.ExpressionDraft.Minimum = "84"; vm.ExpressionDraft.Maximum = "85"; vm.ExpressionDraft.Preferred = "84";
        timeline.SelectedItems = [a, b];
        RejectWithoutMutation(timeline, () => vm.Resync(), "W8 resync rejects an unsaved current preset");
        vm.SaveExpressionPreset(); await Idle(); var before = Signature(timeline);
        Assert(view.ResyncButton.IsEnabled, "W8 Timeline selection enables actual resync UI command");
        ((IInvokeProvider)new ButtonAutomationPeer(view.ResyncButton).GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
        Assert(!vm.HasError && faceA.Frame == 2018 && faceA.Length == 115 && faceA.Layer == 84 && faceB.Frame == 2128 && faceB.Length == 35 && faceB.Layer == 85,
            "W8 actual Resync button uses current Next-Character preset and reserves both planned Layers");
        Assert(timeline.Items.Contains(faceA) && timeline.Items.Contains(faceB) && faceA.Remark == remarkA && faceB.Remark == remarkB,
            "W8 resync preserves actual related Item identity and user/association remarks");
        Assert((faceOther.Frame, faceOther.Length, faceOther.Layer, faceOther.Remark) == otherState, "W8 unselected associated Item remains unchanged");
        var after = Signature(timeline); await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "W8 one native Undo restores every resynced Item");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == after, "W8 one native Redo restores current-preset resync geometry");

        undo.Record(); a.Frame = 2040; b.Frame = 2160; undo.Record();
        before = Signature(timeline); var bFrame = faceB.Frame;
        timeline.SelectedItems = [faceA]; var result = vm.Resync();
        Assert(result.Plan.UpdateCount == 1 && faceA.Frame == 2038 && faceA.Length == 125 && faceB.Frame == bFrame,
            "W8 selecting one related Item resyncs only that Item, not all related Items in Scene");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "W8 single-item resync is one native Undo");

        var bRemark = b.Remark; undo.Record(); b.Remark = "user B lost tag"; a.Frame = 2060; undo.Record();
        before = Signature(timeline); timeline.SelectedItems = [faceA, faceB]; result = vm.Resync();
        Assert(result.Plan.UpdateCount == 1 && result.Skipped.Count == 1 && faceA.Frame == 2058 && faceB.Frame == bFrame,
            "W8 best-effort resync updates unique target and skips missing target without guessing by position or text");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "W8 successful subset of partial resync is one native Undo");
        undo.Record(); b.Remark = bRemark; undo.Record();

        var duplicate = new VoiceItem(a.Character!) { Frame = 2500, Length = 20, Layer = 2, Serif = "different text", Remark = a.Remark };
        undo.Record(); Assert(timeline.TryAddItems([duplicate], duplicate.Frame, duplicate.Layer), "W8 duplicate-ID fixture insertion"); undo.Record();
        before = Signature(timeline); timeline.SelectedItems = [faceA]; result = vm.Resync();
        Assert(result.Plan.UpdateCount == 0 && result.Skipped.Count == 1 && Signature(timeline) == before && duplicate.Remark == a.Remark,
            "W8 multiple ID+Character targets are skipped without ordering, similarity or copy/paste repair");
        undo.Record(); timeline.Items = timeline.Items.Remove(duplicate); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();

        AssociationTag.Voice(a.Remark, out var aid); var otherRemark = other.Remark;
        undo.Record(); other.Remark = AssociationTag.TargetLine(aid); a.Frame = 2080; undo.Record();
        before = Signature(timeline); timeline.SelectedItems = [faceA]; result = vm.Resync();
        Assert(result.Plan.UpdateCount == 1 && faceA.Frame == 2078 && (faceOther.Frame, faceOther.Length, faceOther.Layer, faceOther.Remark) == otherState,
            "W8 Character guard excludes another Character with the same serial");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "W8 Character-guarded resync remains native undoable");
        undo.Record(); other.Remark = otherRemark; undo.Record();

        var aRemark = a.Remark; undo.Record(); a.Remark = aRemark + "\n" + AssociationTag.TargetLine(aid); undo.Record();
        before = Signature(timeline); result = vm.Resync();
        Assert(result.Skipped.Count == 1 && result.Plan.UpdateCount == 0 && Signature(timeline) == before, "W8 duplicate target tag is not normalized or repaired during resync");
        undo.Record(); a.Remark = aRemark; faceA.Remark = remarkA + "\n" + AssociationTag.SourceLine(aid); undo.Record();
        before = Signature(timeline); result = vm.Resync();
        Assert(result.Skipped.Count == 1 && result.Plan.UpdateCount == 0 && Signature(timeline) == before, "W8 malformed or duplicate source tag remains unchanged");
        undo.Record(); faceA.Remark = remarkA; faceA.Group = 42; undo.Record();
        before = Signature(timeline); result = vm.Resync();
        Assert(result.Skipped.Count == 1 && Signature(timeline) == before, "W8 grouped related Item is safely skipped rather than retiming a partial group");
        undo.Record(); faceA.Group = 0; undo.Record();

        vm.ExpressionDraft.Minimum = "90"; vm.ExpressionDraft.Maximum = "90"; vm.ExpressionDraft.Preferred = "90"; vm.SaveExpressionPreset();
        var blocker = new TachieFaceItem(other.Character!) { Frame = 2090, Length = 2, Layer = 90, Remark = "late manual blocker" };
        undo.Record(); Assert(timeline.TryAddItems([blocker], blocker.Frame, blocker.Layer), "W8 late-blocker fixture insertion"); undo.Record();
        before = Signature(timeline); timeline.SelectedItems = [faceA]; result = vm.Resync();
        Assert(result.Plan.UpdateCount == 0 && result.Skipped.Count == 1 && Signature(timeline) == before, "W8 resync checks full planned duration and never moves a blocking manual Item");
        undo.Record(); timeline.Items = timeline.Items.Remove(blocker); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        vm.ExpressionDraft.Minimum = "92"; vm.ExpressionDraft.Maximum = "93"; vm.ExpressionDraft.Preferred = "92"; vm.SaveExpressionPreset();
        var pending = ResyncPlan.Create(timeline, vm.SelectedExpressionPreset!);
        Assert(pending.Plan.UpdateCount == 1, "W8 resync computes an update before mutation");
        a.Remark += "\nnew user note after planning";
        RejectWithoutMutation(timeline, () => pending.Plan.Commit(timeline, undo), "W8 stale relation remarks invalidate the plan before any update");

        var entry = TemplateResolver.Reference(template, "再同期なし", a.CharacterName);
        var palette = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Character, "W8 Quick Drop", a.CharacterName, [entry.Id])
            { Layer = new LayerPolicy { UseTemplateLayer = false, Minimum = 94, Maximum = 94, Preferred = 94 } };
        timeline.CurrentFrame = 2400;
        var quickDrop = QuickDropPlanner.Create(timeline, entry, palette, CharacterLayerMode.Base); quickDrop.Plan.Commit(timeline, undo);
        Assert(AssociationTag.Source(quickDrop.Item.Remark, out _) == AssociationTagState.None && AssociationTag.Voice(quickDrop.Item.Remark, out _) == AssociationTagState.None,
            "W8 actual Quick Drop planning strips inherited associations and creates no new serial");
        before = Signature(timeline); timeline.SelectedItems = [quickDrop.Item]; result = vm.Resync();
        Assert(result.Plan.UpdateCount == 0 && result.Ignored == 1 && Signature(timeline) == before, "W8 Quick Drop is explicitly ignored by resync");
        await undo.UndoAsync(); await Idle(); Assert(!timeline.Items.Contains(quickDrop.Item), "W8 no-op resync creates no extra native Undo entry");

        var fresh = new VoiceItem(a.Character!) { Frame = 2800, Length = 30, Layer = 1, Serif = "fresh ID" };
        var orphan = new TachieFaceItem(a.Character!) { Frame = 2900, Length = 10, Layer = 95, Remark = AssociationTag.SourceLine(7000000) };
        undo.Record(); Assert(timeline.TryAddItems([fresh], fresh.Frame, fresh.Layer) && timeline.TryAddItems([orphan], orphan.Frame, orphan.Layer), "W8 fresh and orphan-ID fixtures inserted"); undo.Record();
        vm.Refresh(); vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, fresh)).SelectedChoice = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, fresh)).Choices.Single(x => ReferenceEquals(x.Template?.Template, template));
        RejectWithoutMutation(timeline, () => AssociatedExpressionPlacement.Create(timeline, vm.Rows.ToArray(), vm.SelectedExpressionPreset!, long.MaxValue), "W8 exhausted serial allocation fails before marking the Voice");
        before = Signature(timeline); Assert(vm.Place() == 1, "W8 new association after imported IDs places normally");
        Assert(AssociationTag.Voice(fresh.Remark, out var freshId) == AssociationTagState.Valid && freshId > 7000000,
            "W8 allocator does not reuse an orphan source serial already present in imported Scene");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "W8 new serial placement remains atomic after imported-ID reconciliation");
    }
}
