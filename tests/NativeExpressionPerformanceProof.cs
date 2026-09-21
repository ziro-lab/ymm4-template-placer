using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private sealed record ExpressionPerfSizeResult(
        int Voices,
        int TimelineItems,
        long LoadElapsedMs,
        long CaptureMs,
        long PrepareMs,
        long PublishMs,
        int HostCaptures,
        int AssociationIndexBuilds,
        int AssociationItemsParsed,
        int CandidateKeysBuilt,
        int FullRowPublishes,
        int BatchPublishes);

    private static async Task WaitExpressionRows(PlacerViewModel vm, int expectedRows)
    {
        for (var i = 0; i < 500 && (vm.IsExpressionLoading || vm.Rows.Count != expectedRows); i++)
        {
            await Task.Delay(10);
            await Idle();
        }
        Assert(!vm.IsExpressionLoading && vm.Rows.Count == expectedRows,
            $"PERF expression load completed with exact row count {expectedRows}");
    }

    private static string ManagedPerfRemark(long serial, IntentAssociationTag tag)
    {
        var remark = "";
        remark = PluginRemarks.Append(remark, PlacementEngine.Marker);
        remark = PluginRemarks.Append(remark, AssociationTag.SourceLine(serial));
        return PluginRemarks.Append(remark, tag.Line);
    }

    private static async Task VerifyExpressionPerformance(Timeline timeline, UndoRedoManager undo)
    {
        stage = "expression workspace performance";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var view = View!;
        var character = new Character { Name = "PERF Voice" };
        var source = scope.AddTemplate("PERF/expression", new TachieFaceItem(character) { Length = 10, Layer = 4 });
        var target = new IntentTargetContext
        {
            ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
            CharacterName = character.Name
        };
        var set = new IntentPalette(Guid.NewGuid(), "PERF表情", "表情", target, new IntentRelation(),
            [new IntentEntry(source.Id) { UseTemplateDuration = true }])
        { ExpressionCandidates = true };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [source];
        settings.IntentPalettes = [set];
        settings.ExpressionBootstrapComplete = true;
        settings.IntentPaletteRevision = 1;
        settings.LegacyWorkspace = false;

        var seed = new VoiceItem(character) { Frame = 10, Length = 20, Layer = 20, Serif = "seed" };
        scope.Apply(settings, [seed], [seed]);
        await Idle();
        view.PaletteTab.IsSelected = true;
        await Idle();

        var intentSource = IntentExpressionCatalog.Read(settings).Single().IntentSource
            ?? throw new InvalidOperationException("PERF expression source missing.");
        var geometryHash = IntentAssociationTag.Hash(intentSource.Bundle);
        var uiThreadId = Environment.CurrentManagedThreadId;
        var sizeResults = new List<ExpressionPerfSizeResult>();

        foreach (var voiceCount in new[] { 100, 500, 1000 })
        {
            view.PaletteTab.IsSelected = true;
            await Idle();

            var voices = Enumerable.Range(0, voiceCount).Select(i => new VoiceItem(character)
            {
                Frame = 100 + i * 12,
                Length = 8 + (i % 5),
                Layer = 20 + (i % 3),
                Serif = "perf " + i
            }).ToArray();

            var nonVoiceCount = voiceCount * 2;
            var nonVoices = new IItem[nonVoiceCount];
            var associated = Math.Max(1, voiceCount / 100);
            for (var i = 0; i < nonVoiceCount; i++)
            {
                var item = new TextItem
                {
                    Frame = 50 + i * 5,
                    Length = 4 + (i % 3),
                    Layer = 100 + (i % 8),
                    Remark = "perf item " + i
                };
                if (i < associated)
                {
                    var voiceIndex = i * 100;
                    var serial = 500_000L + voiceIndex;
                    var tag = new IntentAssociationTag(Guid.NewGuid(), set.Id, source.Id, 0, 1, geometryHash);
                    voices[voiceIndex].Remark = PluginRemarks.Append(voices[voiceIndex].Remark, AssociationTag.TargetLine(serial));
                    item.Frame = voices[voiceIndex].Frame;
                    item.Remark = ManagedPerfRemark(serial, tag);
                }
                nonVoices[i] = item;
            }

            var allItems = voices.Cast<IItem>().Concat(nonVoices).ToArray();
            var inactiveBefore = vm.ExpressionPerformance;
            var checksBefore = vm.VoiceFreshnessCheckCount;
            timeline.Items = [.. allItems];
            timeline.SelectedItems = [];
            timeline.RefreshTimelineLengthAndMaxLayer();
            await Idle();
            var inactiveAfter = vm.ExpressionPerformance;
            Assert(inactiveAfter.HostCaptures == inactiveBefore.HostCaptures &&
                inactiveAfter.FullRowPublishes == inactiveBefore.FullRowPublishes &&
                inactiveAfter.IncrementalVoiceReconciles == inactiveBefore.IncrementalVoiceReconciles &&
                vm.VoiceFreshnessCheckCount == checksBefore && !vm.IsExpressionLoading,
                $"PERF {voiceCount}: placement task performs zero expression capture/reconcile work");

            var before = vm.ExpressionPerformance;
            var loadWatch = Stopwatch.StartNew();
            view.ExpressionTab.IsSelected = true;
            await Idle();
            await WaitExpressionRows(vm, voiceCount);
            loadWatch.Stop();
            var after = vm.ExpressionPerformance;

            Assert(after.HostCaptures == before.HostCaptures + 1 &&
                after.FullVoiceReconciles == before.FullVoiceReconciles + 1,
                $"PERF {voiceCount}: first expression entry performs one full host/Voice capture");
            Assert(after.AssociationIndexBuilds == before.AssociationIndexBuilds + 1 &&
                after.AssociationItemsParsed - before.AssociationItemsParsed == allItems.Length,
                $"PERF {voiceCount}: association parsing is one Timeline pass, not Voice x Timeline");
            Assert(after.CandidateKeyBuilds == before.CandidateKeyBuilds + 1,
                $"PERF {voiceCount}: candidate indexing scales with one distinct Character key");
            Assert(after.FullRowPublishes == before.FullRowPublishes + 1 &&
                after.BatchCollectionPublishes == before.BatchCollectionPublishes + 1,
                $"PERF {voiceCount}: full expression load publishes Rows once");
            Assert(after.LastCaptureThreadId == uiThreadId && after.LastPublishThreadId == uiThreadId &&
                after.LastPrepareThreadId != 0 && after.LastPrepareThreadId != uiThreadId,
                $"PERF {voiceCount}: host capture/publish stay on UI thread and pure preparation runs off-thread");
            Assert(vm.WatchedVoiceCount == voiceCount,
                $"PERF {voiceCount}: one capture supplies the exact current Voice watcher set");

            sizeResults.Add(new(
                voiceCount,
                allItems.Length,
                loadWatch.ElapsedMilliseconds,
                after.LastCaptureMilliseconds,
                after.LastPrepareMilliseconds,
                after.LastPublishMilliseconds,
                after.HostCaptures - before.HostCaptures,
                after.AssociationIndexBuilds - before.AssociationIndexBuilds,
                after.AssociationItemsParsed - before.AssociationItemsParsed,
                after.CandidateKeyBuilds - before.CandidateKeyBuilds,
                after.FullRowPublishes - before.FullRowPublishes,
                after.BatchCollectionPublishes - before.BatchCollectionPublishes));

            var stableRows = vm.Rows.ToArray();
            view.PaletteTab.IsSelected = true;
            await Idle();
            var dormantChecks = vm.VoiceFreshnessCheckCount;
            var dormantIncremental = vm.ExpressionPerformance.IncrementalVoiceReconciles;
            voices[^1].Serif += " inactive";
            await Idle();
            Assert(vm.VoiceFreshnessCheckCount == dormantChecks &&
                vm.ExpressionPerformance.IncrementalVoiceReconciles == dormantIncremental,
                $"PERF {voiceCount}: dormant Voice subscriptions do zero expression work outside the expression task");

            // Restore the value before the unchanged re-entry proof.
            voices[^1].Serif = "perf " + (voiceCount - 1);
            var reentryBefore = vm.ExpressionPerformance;
            view.ExpressionTab.IsSelected = true;
            await Idle();
            await WaitExpressionRows(vm, voiceCount);
            var reentryAfter = vm.ExpressionPerformance;
            Assert(reentryAfter.HostCaptures == reentryBefore.HostCaptures + 1 &&
                reentryAfter.AssociationIndexBuilds == reentryBefore.AssociationIndexBuilds &&
                reentryAfter.FullRowPublishes == reentryBefore.FullRowPublishes &&
                reentryAfter.BatchCollectionPublishes == reentryBefore.BatchCollectionPublishes &&
                vm.Rows.SequenceEqual(stableRows),
                $"PERF {voiceCount}: unchanged tab re-entry validates once without rebuilding/indexing Rows");

            if (voiceCount == 1000)
            {
                var editVoice = voices[1];
                var editRow = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, editVoice));
                var editBefore = vm.ExpressionPerformance;
                editVoice.Serif += " live";
                await Idle();
                var editAfter = vm.ExpressionPerformance;
                Assert(ReferenceEquals(editRow, vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, editVoice))) &&
                    editAfter.IncrementalVoiceReconciles == editBefore.IncrementalVoiceReconciles + 1 &&
                    editAfter.HostCaptures == editBefore.HostCaptures &&
                    editAfter.FullRowPublishes == editBefore.FullRowPublishes &&
                    editAfter.BatchCollectionPublishes == editBefore.BatchCollectionPublishes,
                    "PERF 1000: one Serif edit updates one Voice without host scan or collection Reset");

                var frameBefore = vm.ExpressionPerformance;
                editVoice.Frame = voices.Max(x => x.Frame) + 100;
                await Idle();
                var frameAfter = vm.ExpressionPerformance;
                Assert(ReferenceEquals(vm.Rows[^1], editRow) &&
                    frameAfter.IncrementalVoiceReconciles == frameBefore.IncrementalVoiceReconciles + 1 &&
                    frameAfter.HostCaptures == frameBefore.HostCaptures &&
                    frameAfter.FullRowPublishes == frameBefore.FullRowPublishes &&
                    frameAfter.BatchCollectionPublishes == frameBefore.BatchCollectionPublishes,
                    "PERF 1000: one Frame edit reorders the existing Row without full capture or Reset");

                var getterBefore = vm.ExpressionPerformance;
                for (var i = 0; i < 200; i++)
                {
                    _ = vm.Summary;
                    _ = vm.ShowExpressionBatchPlace;
                    _ = vm.PlaceCommand.CanExecute(null);
                }
                var getterAfter = vm.ExpressionPerformance;
                Assert(getterAfter.HostCaptures == getterBefore.HostCaptures &&
                    getterAfter.AssociationIndexBuilds == getterBefore.AssociationIndexBuilds &&
                    getterAfter.IncrementalVoiceReconciles == getterBefore.IncrementalVoiceReconciles,
                    "PERF 1000: repeated Summary/CanExecute reads trigger no Timeline or association work");

                var oldVoice = voices[2];
                var replacement = new VoiceItem(character)
                {
                    Frame = oldVoice.Frame,
                    Length = oldVoice.Length,
                    Layer = oldVoice.Layer,
                    Serif = oldVoice.Serif
                };
                var replaceBefore = vm.ExpressionPerformance;
                timeline.Items = timeline.Items.Replace(oldVoice, replacement);
                await Idle();
                await WaitExpressionRows(vm, voiceCount);
                var replaceAfter = vm.ExpressionPerformance;
                Assert(replaceAfter.HostCaptures == replaceBefore.HostCaptures + 1 &&
                    replaceAfter.AssociationIndexBuilds == replaceBefore.AssociationIndexBuilds + 1 &&
                    replaceAfter.FullRowPublishes == replaceBefore.FullRowPublishes + 1 &&
                    vm.Rows.Any(x => ReferenceEquals(x.Target.Voice, replacement)) &&
                    !vm.Rows.Any(x => ReferenceEquals(x.Target.Voice, oldVoice)),
                    "PERF 1000: one Voice identity replacement uses one coalesced full capture and exact row replacement");
            }

            view.PaletteTab.IsSelected = true;
            await Idle();
        }

