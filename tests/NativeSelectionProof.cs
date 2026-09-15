using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task InvokeSelectionButton(Button button)
    {
        button.BringIntoView(); await Idle();
        Assert(button.IsEnabled, "selection WPF button is enabled");
        ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
    }
    private static async Task VerifySelectionProfiles(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W9 selection profiles";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var original = Signature(timeline);
        var ca = new Character { Name = "SelectionA" }; var cb = new Character { Name = "SelectionB" };
        var target = new VoiceItem(ca) { Frame = 3000, Length = 41, Layer = 2, Serif = "selection target", Remark = "keep target prose" };
        var blocker = new TachieFaceItem(cb) { Frame = 3035, Length = 20, Layer = 80, Remark = "late manual blocker" };
        var source = new TachieItem(ca) { Frame = 7, Length = 9, Layer = 80, Group = 17, Remark = "source prose\nCWT_TPL:S=123;P=expression" };
        var template = Template("W9/Decoration", [source]); ItemSettings.Default.Templates.Add(template);
        undo.Record();
        Assert(timeline.TryAddItems([target], target.Frame, target.Layer), "W9 native target fixture insertion");
        Assert(timeline.TryAddItems([blocker], blocker.Frame, blocker.Layer), "W9 native late blocker insertion");
        undo.Record(); vm.Refresh();
        vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "飾り"; var entry = vm.RegisterLibrary();
        vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id);
        // TryAddItems selects inserted Items in the host. Establish the intended empty context explicitly.
        timeline.SelectedItems = [];
        ShowTask(view, "selection"); await Idle();
        var surface = view.SelectionSurface;
        Assert(surface.IsLoaded && ReferenceEquals(surface.DataContext, vm), "W9 actual Selection panel is hosted in native YMM4");
        Assert(timeline.SelectedItems.Count == 0 && vm.SelectionProfiles.Count == 0 && !vm.PlaceSelectionCommand.CanExecute(null), "W9 no selection exposes no unusable profiles");
        timeline.SelectedItems = [target]; await Idle();
        Assert(vm.SelectionProfiles.Count == 2 && surface.ProfileSelector.Items.Count == 2, "W9 single native target exposes only Companion and Point");
        var defaultCompanion = vm.SelectedSelectionPreset!.Id;
        vm.CopySelectionPreset(); var companionId = vm.SelectedSelectionPreset!.Id;
        vm.SelectionDraft.Name = "対象の前後に飾る"; vm.SelectionDraft.StartOffset = "invalid";
        var persisted = File.ReadAllText(PlacerSettingsStore.DefaultPath); var stable = Signature(timeline);
        RejectWithoutMutation(timeline, vm.SaveSelectionPreset, "W9 invalid numeric draft fails before Timeline changes");
        Assert(File.ReadAllText(PlacerSettingsStore.DefaultPath) == persisted, "W9 invalid draft does not overwrite saved settings");
        vm.SelectionDraft.StartOffset = "-5"; vm.SelectionDraft.EndOffset = "10";
        vm.SelectionDraft.UseTemplateLayer = false; vm.SelectionDraft.Minimum = "80"; vm.SelectionDraft.Maximum = "82"; vm.SelectionDraft.Preferred = "80";
        Assert(vm.SelectionPresetDirty && !vm.PlaceSelectionCommand.CanExecute(null), "W9 dirty preset disables placement");
        RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W9 direct selection placement also rejects dirty preset");
        surface.SelectionPresetEditor.IsExpanded = true; await Idle();
        await InvokeSelectionButton(surface.SaveSelectionPresetButton);
        Assert(!vm.HasError && !vm.SelectionPresetDirty, "W9 actual WPF Save commits the preset draft");
        var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        var companion = saved.SelectionPresets.Single(x => x.Id == companionId);
        Assert(saved.CurrentSelectionPresetId == companionId && companion.StartOffset == -5 && companion.EndOffset == 10 && companion.Layer.Preferred == 80,
            "W9 multiple presets and current parameters survive independent settings reload");
        surface.SelectionPresetEditor.IsExpanded = false; await Idle();
        await InvokeSelectionButton(surface.PreviewSelectionButton);
        Assert(!vm.HasError && Signature(timeline) == stable && vm.SelectionPreview.Contains("2995", StringComparison.Ordinal), "W9 actual preview is mutation-free and reports planned geometry");
        var before = timeline.Items;
        await InvokeSelectionButton(surface.PlaceSelectionButton);
        var added = timeline.Items.Except(before).Single();
        Assert(!vm.HasError && added is TachieItem && added.Frame == 2995 && added.Length == 56 && added.Layer == 81,
            "W9 actual Companion Place uses target span plus offsets and full-span Layer planning");
        Assert(added.Group == 0 && added.Remark == "source prose\n" && target.Remark == "keep target prose", "W9 singleton non-Face clone preserves user prose and newline and creates no association");
        Assert(source.Frame == 7 && source.Length == 9 && source.Layer == 80 && source.Group == 17 && source.Remark.Contains("CWT_TPL:", StringComparison.Ordinal),
            "W9 source Template and its copied association remain unchanged");
        var placed = Signature(timeline); await undo.UndoAsync(); await Idle();
        Assert(Signature(timeline) == stable, "W9 Companion is one native Undo");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "W9 Companion is one native Redo");
        await undo.UndoAsync(); await Idle(); timeline.SelectedItems = [target];
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, companion with { StartOffset = int.MinValue }), "W9 negative start rejects without partial addition");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, companion with { EndOffset = int.MaxValue }), "W9 overflow rejects without partial addition");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, companion with { Layer = companion.Layer with { Maximum = 80 } }), "W9 exhausted Layer band preserves all existing items");
        var stale = SelectionPlacement.Create(timeline, entry, companion); target.Frame++;
        RejectWithoutMutation(timeline, () => stale.Plan.Commit(timeline, undo), "W9 existing stale-plan guard covers target changes"); target.Frame--;
        timeline.SelectedItems = [target, blocker]; await Idle();
        Assert(vm.SelectionProfiles.All(x => x.Value is not (SelectionProfile.TargetCompanion or SelectionProfile.PointEmphasis)) && !vm.PlaceSelectionCommand.CanExecute(null), "W9 multiple selection does not offer single-target profiles");
        RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W9 invalid multi-target cardinality is enforced before mutation");
        timeline.SelectedItems = [blocker]; await Idle();
        RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W9 another Character target is never silently rewritten");
        timeline.SelectedItems = [target]; await Idle();
        ItemSettings.Default.Templates.Remove(template);
        try { RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W9 missing live Template is rejected instead of copied from Library storage"); }
        finally { ItemSettings.Default.Templates.Add(template); }
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.PointEmphasis);
        vm.CopySelectionPreset(); vm.SelectionDraft.Name = "75%強調"; vm.SelectionDraft.AnchorPercent = 75;
        vm.SelectionDraft.StartOffset = "-3"; vm.SelectionDraft.Duration = "7";
        vm.SelectionDraft.UseTemplateLayer = false; vm.SelectionDraft.Minimum = "80"; vm.SelectionDraft.Maximum = "82"; vm.SelectionDraft.Preferred = "080";
        vm.SaveSelectionPreset();
        Assert(!vm.SelectionPresetDirty && vm.SelectionDraft.Preferred == "80", "W9 semantically unchanged numeric values normalize after save");
        var point = vm.SelectedSelectionPreset!;
        foreach (var percent in new[] { 0, 25, 50, 75, 100 })
        {
            var span = PointEmphasisProfile.Span(target, point with { AnchorPercent = percent });
            Assert(span == (3000 + 41 * percent / 100 - 3, 7), $"W9 point {percent} percent uses deterministic floor anchor and fixed length");
        }
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, point with { AnchorPercent = 33 }), "W9 arbitrary anchor values are not a rule language");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, point with { Duration = 0 }), "W9 zero fixed duration rejects");
        before = timeline.Items; await InvokeSelectionButton(surface.PlaceSelectionButton);
        added = timeline.Items.Except(before).Single();
        Assert(!vm.HasError && added.Frame == 3027 && added.Length == 7 && added.Layer == 80, "W9 actual Point WPF Place uses current point preset");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == stable, "W9 Point is one native Undo preserving unrelated items");
        timeline.SelectedItems = [target];
        vm.DeleteSelectionPreset();
        RejectWithoutMutation(timeline, vm.DeleteSelectionPreset, "W9 each Profile retains at least one saved preset");
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.TargetCompanion);
        vm.SelectedSelectionPreset = vm.SelectionPresets.Single(x => x.Id == companionId); vm.DeleteSelectionPreset();
        Assert(vm.SelectedSelectionPreset!.Id == defaultCompanion, "W9 deleting copy restores another valid preset in its Profile");
        timeline.SelectedItems = [];
        vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        undo.Record(); timeline.Items = timeline.Items.RemoveAll(x => ReferenceEquals(x, target) || ReferenceEquals(x, blocker)); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        ItemSettings.Default.Templates.Remove(template); vm.Refresh(); await Idle();
        Assert(Signature(timeline) == original, "W9 fixture cleanup restores unrelated original native state");
        Log("W9=PASS");
    }
}
