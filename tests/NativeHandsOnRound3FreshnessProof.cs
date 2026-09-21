using System.IO;
using System.Reflection;
using System.Windows;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnRound3Freshness(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-E event-driven Voice freshness and pending-work preservation";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!;
        var character = new Character { Name = "R3 Freshness" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 30, Layer = 20, Serif = "one" };
        var second = new VoiceItem(character) { Frame = 300, Length = 30, Layer = 20, Serif = "two" };
        var source = scope.AddTemplate("R3E/expression", new TachieFaceItem(character) { Length = 10, Layer = 4 });
        var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
        var set = new IntentPalette(Guid.NewGuid(), "表情", "表情", target, new IntentRelation(), [new(source.Id) { UseTemplateDuration = true }]) { ExpressionCandidates = true };
        var settings = PlacerSettingsStore.Copy(scope.Original); settings.Library = [source]; settings.IntentPalettes = [set]; settings.ExpressionBootstrapComplete = true;
        scope.Apply(settings, [voice, second], [voice]); view.ExpressionTab.IsSelected = true; await Idle();
        view.ExpressionMaintenance.IsExpanded = false;
        Round3Assert(view.RefreshButton.Command == vm.RefreshCommand && !view.RefreshButton.IsVisible &&
            view.RefreshButton.Content?.ToString() == "一覧を読み直す", "E1", "normal header has no manual Scene Refresh; guarded reload is retained in collapsed maintenance");
        var third = new VoiceItem(character) { Frame = 500, Length = 25, Layer = 20 };
        timeline.Items = timeline.Items.Add(third); await Idle();
        var added = vm.Rows.Any(x => ReferenceEquals(x.Target.Voice, third));
        timeline.Items = timeline.Items.Remove(third); await Idle();
        Round3Assert(added && vm.Rows.Count == 2 && !vm.Rows.Any(x => ReferenceEquals(x.Target.Voice, third)), "E2", "native Voice collection add/remove automatically rebuilds the current Rows");
        var fieldChecks = true;
        var trackedRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
        foreach (Action edit in new Action[] { () => voice.Frame++, () => voice.Length++, () => voice.Layer++, () => voice.Serif = "edited" })
        {
            var beforePerf = vm.ExpressionPerformance; var rebuildsBefore = vm.AutomaticVoiceRebuildCount;
            edit(); await Idle();
            var currentRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            var snapshot = currentRow.Target; var afterPerf = vm.ExpressionPerformance;
            fieldChecks &= ReferenceEquals(currentRow, trackedRow) &&
                afterPerf.IncrementalVoiceReconciles == beforePerf.IncrementalVoiceReconciles + 1 &&
                afterPerf.FullRowPublishes == beforePerf.FullRowPublishes &&
                vm.AutomaticVoiceRebuildCount == rebuildsBefore &&
                snapshot.Frame == voice.Frame && snapshot.Length == voice.Length &&
                snapshot.Layer == voice.Layer && snapshot.Character == voice.CharacterName && snapshot.Serif == voice.Serif;
        }
        var fullBefore = vm.ExpressionPerformance; voice.Character = new Character { Name = "R3 Freshness other" }; await Idle();
        for (var wait = 0; wait < 200 && vm.IsExpressionLoading; wait++) { await Task.Delay(10); await Idle(); }
        var characterRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
        var fullAfter = vm.ExpressionPerformance;
        fieldChecks &= fullAfter.HostCaptures == fullBefore.HostCaptures + 1 &&
            fullAfter.FullRowPublishes == fullBefore.FullRowPublishes &&
            fullAfter.BatchCollectionPublishes == fullBefore.BatchCollectionPublishes &&
            ReferenceEquals(characterRow, trackedRow) &&
            characterRow.Target.Character == voice.CharacterName;
        voice.Character = character; await Idle();
        for (var wait = 0; wait < 200 && vm.IsExpressionLoading; wait++) { await Task.Delay(10); await Idle(); }
        // A different Voice with identical value fields must still replace its snapshot by reference identity.
        fullBefore = vm.ExpressionPerformance;
        var replacement = new VoiceItem(character) { Frame = second.Frame, Length = second.Length, Layer = second.Layer, Serif = second.Serif };
        timeline.Items = timeline.Items.Replace(second, replacement); await Idle();
        for (var wait = 0; wait < 200 && vm.IsExpressionLoading; wait++) { await Task.Delay(10); await Idle(); }
        fullAfter = vm.ExpressionPerformance;
        fieldChecks &= fullAfter.HostCaptures == fullBefore.HostCaptures + 1 &&
            vm.Rows.Any(x => ReferenceEquals(x.Target.Voice, replacement)) && !vm.Rows.Any(x => ReferenceEquals(x.Target.Voice, second));
        Round3Assert(fieldChecks, "E3", "Frame/Length/Layer/Serif update only the dirty Voice; Character changes reconcile in place and reference-identity changes use one exact full capture");
        var rows = vm.Rows.ToArray(); var rebuilds = vm.AutomaticVoiceRebuildCount;
        var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
        await SelectInDropdown(view, row, "R3E/expression"); await Idle();
        var associated = ManagedIntentExpressionReader.Read(timeline, voice).Bundle != null;
        voice.Remark += "\nuser note"; await Idle();
        Round3Assert(associated && rows.SequenceEqual(vm.Rows) && vm.AutomaticVoiceRebuildCount == rebuilds,
            "E4", "real immediate association insertion and Remark-only edits never reconstruct Voice Rows");
        row.SelectedChoice = row.Choices.First(x => x.Template == null); vm.CloseExpressionTrialSession(); await Idle();
        rows = vm.Rows.ToArray(); rebuilds = vm.AutomaticVoiceRebuildCount;
        for (var i = 0; i < 20; i++) vm.RequestVoiceFreshnessCheck();
        await Idle(); await Task.Delay(80); await Idle();
        Round3Assert(rows.SequenceEqual(vm.Rows) && vm.AutomaticVoiceRebuildCount == rebuilds, "E5", "unchanged signatures and idle time never rebuild Rows");
        var checks = vm.VoiceFreshnessCheckCount; var perfBeforeBurst = vm.ExpressionPerformance;
        voice.Frame++; voice.Length++; voice.Layer++; voice.Serif = "burst";
        for (var i = 0; i < 20; i++) vm.RequestVoiceFreshnessCheck();
        await Idle();
        var perfAfterBurst = vm.ExpressionPerformance;
        Round3Assert(vm.AutomaticVoiceRebuildCount == rebuilds && vm.VoiceFreshnessCheckCount == checks + 1 &&
            perfAfterBurst.IncrementalVoiceReconciles == perfBeforeBurst.IncrementalVoiceReconciles + 1 &&
            perfAfterBurst.FullRowPublishes == perfBeforeBurst.FullRowPublishes,
            "E6", "same-turn Voice notifications coalesce to one freshness check and one dirty-Voice update without a full Rows rebuild");
        // Bounded test fixture injection into our own coordinator, never product reflection or a private host field.
        var timelineField = typeof(PlacerViewModel).GetField("timeline", BindingFlags.Instance | BindingFlags.NonPublic)!;
        DumpType(typeof(Timeline));
        var alternate = (Timeline?)Activator.CreateInstance(typeof(Timeline)) ?? throw new InvalidOperationException("Native Timeline fixture needs its public constructor.");
        var alternateVoice = new VoiceItem(character) { Frame = 20, Length = 10, Layer = 2 };
        alternate.Items = [alternateVoice];
        var rebound = false;
        try
        {
            timelineField.SetValue(vm, alternate); vm.RebindVoiceFreshness(); vm.RequestExpressionLoad(true); await Idle();
            for (var wait = 0; wait < 200 && vm.IsExpressionLoading; wait++) { await Task.Delay(10); await Idle(); }
            checks = vm.VoiceFreshnessCheckCount; voice.Serif = "detached old Voice"; await Idle();
            rebound = !vm.IsExpressionLoading && ReferenceEquals(vm.WatchedVoiceTimeline, alternate) &&
                vm.WatchedVoiceCount == 1 && vm.VoiceFreshnessCheckCount == checks && vm.Rows.Count == 1;
            alternateVoice.Frame++; await Idle(); rebound &= vm.Rows.Single().Frame == alternateVoice.Frame;
        }
        finally { timelineField.SetValue(vm, timeline); vm.RebindVoiceFreshness(); vm.Refresh(); }
        for (var i = 0; i < 3; i++) { view.Visibility = Visibility.Collapsed; view.Visibility = Visibility.Visible; }
        await Idle();
        Round3Assert(rebound && ReferenceEquals(vm.WatchedVoiceTimeline, timeline) && vm.WatchedVoiceCount == 2,
            "E7", "Timeline replacement detaches old Voice events; repeated visible lifecycle retains exactly the current subscriptions");
        view.PaletteTab.IsSelected = true; await Idle(); checks = vm.VoiceFreshnessCheckCount;
        var capturesBeforeReentry = vm.ExpressionPerformance.HostCaptures;
        view.ExpressionTab.IsSelected = true; await Idle();
        for (var wait = 0; wait < 200 && vm.IsExpressionLoading; wait++) { await Task.Delay(10); await Idle(); }
        Round3Assert(vm.ExpressionPerformance.HostCaptures == capturesBeforeReentry + 1 && vm.VoiceFreshnessCheckCount == checks,
            "E8", "entering the expression task performs one current host snapshot without reviving the old global freshness scan");
        var workbook = Path.Combine(output, "round3-pending.xlsx");
        vm.ExportTo(workbook); EditCell(workbook, "F2", "R3E/expression");
        var before = Signature(timeline); vm.ImportFrom(workbook); await Idle();
        var imported = vm.Rows.ToArray(); var importedChoice = imported[0].SelectedChoice; voice.Frame++; await Idle();
        var preserved = vm.ExpressionRowsStale && imported.SequenceEqual(vm.Rows) && ReferenceEquals(importedChoice, vm.Rows[0].SelectedChoice) &&
            !vm.PlaceCommand.CanExecute(null) && !view.VoiceGrid.IsEnabled && vm.ExpressionFreshnessNotice.Contains("保持", StringComparison.Ordinal);
        RejectWithoutMutation(timeline, () => vm.Place(), "R3-E stale pending batch cannot be committed");
        var pending = vm.TakeTransientWork(); var resumeSettings = PlacerSettingsStore.Copy(scope.Current); PlacerViewModel? resumed = null;
        try
        {
            resumed = new PlacerViewModel();
            typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(resumed, resumeSettings);
            timelineField.SetValue(resumed, timeline); resumed.ActivateIntentWorkspace(); resumed.Refresh(); resumed.AcceptTransientWork(pending);
            preserved &= resumed.ExpressionRowsStale && resumed.Rows[0].Target == imported[0].Target &&
                ReferenceEquals(resumed.Rows[0].SelectedChoice.Template, importedChoice.Template) && !ReferenceEquals(resumed.Rows[0], imported[0]);
        }
        finally { resumed?.Dispose(); ViewModel = vm; }
        Round3Assert(preserved, "E9", "Excel pending work survives Voice edits and fresh coordinator resume as exact stale copies; recovery is explicit, never silent discard");
        vm.Refresh(); await Idle();
        Round3Assert(!vm.ExpressionRowsStale && !view.ExpressionEmptyNotice.Text.Contains("シーン更新", StringComparison.Ordinal) &&
            view.ExpressionEmptyNotice.Text.Contains("自動", StringComparison.Ordinal) && !vm.ExpressionPlaceHint.Contains("シーン更新", StringComparison.Ordinal),
            "E10", "explicit recovery rebuilds current Rows and empty/help copy describes automatic Voice discovery");
        if (File.Exists(workbook)) File.Delete(workbook);
        SaveNamedView(view, "v042-round3-freshness-360.png");
        Round3Phase("E", "hands-on-round3-freshness.json"); Log("HANDS_ON_ROUND3_E=PASS");
    }
}