#if YMM4_PROOF
        // Hold the pure worker to prove that the UI can leave expression immediately and
        // that a canceled/stale generation never publishes afterward.
        var completedRows = vm.Rows.ToArray();
        using var entered = new ManualResetEventSlim(false);
        using var gate = new ManualResetEventSlim(false);
        ExpressionPreparation.ProofPrepareEntered = entered;
        ExpressionPreparation.ProofPrepareGate = gate;
        try
        {
            var cancelBefore = vm.ExpressionPerformance;
            view.ExpressionTab.IsSelected = true;
            await Idle();
            for (var i = 0; i < 200 && !entered.IsSet; i++) { await Task.Delay(10); await Idle(); }
            Assert(entered.IsSet && vm.IsExpressionLoading,
                "PERF cancel: background expression preparation is actually in flight");
            view.PaletteTab.IsSelected = true;
            await Idle();
            Assert(view.PaletteTab.IsSelected && !vm.IsExpressionLoading,
                "PERF cancel: another Tool tab remains selectable while expression preparation is running");
            gate.Set();
            await Task.Delay(50);
            await Idle();
            var cancelAfter = vm.ExpressionPerformance;
            Assert(vm.Rows.SequenceEqual(completedRows) &&
                cancelAfter.FullRowPublishes == cancelBefore.FullRowPublishes &&
                cancelAfter.BatchCollectionPublishes == cancelBefore.BatchCollectionPublishes &&
                cancelAfter.CancelledLoads + cancelAfter.StaleResultsDiscarded >
                    cancelBefore.CancelledLoads + cancelBefore.StaleResultsDiscarded,
                "PERF cancel: canceled/stale generation cannot publish Rows");
        }
        finally
        {
            gate.Set();
            ExpressionPreparation.ProofPrepareGate = null;
            ExpressionPreparation.ProofPrepareEntered = null;
        }
#endif

        var manifest = new
        {
            schema = "YMM4-Template-Placer-Expression-Performance/1",
            version = "0.4.2",
            host = "YMM4 4.55.1.1 Lite",
            result = "PASS",
            sizes = sizeResults,
            checks = new[]
            {
                "lazy-outside-expression",
                "one-host-capture",
                "linear-association-index",
                "character-key-candidate-index",
                "one-batch-publish",
                "ui-worker-thread-boundary",
                "unchanged-reentry-no-rebuild",
                "single-voice-incremental",
                "cached-summary-command",
                "latest-wins-cancel"
            }
        };
        File.WriteAllText(Path.Combine(output, "expression-performance.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var item in sizeResults)
            Log($"PERF size={item.Voices}; items={item.TimelineItems}; loadMs={item.LoadElapsedMs}; captureMs={item.CaptureMs}; prepareMs={item.PrepareMs}; publishMs={item.PublishMs}; captures={item.HostCaptures}; parsed={item.AssociationItemsParsed}; keys={item.CandidateKeysBuilt}; rowPublishes={item.FullRowPublishes}; batch={item.BatchPublishes}");
        Log("EXPRESSION_PERFORMANCE=PASS");
    }
}
