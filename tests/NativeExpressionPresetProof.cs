using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyExpressionPresets(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W7 expression presets";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var original = Signature(timeline); var defaultId = vm.SelectedExpressionPreset!.Id;
        var ca = new Character { Name = "PresetA" }; var cb = new Character { Name = "PresetB" };
        var a = new VoiceItem(ca) { Frame = 1000, Length = 40, Layer = 1, Serif = "preset first" };
        var b = new VoiceItem(cb) { Frame = 1060, Length = 20, Layer = 2, Serif = "other Character between" };
        var next = new VoiceItem(ca) { Frame = 1090, Length = 30, Layer = 1, Serif = "next same Character" };
        var overlap = new VoiceItem(ca) { Frame = 1100, Length = 80, Layer = 2, Serif = "overlapping next" };
        var distant = new VoiceItem(ca) { Frame = 1400, Length = 20, Layer = 1, Serif = "outside MaxGap" };
        var blocker = new TachieFaceItem(cb) { Frame = 1088, Length = 3, Layer = 60, Remark = "manual late blocker" };
        IItem[] fixture = [a, b, next, overlap, distant, blocker];
        var face = new TachieFaceItem(ca) { Frame = 7, Length = 11, Layer = 60, Remark = "source note" };
        var template = Template("W7/FaceA", [face]); ItemSettings.Default.Templates.Add(template);
        undo.Record();
        foreach (var item in fixture) Assert(timeline.TryAddItems([item], item.Frame, item.Layer), "W7 native fixture insertion");
        undo.Record(); vm.Refresh(); view.MainTabs.SelectedIndex = 0; await Idle();
        Assert(vm.Rows.Single(x => x.Character == "TestA").Choices[1].Label.Contains("（棚）", StringComparison.Ordinal),
            "W7 Character Palette entries precede other compatible Face choices without removing candidates");
        foreach (var row in vm.Rows.Where(x => ReferenceEquals(x.Target.Voice.Character, ca)))
            row.SelectedChoice = row.Choices.Single(x => ReferenceEquals(x.Template?.Template, template));
        var fixtureState = Signature(timeline);
        vm.CopyExpressionPreset(); var copyId = vm.SelectedExpressionPreset!.Id;
        Assert(copyId != defaultId && vm.ExpressionPresets.Count == 2, "W7 multiple presets share one expression profile");
        vm.ExpressionDraft.Name = "次まで試験"; vm.ExpressionDraft.Duration = ExpressionDuration.NextSameCharacter;
        vm.ExpressionDraft.MaxGap = "invalid";
        var settingsBefore = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        RejectWithoutMutation(timeline, vm.SaveExpressionPreset, "W7 invalid numeric draft rejects before settings or Timeline mutation");
        Assert(File.ReadAllText(PlacerSettingsStore.DefaultPath) == settingsBefore, "W7 invalid save retains persisted preset");
        vm.ExpressionDraft.MaxGap = "90"; vm.ExpressionDraft.StartOffset = "-5"; vm.ExpressionDraft.EndOffset = "3";
        vm.ExpressionDraft.UseTemplateLayer = false; vm.ExpressionDraft.Minimum = "60"; vm.ExpressionDraft.Maximum = "62"; vm.ExpressionDraft.Preferred = "60";
        Assert(vm.ExpressionPresetDirty && !vm.PlaceCommand.CanExecute(null), "W7 unsaved preset disables placement rather than using stale settings");
        RejectWithoutMutation(timeline, () => vm.Place(), "W7 direct placement also rejects dirty preset");
        var surface = view.PresetSurface; surface.PresetEditor.IsExpanded = true; await Idle();
        Assert(surface.IsLoaded && ReferenceEquals(surface.DataContext, vm), "W7 actual native preset WPF surface is hosted");
        ((IInvokeProvider)new ButtonAutomationPeer(surface.SavePresetButton).GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
        Assert(!vm.HasError && !vm.ExpressionPresetDirty && surface.PresetSelector.Items.Count == 2, "W7 actual Save button commits and refreshes preset UI");
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var preset = saved.ExpressionPresets.Single(x => x.Id == copyId);
        Assert(saved.CurrentExpressionPresetId == copyId && preset.Duration == ExpressionDuration.NextSameCharacter && preset.Layer.Preferred == 60,
            "W7 current preset and semantic parameters survive independent settings reload");
        var snapshots = VoiceSnapshot.Capture(timeline);
        VoiceSnapshot Snapshot(VoiceItem voice) => snapshots.Single(x => ReferenceEquals(x.Voice, voice));
        Assert(CharacterExpressionProfile.Span(Snapshot(a), snapshots, preset) == (995, 98), "W7 next same Character skips intervening other Character and applies both offsets");
        Assert(CharacterExpressionProfile.Span(Snapshot(next), snapshots, preset) == (1085, 38), "W7 overlapping next Voice never shortens current Voice span");
        Assert(CharacterExpressionProfile.Span(Snapshot(overlap), snapshots, preset) == (1095, 88), "W7 excessive gap falls back to current Voice end");
        Assert(CharacterExpressionProfile.Span(Snapshot(distant), snapshots, preset) == (1395, 28), "W7 last same-Character Voice falls back safely");
        Assert(CharacterExpressionProfile.Span(Snapshot(a), snapshots, preset with { MaxGap = 49, StartOffset = 0, EndOffset = 0 }) == (1000, 40), "W7 gap above threshold is not extended");
        Assert(CharacterExpressionProfile.Span(Snapshot(a), snapshots, preset with { MaxGap = 50, StartOffset = 0, EndOffset = 0 }) == (1000, 90), "W7 MaxGap boundary is inclusive");
        RejectWithoutMutation(timeline, () => CharacterExpressionProfile.Create(timeline, vm.Rows.ToArray(), preset with { StartOffset = int.MinValue }), "W7 negative planned start rejects entire batch");
        RejectWithoutMutation(timeline, () => CharacterExpressionProfile.Create(timeline, vm.Rows.ToArray(), preset with { EndOffset = int.MaxValue }), "W7 arithmetic overflow rejects entire batch");
        var before = timeline.Items;
        var plan = CharacterExpressionProfile.Create(timeline, vm.Rows.ToArray(), preset);
        Assert(Signature(timeline) == fixtureState && ReferenceEquals(before, timeline.Items) && plan.Count == 4, "W7 complete expression plan is ready before any Timeline mutation");
        Assert(plan.Commit(timeline, undo) == 4, "W7 preflighted expression batch commits through existing native plan");
        var additions = timeline.Items.Except(before).ToArray();
        Assert(additions.Single(x => x.Frame == 995).Layer == 61 && additions.Single(x => x.Frame == 1085).Layer == 62,
            "W7 Layer Band reserves existing late blocker and earlier plans in the same batch");
        Assert(face.Frame == 7 && face.Length == 11 && face.Layer == 60 && face.Remark == "source note" && blocker.Remark == "manual late blocker",
            "W7 source Template and manual items are unchanged");
        var after = Signature(timeline); await undo.UndoAsync(); await Idle();
        Assert(Signature(timeline) == fixtureState, "W7 one native Undo restores complete pre-placement Timeline");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == after, "W7 one native Redo restores identical planned geometry");
        await undo.UndoAsync(); await Idle();
        vm.ExpressionDraft.Maximum = "61"; vm.SaveExpressionPreset();
        RejectWithoutMutation(timeline, () => vm.Place(), "W7 later batch Layer exhaustion never leaves earlier additions behind");
        vm.ExpressionDraft.Maximum = "62"; vm.SaveExpressionPreset();
        vm.ExpressionDraft.Preferred = "060"; vm.SaveExpressionPreset();
        Assert(!vm.ExpressionPresetDirty && vm.ExpressionDraft.Preferred == "60", "W7 semantically equal numeric save canonicalizes the editor");
        var workbook = Path.Combine(output, "W7-current-preset.xlsx"); vm.ExportTo(workbook);
        using (var document = SpreadsheetDocument.Open(workbook, false))
            Assert(!new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).Any(), "W7 prioritized assignments retain valid Open XML export");
        vm.ExpressionDraft.Duration = ExpressionDuration.VoiceSpan; vm.ExpressionDraft.StartOffset = "0"; vm.ExpressionDraft.EndOffset = "0";
        vm.ExpressionDraft.Minimum = "70"; vm.ExpressionDraft.Maximum = "73"; vm.ExpressionDraft.Preferred = "70"; vm.SaveExpressionPreset();
        foreach (var row in vm.Rows) row.SelectedChoice = row.Choices[0];
        vm.ImportFrom(workbook);
        Assert(Signature(timeline) == fixtureState && vm.SelectedExpressionPreset!.Id == copyId && vm.Rows.Count(x => x.SelectedChoice.Template != null) == 4,
            "W7 Excel import changes assignments only and retains the current preset");
        surface.PresetEditor.IsExpanded = false; await Idle(); await ClickPlace(view);
        var importedAdditions = timeline.Items.Except(before).ToArray();
        Assert(!vm.HasError && importedAdditions.Length == 4 && importedAdditions.Single(x => x.Frame == 1000).Length == 40 && importedAdditions.Single(x => x.Frame == 1000).Layer == 70,
            "W7 post-Excel actual Place button uses preset changed after export, not a workbook snapshot");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == fixtureState, "W7 post-Excel batch is one native Undo");
        vm.DeleteExpressionPreset();
        Assert(vm.SelectedExpressionPreset!.Id == defaultId && vm.ExpressionPresets.Count == 1, "W7 deleting a preset retains a valid current preset without retiming Items");
        RejectWithoutMutation(timeline, vm.DeleteExpressionPreset, "W7 last expression preset cannot be deleted");
        undo.Record(); timeline.Items = timeline.Items.RemoveAll(x => fixture.Contains(x)); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        ItemSettings.Default.Templates.Remove(template); vm.Refresh(); await Idle();
        Assert(Signature(timeline) == original, "W7 proof restores all original items after native fixture cleanup");
        Log("W7=PASS");
    }
}
