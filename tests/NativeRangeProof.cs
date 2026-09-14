using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifySelectionRange(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W10 Selection Range";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var original = Signature(timeline);
        var ca = new Character { Name = "RangeA" }; var cb = new Character { Name = "RangeB" };
        var a = new VoiceItem(ca) { Frame = 4000, Length = 40, Layer = 1, Remark = "range A user note" };
        var b = new VoiceItem(cb) { Frame = 4100, Length = 50, Layer = 2, Remark = "range B user note" };
        var c = new TextItem { Frame = 4070, Length = 120, Layer = 3, Remark = "middle starts earlier but ends latest" };
        var blocker = new TextItem { Frame = 4150, Length = 25, Layer = 90, Remark = "range late blocker" };
        var source = new TextItem { Frame = 7, Length = 12, Layer = 90, Remark = "neutral overlay" };
        var template = Template("W10/RangeOverlay", [source]); ItemSettings.Default.Templates.Add(template);
        undo.Record();
        foreach (var item in new IItem[] { a, b, c, blocker }) Assert(timeline.TryAddItems([item], item.Frame, item.Layer), "W10 native fixture insertion");
        undo.Record(); vm.Refresh(); vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "範囲の飾り";
        var entry = vm.RegisterLibrary(); vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id);
        timeline.SelectedItems = [b, a, c]; view.MainTabs.SelectedIndex = 1; await Idle();
        Assert(vm.SelectionProfiles.Count == 1 && vm.SelectionProfiles[0].Value == SelectionProfile.SelectionRange,
            "W10 three selected native items offer the Range profile only");
        vm.SelectedSelectionProfile = vm.SelectionProfiles[0]; vm.CopySelectionPreset();
        vm.SelectionDraft.Name = "前10・後20の範囲"; vm.SelectionDraft.HeadPadding = "10"; vm.SelectionDraft.TailPadding = "20";
        vm.SelectionDraft.UseTemplateLayer = false; vm.SelectionDraft.Minimum = "90"; vm.SelectionDraft.Maximum = "92"; vm.SelectionDraft.Preferred = "90";
        var surface = view.SelectionSurface; surface.SelectionPresetEditor.IsExpanded = true; await Idle();
        Assert(surface.RangePaddingFields.IsVisible && vm.SelectionPresetDirty && !vm.PlaceSelectionCommand.CanExecute(null), "W10 actual range padding editor is visible and unsaved changes disable placement");
        await InvokeSelectionButton(surface.SaveSelectionPresetButton);
        var preset = vm.SelectedSelectionPreset!;
        var loaded = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(loaded.SelectionPresets.Single(x => x.Id == preset.Id) == preset && preset.HeadPadding == 10 && preset.TailPadding == 20,
            "W10 saved range padding and current preset persist independently");
        surface.SelectionPresetEditor.IsExpanded = false; await Idle();
        var stable = Signature(timeline); var before = timeline.Items;
        await InvokeSelectionButton(surface.PreviewSelectionButton);
        Assert(Signature(timeline) == stable && vm.SelectionPreview.Contains("3990", StringComparison.Ordinal), "W10 actual range preview does not mutate native items");
        var planned = SelectionPlacement.Create(timeline, entry, preset);
        Assert(planned.Plan.Count == 1 && planned.Item.Frame == 3990 && planned.Item.Length == 220 && planned.Item.Layer == 91,
            "W10 union min start and max end plus padding produce one overlay with full-span late-blocker avoidance");
        timeline.SelectedItems = [c, a, b];
        var reverse = SelectionPlacement.Create(timeline, entry, preset);
        Assert((reverse.Item.Frame, reverse.Item.Length, reverse.Item.Layer) == (3990, 220, 91), "W10 result is independent of selection order and last-starting target");
        await InvokeSelectionButton(surface.PlaceSelectionButton);
        var added = timeline.Items.Except(before).Single();
        Assert(!vm.HasError && added is TextItem && added.Frame == 3990 && added.Length == 220 && added.Layer == 91,
            "W10 actual WPF Range Place adds exactly one neutral native overlay across different Characters");
        Assert(a.Remark == "range A user note" && b.Remark == "range B user note" && added.Remark == "neutral overlay" && source.Frame == 7 && source.Length == 12,
            "W10 selected targets and original template remain unmodified with no association allocated");
        var placed = Signature(timeline); await undo.UndoAsync(); await Idle();
        Assert(Signature(timeline) == stable, "W10 one native Undo removes only the range overlay");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "W10 one native Redo restores the same planned range overlay");
        await undo.UndoAsync(); await Idle(); timeline.SelectedItems = [a, b, c];
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { HeadPadding = -1 }), "W10 negative head padding rejects without mutation");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { TailPadding = -1 }), "W10 negative tail padding rejects without mutation");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { HeadPadding = int.MaxValue }), "W10 padding cannot move the start below zero");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { TailPadding = int.MaxValue }), "W10 end overflow rejects before partial addition");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry, preset with { Layer = preset.Layer with { Maximum = 90 } }), "W10 exhausted range Layer band leaves unrelated items intact");
        RejectWithoutMutation(timeline, () => SelectionPlacement.Create(timeline, entry with { CharacterName = ca.Name }, preset), "W10 character-associated entry cannot silently cover incompatible Character targets");
        var stale = SelectionPlacement.Create(timeline, entry, preset); c.Length++;
        RejectWithoutMutation(timeline, () => stale.Plan.Commit(timeline, undo), "W10 changes to any selected target invalidate the preflight plan"); c.Length--;
        timeline.SelectedItems = [a];
        RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W10 one target is not accepted by Range");
        timeline.SelectedItems = [];
        RejectWithoutMutation(timeline, () => vm.PlaceSelection(), "W10 no selection is not accepted by Range");
        timeline.SelectedItems = [a, b, c];
        VerifySelectionPresetMigration(timeline);
        vm.DeleteSelectionPreset();
        RejectWithoutMutation(timeline, vm.DeleteSelectionPreset, "W10 last range preset cannot be removed");
        timeline.SelectedItems = [a];
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.TargetCompanion);
        timeline.SelectedItems = []; vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        undo.Record(); timeline.Items = timeline.Items.RemoveAll(x => ReferenceEquals(x, a) || ReferenceEquals(x, b) || ReferenceEquals(x, c) || ReferenceEquals(x, blocker)); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        ItemSettings.Default.Templates.Remove(template); vm.Refresh(); await Idle();
        Assert(Signature(timeline) == original, "W10 fixture cleanup preserves all original items");
        Log("W10=PASS");
    }
    private static void VerifySelectionPresetMigration(Timeline timeline)
    {
        var path = Path.Combine(output, "selection-w9-migration.json");
        var old = new PlacerSettings { SelectionPresets = [SelectionPreset.Companion with { Name = "user companion" }, SelectionPreset.Emphasis],
            CurrentSelectionPresetId = SelectionPreset.Emphasis.Id, SelectionPresetRevision = 0 };
        var original = JsonSerializer.Serialize(old); File.WriteAllText(path, original);
        var store = new PlacerSettingsStore(path); var loaded = store.Load();
        Assert(loaded.SelectionPresetRevision >= 1 && loaded.CurrentSelectionPresetId == old.CurrentSelectionPresetId &&
            loaded.SelectionPresets.Single(x => x.Id == SelectionPreset.Companion.Id).Name == "user companion" &&
            loaded.SelectionPresets.Count(x => x.Profile == SelectionProfile.SelectionRange) == 1 && File.ReadAllText(path) == original,
            "W10 legacy settings gain only new default profiles in memory while names/current selection and disk bytes are preserved");
        store.Save(loaded);
        Assert(new PlacerSettingsStore(path).Load().SelectionPresets.SequenceEqual(loaded.SelectionPresets), "W10 explicit migrated save reloads without duplicate presets");
        old.SelectionPresetRevision = int.MaxValue; File.WriteAllText(path, JsonSerializer.Serialize(old));
        RejectWithoutMutation(timeline, () => new PlacerSettingsStore(path).Load(), "W10 future selection preset revisions are rejected rather than guessed");
        old.SelectionPresetRevision = 0; old.SelectionPresets.RemoveAt(0); File.WriteAllText(path, JsonSerializer.Serialize(old));
        RejectWithoutMutation(timeline, () => new PlacerSettingsStore(path).Load(), "W10 upgrade never repairs a missing legacy user profile by guessing");
    }
}
