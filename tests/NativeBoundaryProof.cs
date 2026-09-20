using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyBoundary(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W11 Boundary";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var original = Signature(timeline);
        var a = new TextItem { Frame = 5000, Length = 40, Layer = 1, Remark = "earlier item" };
        var b = new TextItem { Frame = 5040, Length = 50, Layer = 2, Remark = "later item" };
        var distractor = new TextItem { Frame = 5040, Length = 20, Layer = 3, Remark = "never substitute this nearby unselected cut" };
        var blocker = new TextItem { Frame = 5045, Length = 10, Layer = 95, Remark = "cut late blocker" };
        var source = new TextItem { Frame = 13, Length = 8, Layer = 95, Remark = "cut source" };
        var template = Template("W11/CutAccent", [source]); ItemSettings.Default.Templates.Add(template);
        undo.Record();
        foreach (var item in new IItem[] { a, b, distractor, blocker }) Assert(timeline.TryAddItems([item], item.Frame, item.Layer), "W11 native fixture insertion");
        undo.Record(); vm.Refresh(); vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "切替の強調";
        var entry = vm.RegisterLibrary(); vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id);
        timeline.SelectedItems = [b, a]; ShowTask(view, "selection"); await Idle();
        Assert(vm.SelectionProfiles.Select(x => x.Value).SequenceEqual(new[] { SelectionProfile.SelectionRange, SelectionProfile.Boundary }), "W11 exactly two targets offer Range and Boundary without single-target profiles");
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.Boundary); vm.CopySelectionPreset();
        vm.SelectionDraft.Name = "境界を挟む30frame"; vm.SelectionDraft.StartOffset = "-15"; vm.SelectionDraft.Duration = "30";
        vm.SelectionDraft.UseTemplateLayer = false; vm.SelectionDraft.Minimum = "95"; vm.SelectionDraft.Maximum = "97"; vm.SelectionDraft.Preferred = "95";
        vm.SelectionDraft.Tolerance = "-1";
        RejectWithoutMutation(timeline, vm.SaveSelectionPreset, "W11 negative boundary tolerance cannot be saved");
        vm.SelectionDraft.Tolerance = "0"; var surface = view.SelectionSurface; surface.SelectionPresetEditor.IsExpanded = true; await Idle();
        Assert(surface.BoundaryFields.IsVisible && !surface.RangePaddingFields.IsVisible && !vm.PlaceSelectionCommand.CanExecute(null), "W11 actual Boundary editor exposes tolerance and protects unsaved edits");
        await InvokeSelectionButton(surface.SaveSelectionPresetButton);
        var preset = vm.SelectedSelectionPreset!;
        Assert(new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().SelectionPresets.Single(x => x.Id == preset.Id) == preset,
            "W11 saved boundary parameters survive independent reload");
        surface.SelectionPresetEditor.IsExpanded = false; await Idle();
        var stable = Signature(timeline); var before = timeline.Items;
        await InvokeSelectionButton(surface.PreviewSelectionButton);
        Assert(!vm.HasError && Signature(timeline) == stable && vm.SelectionPreview.Contains("5025", StringComparison.Ordinal), "W11 actual preview reports the agreed earlier-item end without mutation");
        await InvokeSelectionButton(surface.PlaceSelectionButton);
        var added = timeline.Items.Except(before).Single();
        Assert(!vm.HasError && added.Frame == 5025 && added.Length == 30 && added.Layer == 96,
            "W11 actual Boundary Place uses the existing LayerPlanner and commits exactly one preflighted overlay");
        Assert(source.Frame == 13 && source.Length == 8 && source.Layer == 95 && added.Remark == "cut source" && a.Remark == "earlier item" && b.Remark == "later item",
            "W11 source, selected targets and user remarks remain unchanged");
        var placed = Signature(timeline); await undo.UndoAsync(); await Idle();
        Assert(Signature(timeline) == stable, "W11 boundary addition is one native Undo");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "W11 boundary addition is one native Redo");
        await undo.UndoAsync(); await Idle(); timeline.SelectedItems = [a, b];
        Assert(BoundaryProfile.Span(new IItem[] { a, b }, preset) == BoundaryProfile.Span(new IItem[] { b, a }, preset), "W11 selected-pair ordering does not change the cut");
        foreach (var delta in new[] { -3, 0, 3 })
        {
            b.Frame = 5040 + delta;
            var candidate = SelectionPlacement.Create(timeline, entry, preset with { Tolerance = 3 });
            Assert(candidate.Item.Frame == 5025 && candidate.Item.Length == 30 && candidate.Item.Layer == 96,
                $"W11 tolerance includes signed boundary difference {delta} and retains the earlier exclusive end");
        }
        foreach (var delta in new[] { -4, 4, 30 })
        {
            b.Frame = 5040 + delta;
            RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { Tolerance = 3 }),
                $"W11 difference {delta} outside tolerance never uses a nearby unselected cut");
        }
        b.Frame = a.Frame;
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { Tolerance = 100 }), "W11 same-start items are ambiguous even with a generous tolerance");
        b.Frame = 5040;
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { StartOffset = int.MinValue }), "W11 negative result start rejects");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { Duration = int.MaxValue }), "W11 result end overflow rejects");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { Duration = 0 }), "W11 zero duration rejects");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { Layer = preset.Layer with { Maximum = 95 } }), "W11 exhausted boundary Layer band does not move or shorten existing items");
        timeline.SelectedItems = [a]; RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W11 one item cannot supply a pair boundary");
        timeline.SelectedItems = [a, b, distractor];
        Assert(vm.SelectionProfiles.All(x => x.Value != SelectionProfile.Boundary), "W11 three items do not offer Boundary");
        RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W11 three items are not silently narrowed to a pair");
        timeline.SelectedItems = []; RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W11 empty selection rejects");
        timeline.SelectedItems = [a, b];
        var path = Path.Combine(output, "selection-w10-migration.json");
        var old = new PlacerSettings { SelectionPresetRevision = 1, SelectionPresets = [SelectionPreset.Companion, SelectionPreset.Emphasis, SelectionPreset.Range with { HeadPadding = 11 }], CurrentSelectionPresetId = SelectionPreset.Range.Id };
        var text = JsonSerializer.Serialize(old); File.WriteAllText(path, text);
        var migrated = new PlacerSettingsStore(path).Load();
        Assert(migrated.SelectionPresetRevision == 2 && migrated.SelectionPresets.Count == 4 && migrated.CurrentSelectionPresetId == old.CurrentSelectionPresetId &&
            migrated.SelectionPresets.Single(x => x.Id == SelectionPreset.Range.Id).HeadPadding == 11 && File.ReadAllText(path) == text,
            "W11 W10 settings gain Boundary without replacing existing range parameters or writing on load");
        vm.DeleteSelectionPreset(); RejectWithoutMutation(timeline, vm.DeleteSelectionPreset, "W11 last Boundary preset cannot be removed");
        timeline.SelectedItems = [a]; vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.TargetCompanion);
        timeline.SelectedItems = []; vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        undo.Record(); timeline.Items = timeline.Items.RemoveAll(x => ReferenceEquals(x, a) || ReferenceEquals(x, b) || ReferenceEquals(x, distractor) || ReferenceEquals(x, blocker)); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        ItemSettings.Default.Templates.Remove(template); vm.Refresh(); await Idle();
        Assert(Signature(timeline) == original, "W11 cleanup restores all unrelated native items");
        Log("W11=PASS");
    }
}
