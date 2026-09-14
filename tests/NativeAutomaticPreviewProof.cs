using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyAutomaticPreview(Timeline timeline, UndoRedoManager undo)
    {
        stage = "WUX6 automatic preview and feedback";
        var vm = ViewModel!; var view = View!; var panel = view.SelectionSurface;
        timeline.SelectedItems = [];
        var original = Signature(timeline); var originalItems = timeline.Items;
        var target = new TextItem { Frame = 9000, Length = 30, Layer = 1, Remark = "UX6 target" };
        var character = new Character { Name = "UX6 measurement" };
        var voices = Enumerable.Range(0, 204).Select(i => new VoiceItem(character)
            { Frame = 10000 + i * 12, Length = 5, Layer = 1, Serif = "native preview cost fixture " + i }).ToArray();
        var source = new TextItem { Length = 9, Layer = 130, Remark = "UX6 source" };
        var template = Template("UX6/Highlight", [source]); ItemSettings.Default.Templates.Add(template);
        undo.Record(); timeline.Items = timeline.Items.Add(target).AddRange(voices); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        vm.Refresh(); vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "ひかり"; var entry = vm.RegisterLibrary();
        timeline.SelectedItems = [target]; ShowTask(view, "selection");
        vm.SelectedSelectionProfile = vm.SelectionProfiles.Single(x => x.Value == SelectionProfile.TargetCompanion);
        panel.TemplateSelector.SelectedItem = vm.SelectionTemplates.Single(x => x.Id == entry.Id); await Idle();
        Assert(vm.AutomaticPreviewCount > 0 && vm.SelectionPreview.Contains("9000", StringComparison.Ordinal) && vm.Status == "",
            "WUX6 real Template selection automatically displays planned geometry without a Preview click or global success");
        var stable = Signature(timeline); var saved = File.ReadAllText(PlacerSettingsStore.DefaultPath);
        var count = vm.AutomaticPreviewCount;
        for (var i = 0; i < 8; i++) vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id);
        await Idle();
        Assert(vm.AutomaticPreviewCount == count + 1 && Signature(timeline) == stable && File.ReadAllText(PlacerSettingsStore.DefaultPath) == saved,
            "WUX6 repeated same-turn input events coalesce to one mutation-free calculation");
        count = vm.AutomaticPreviewCount; await Task.Delay(200); await Idle();
        Assert(vm.AutomaticPreviewCount == count, "WUX6 idle time never polls or repeats a calculation");
        var samples = new List<double>();
        for (var i = 0; i < 24; i++)
        {
            var watch = Stopwatch.StartNew(); var plan = SelectionPlacement.Create(timeline, entry, vm.SelectedSelectionPreset!); watch.Stop();
            samples.Add(watch.Elapsed.TotalMilliseconds);
            if (plan.Item.Frame != target.Frame) throw new InvalidOperationException("Measurement planned the wrong target.");
        }
        var ordered = samples.OrderBy(x => x).ToArray(); var p95 = ordered[(int)Math.Ceiling(ordered.Length * .95) - 1];
        File.WriteAllText(Path.Combine(output, "ux-preview-cost.json"), JsonSerializer.Serialize(new
        {
            host = "YMM4 4.55.1.1 Lite", added_voice_items = voices.Length, total_items = timeline.Items.Count,
            iterations = samples.Count, average_ms = samples.Average(), p95_ms = p95, max_ms = samples.Max(),
            trigger = "active Selection task, coalesced input events only", polling = false, real_user_assets_tested = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        Assert(p95 < 50 && Signature(timeline) == stable && File.ReadAllText(PlacerSettingsStore.DefaultPath) == saved,
            "WUX6 native synthetic 204-Voice planning p95 is below 50ms and performs no Timeline/settings writes");
        ShowTask(view, "palette"); count = vm.AutomaticPreviewCount;
        timeline.SelectedItems = []; timeline.SelectedItems = [target]; vm.SelectionTemplate = vm.SelectionTemplates.Single(x => x.Id == entry.Id); await Idle();
        Assert(vm.AutomaticPreviewCount == count, "WUX6 other tasks do not spend work computing invisible previews");
        ShowTask(view, "selection"); await Idle();
        Assert(vm.AutomaticPreviewCount > count && vm.SelectionPreview.Contains("9000", StringComparison.Ordinal), "WUX6 returning to Selection refreshes the current live plan");
        ItemSettings.Default.Templates.Remove(template);
        timeline.SelectedItems = []; timeline.SelectedItems = [target]; await Idle();
        Assert(vm.SelectionPreview.Contains("元テンプレート", StringComparison.Ordinal) && !vm.HasError && vm.Status == "" && Signature(timeline) == stable,
            "WUX6 a missing strict source becomes inline preview failure, not mutation or an invented success");
        ItemSettings.Default.Templates.Add(template);
        vm.SelectionDraft.StartOffset = "bad"; vm.SaveSelectionPresetCommand.Execute(null); await Idle();
        var error = vm.Status; count = vm.AutomaticPreviewCount;
        Assert(vm.HasError && vm.SelectionDraft.StartOffset == "bad" && File.ReadAllText(PlacerSettingsStore.DefaultPath) == saved,
            "WUX6 failed input validation retains the user's draft and saved settings");
        ShowTask(view, "palette"); ShowTask(view, "expression"); await Idle();
        Assert(vm.HasError && vm.Status == error, "WUX6 an unresolved error survives task navigation");
        ShowTask(view, "selection"); await Idle();
        Assert(vm.AutomaticPreviewCount == count && !panel.PlaceSelectionButton.IsEnabled, "WUX6 dirty drafts suppress automatic planning and placement");
        vm.RevertSelectionPresetCommand.Execute(null); await Idle();
        Assert(vm.HasError && vm.Status == error && vm.SelectionPreview.Contains("9000", StringComparison.Ordinal),
            "WUX6 automatic preview recovery does not erase a prior actionable error");
        await InvokeSelectionButton(panel.PreviewSelectionButton);
        Assert(!vm.HasError && vm.Status.Length > 0, "WUX6 an explicit successful action replaces the resolved error");
        ShowTask(view, "palette"); await Idle(); Assert(vm.Status == "", "WUX6 success is cleared on task change without a notification timer");
        ShowTask(view, "selection"); await Idle();
        var oldExpression = vm.SelectedExpressionPreset!.Id;
        vm.CopyExpressionPreset(); vm.ExpressionDraft.Name = "UX6 unrelated condition"; vm.SaveExpressionPreset();
        vm.DeleteExpressionPreset(); vm.SelectedExpressionPreset = vm.ExpressionPresets.Single(x => x.Id == oldExpression); await Idle();
        Assert(panel.TemplateSelector.SelectedItem is LibraryEntryView selected && selected.Id == entry.Id,
            "WUX6 unrelated settings saves preserve the actual selected Template rather than clearing its picker");
        // No moving-item subscription is required: Place always makes a fresh plan, not a cached preview commit.
        undo.Record(); target.Frame = 9011; undo.Record(); var beforePlacement = Signature(timeline); var beforeItems = timeline.Items;
        await InvokeSelectionButton(panel.PlaceSelectionButton);
        var added = timeline.Items.Except(beforeItems).Single();
        Assert(!vm.HasError && added.Frame == 9011 && added.Length == 30 && source.Length == 9,
            "WUX6 Place re-plans the live target even when it moved after the displayed preview");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == beforePlacement, "WUX6 auto-preview task placement still has one-step native Undo");
        undo.Record(); target.Frame = 9000; undo.Record(); timeline.SelectedItems = [target];
        ShowTask(view, "palette"); ShowTask(view, "selection"); await Idle();
        SaveNamedView(view, "ux-auto-preview-normal.png");
        var width = view.Width; var height = view.Height;
        try
        {
            view.Width = 360; view.Height = 320; await Idle(); Descendant<ScrollViewer>(panel)?.ScrollToTop(); await Idle();
            Assert(WithinView(panel.TemplateSelector, view) && WithinView(panel.ProfileSelector, view) && WithinView(panel.PlaceSelectionButton, view),
                "WUX6 automatic preview and cleared feedback leave what/where/Place usable at 360px");
            SaveNamedView(view, "ux-auto-preview-narrow.png");
        }
        finally { view.Width = width; view.Height = height; await Idle(); }
        // Real partial Resync is retained; this is not a fabricated message test.
        var face = timeline.Items.OfType<TachieFaceItem>().First(x => AssociationTag.Source(x.Remark, out _) == AssociationTagState.Valid);
        AssociationTag.Source(face.Remark, out var tag);
        var voice = timeline.Items.OfType<VoiceItem>().Single(x => ReferenceEquals(x.Character, face.Character) &&
            AssociationTag.Voice(x.Remark, out var id) == AssociationTagState.Valid && id == tag!.Serial);
        var remark = voice.Remark; undo.Record(); voice.Remark = "UX6 deliberately missing relation"; undo.Record();
        timeline.SelectedItems = [face]; ShowTask(view, "expression"); var partial = vm.Resync(); var partialMessage = vm.Status;
        Assert(partial.Skipped.Count == 1 && partial.Plan.UpdateCount == 0 && !vm.HasError, "WUX6 a genuine partial Resync reports the skip rather than claiming full success");
        ShowTask(view, "palette"); ShowTask(view, "selection"); await Idle();
        Assert(vm.Status == partialMessage && vm.Status.Contains("スキップ", StringComparison.Ordinal), "WUX6 partial result counts/reasons survive task changes and automatic previews");
        undo.Record(); voice.Remark = remark; undo.Record();
        ShowTask(view, "library"); await Idle();
        var library = view.LibrarySurface;
        library.SourceCombo.SelectedItem = vm.SourceTemplates.First(x => x.Items.Count == 1 && x.Items[0] is TachieFaceItem f && f.CharacterName == "TestA"); await Idle();
        Assert(vm.SelectedLibraryCharacter?.Name == "TestA" && library.CharacterSummary.Text.Contains("自動", StringComparison.Ordinal) && !library.CharacterOverrideEditor.IsExpanded,
            "WUX6 management reads a unique source Character and does not require another basic-path selection");
        library.SourceCombo.SelectedItem = template; await Idle();
        Assert(vm.SelectedLibraryCharacter?.Name == null && library.CharacterSummary.Text.Contains("指定なし", StringComparison.Ordinal), "WUX6 a neutral source retains its no-Character capability");
        library.CharacterOverrideEditor.IsExpanded = true;
        library.CharacterOverrideSelector.SelectedItem = vm.LibraryCharacters.Single(x => x.Name == "TestA"); await Idle();
        Assert(vm.SelectedLibraryCharacter?.Name == "TestA" && library.CharacterSummary.Text.Contains("指定）", StringComparison.Ordinal), "WUX6 advanced manual Character association remains available through the real bound control");
        library.CharacterOverrideEditor.IsExpanded = false; library.SourceCombo.SelectedItem = vm.SourceTemplates.First(x => x.Items.Count == 1 && x.Items[0] is TachieFaceItem);
        await Idle(); SaveNamedView(view, "ux-character-auto-normal.png");
        timeline.SelectedItems = []; vm.SelectedLibraryEntry = vm.LibraryEntries.Single(x => x.Id == entry.Id); vm.UnregisterLibrary();
        undo.Record(); timeline.Items = originalItems; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        ItemSettings.Default.Templates.Remove(template); vm.Refresh(); ShowTask(view, "expression"); await Idle();
        Assert(Signature(timeline) == original, "WUX6 preview, notification and Character UI proof preserves all original Timeline items");
        Log("WUX6=PASS");
    }
}
