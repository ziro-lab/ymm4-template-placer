using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private sealed class P4Config : TachieCharacterParameterBase
    {
        private string definitions = "//Neutral\nmood=neutral\n//Smile\nmood=smile\n";
        public string PresetDefinitions { get => definitions; set => Set(ref definitions, value); }
    }

    private static async Task VerifyTachiePresetRowIntegration(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P4/P5 row integration";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!;
        var checks = new List<string>();
        var p5Checks = new List<string>();
        void Check(bool ok, string name) { Assert(ok, "TP-P4 " + name); checks.Add(name); }
        void CheckP5(bool ok, string name) { Assert(ok, "TP-P5 " + name); p5Checks.Add(name); }
        var oldResolver = vm.PresetTargetResolver;
        var config = new P4Config();
        var character = new Character { Name = "P4 rows", TachieCharacterParameter = config };
        var broken = new Character { Name = "P4 unavailable" };
        var source = scope.AddTemplate("P4/expression", new TachieFaceItem(character) { Length = 8, Layer = 4 });
        var set = new IntentPalette(Guid.NewGuid(), "P4表情", "表情",
            new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name },
            new(), [new(source.Id)]) { ExpressionCandidates = true };
        var settings = PlacerSettingsStore.Copy(scope.Original);
        settings.Library = [source]; settings.IntentPalettes = [set]; settings.ExpressionBootstrapComplete = true;
        settings.IntentPaletteRevision = 1; settings.LegacyWorkspace = false;
        var voices = Enumerable.Range(0, 1000).Select(i => new VoiceItem(character)
            { Frame = 100 + i * 20, Length = 10, Layer = 20, Serif = "P4 " + i }).ToArray();
        voices[2].Remark = PluginRemarks.Append(voices[2].Remark, "CWT_TPL:V=broken");
        var tag = new IntentAssociationTag(Guid.NewGuid(), set.Id, source.Id, 0, 1, IntentAssociationTag.Hash(TemplateResolver.RequireBundle(source)));
        voices[0].Remark = PluginRemarks.Append(voices[0].Remark, AssociationTag.TargetLine(700000));
        var managed = new TachieFaceItem(character) { Frame = voices[0].Frame, Length = 8, Layer = 4, Remark = ManagedPerfRemark(700000, tag) };
        var useEditor = false;
        vm.PresetTargetResolver = c => c.Name == broken.Name ? throw new InvalidOperationException("P4 local unavailable") :
            new(c, config, typeof(P4Config), () => useEditor ? new P3LegacyFace() : new P3DirectFace(), () => true);
        vm.InvalidatePresetCapabilityCache();
        try
        {
            scope.Apply(settings, voices.Cast<IItem>().Append(managed).ToArray(), []); await Idle();
            var collection = vm.Rows; var originalRows = vm.Rows.ToArray();
            var signature = Signature(timeline); var saved = JsonSerializer.Serialize(scope.Current);
            var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
            bool DiskSame() => disk == null ? !File.Exists(PlacerSettingsStore.DefaultPath) : File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(disk);
            var before = vm.ExpressionPerformance; var scanBefore = vm.PresetCapabilityDiagnostics.CharacterScans;
            vm.IsTachiePresetExpressionSource = true; await Idle();
            Check(vm.PresetCapabilityDiagnostics.CharacterScans == scanBefore && vm.ExpressionPerformance.HostCaptures == before.HostCaptures,
                "source switch while inactive performs zero capability/capture work");
            view.ExpressionTab.IsSelected = true; await vm.ExpressionLoadCompletion; await Idle(); await WaitExpressionRows(vm, 1000);
            Check(vm.ExpressionRowsMatchSource && ReferenceEquals(collection, vm.Rows) && vm.Rows.SequenceEqual(originalRows),
                "one existing collection and the same 1000 Voice rows serve both sources");
            Check(vm.PresetCapabilityDiagnostics.CharacterScans == scanBefore + 1 &&
                vm.ExpressionPerformance.CandidateCatalogBuilds == before.CandidateCatalogBuilds,
                "1000 Voices use one Character discovery and no Template catalog scan");
            Check(vm.ExpressionPerformance.LastPrepareThreadId != Environment.CurrentManagedThreadId &&
                vm.ExpressionPerformance.LastPublishThreadId == Environment.CurrentManagedThreadId,
                "preset descriptors use the existing off-thread preparation and UI publication");
            Check(view.VoiceGrid.IsVisible && ReferenceEquals(view.VoiceGrid.ItemsSource, collection) &&
                vm.Rows.All(r => r.HasCandidates && r.Choices.Count(c => c.TachiePreset != null) == 2 && r.Choices.All(c => c.Template == null)),
                "the existing DataGrid displays real immutable preset choices without fake Templates");
            Check(vm.Rows[0].SelectedChoice.IsCurrentOtherSource && vm.Rows[0].SelectedChoice.IsAvailable &&
                vm.Rows[0].SourceNotice.Contains("テンプレート", StringComparison.Ordinal),
                "valid Template association is explicitly other-source, not corrupt or unselected");
            var noneState = vm.Rows.Single(r => ReferenceEquals(r.Target.Voice, voices[3]));
            var invalidState = vm.Rows.Single(r => ReferenceEquals(r.Target.Voice, voices[2]));
            CheckP5(!noneState.SelectedChoice.HasCandidate && noneState.SelectedChoice.IsAvailable &&
                !noneState.SelectedChoice.IsCurrentOtherSource && !noneState.SelectedChoice.IsInvalidAssociation &&
                noneState.State == "未選択", "none is an explicit source-aware row state");
            CheckP5(invalidState.SelectedChoice.IsInvalidAssociation && !invalidState.SelectedChoice.IsAvailable &&
                invalidState.State == "関連付けを確認", "invalid managed association is explicit and never guessed");
            CheckP5(vm.Rows[0].SelectedChoice.IsCurrentOtherSource && vm.Rows[0].State == "別の元から配置済み",
                "valid current Template association is explicit other-source state in TachiePreset mode");
            Check(view.PresetSurface.IsVisible && view.TachiePresetPlacementRuleLabel.IsVisible && !view.ExcelEditor.IsVisible &&
                vm.ExpressionSourceNotice.Contains("実験候補", StringComparison.Ordinal) &&
                vm.ExpressionSourceNotice.Contains("Preview", StringComparison.Ordinal) &&
                vm.ExpressionSourceNotice.Contains("Excel", StringComparison.Ordinal),
                "placement-rule UI truthfully describes safe trial of Experimental candidates, Preview verification and Template-only Excel");
            SaveNamedView(view, "tachie-preset-p4-inspector.png");
            var row = vm.Rows[1];
            view.PaletteTab.IsSelected = true; await Idle();
            row.SelectedChoice = row.Choices.Single(c => c.TachiePreset?.CandidateIdentity == "Smile");
            await vm.TachiePresetApplyCompletion; await Idle();
            Check(row.SelectedChoice.TachiePreset != null && Signature(timeline) == signature && JsonSerializer.Serialize(scope.Current) == saved && DiskSame(),
                "inactive candidate state can be retained for row-projection proof without mutating Timeline/settings");
            view.ExpressionTab.IsSelected = true; await vm.ExpressionLoadCompletion; await Idle();
            row = vm.Rows.Single(r => ReferenceEquals(r.Target.Voice, voices[1]));
            Check(row.SelectedChoice.TachiePreset?.CandidateIdentity == "Smile",
                "re-entering expression preserves the inactive candidate state without inventing a managed association");
            CheckP5(row.SelectedChoice.TachiePreset?.CandidateIdentity == "Smile" && row.SelectedChoice.IsAvailable &&
                row.State == "選択済み", "TachiePreset is an explicit candidate state without a fake Template");
            RejectWithoutMutation(timeline, () => vm.Place(), "TP-P4 direct Place cannot bypass source admission");
            RejectWithoutMutation(timeline, () => vm.ExportTo(Path.Combine(output, "p4-forbidden.xlsx")), "TP-P4 root Excel export rejects preset source");
            RejectWithoutMutation(timeline, () => IntentExpressionMutation.Create(timeline, row, row.SelectedChoice, scope.Current,
                new IntentAssociationSerialAllocator(timeline, 1), false), "TP-P4 preset choice cannot be interpreted as Template removal");
            RejectWithoutMutation(timeline, () => WorkbookBridge.Export(Path.Combine(output, "p4-forbidden-direct.xlsx"), timeline.Name, vm.Rows.ToArray(), []),
                "TP-P4 direct Workbook API cannot serialize preset choices as empty Templates");
            Check(!File.Exists(Path.Combine(output, "p4-forbidden-direct.xlsx")), "rejected Workbook path writes no output file");
            var changes = 0;
            NotifyCollectionChangedEventHandler onChange = (_, e) => { if (e.Action == NotifyCollectionChangedAction.Reset) changes++; };
            ((INotifyCollectionChanged)vm.Rows).CollectionChanged += onChange;
            try
            {
                var metrics = vm.ExpressionPerformance; var scans = vm.PresetCapabilityDiagnostics.CharacterScans;
                voices[1].Serif += " edited"; await Idle();
                voices[1].Frame = voices[^1].Frame + 100; await Idle();
                Check(ReferenceEquals(vm.Rows[^1], row) && row.SelectedChoice.TachiePreset?.CandidateIdentity == "Smile" && changes == 0 &&
                    vm.ExpressionPerformance.HostCaptures == metrics.HostCaptures && vm.PresetCapabilityDiagnostics.CharacterScans == scans,
                    "Serif/Frame edits preserve row and preset identity with no scan or Reset");
            }
            finally { ((INotifyCollectionChanged)vm.Rows).CollectionChanged -= onChange; }
            signature = Signature(timeline);
            var reentry = vm.PresetCapabilityDiagnostics; var reentryMetrics = vm.ExpressionPerformance;
            view.PaletteTab.IsSelected = true; await Idle(); view.ExpressionTab.IsSelected = true;
            await vm.ExpressionLoadCompletion; await Idle();
            Check(vm.PresetCapabilityDiagnostics.CharacterScans == reentry.CharacterScans && vm.PresetCapabilityDiagnostics.CacheHits > reentry.CacheHits &&
                vm.ExpressionPerformance.FullRowPublishes == reentryMetrics.FullRowPublishes,
                "unchanged re-entry reuses session cache without rebuilding rows");
            var configBefore = vm.PresetCapabilityDiagnostics.CharacterScans;
            config.PresetDefinitions = "//Neutral\nmood=neutral\n"; await Idle(); await vm.ExpressionLoadCompletion; await Idle();
            Check(vm.PresetCapabilityDiagnostics.CharacterScans == configBefore + 1 && !row.SelectedChoice.IsAvailable &&
                row.SelectedChoice.TachiePreset?.CandidateIdentity == "Smile",
                "configuration notification invalidates candidates and preserves an explicit unavailable selection");
            CheckP5(!row.SelectedChoice.IsAvailable && row.SelectedChoice.TachiePreset?.CandidateIdentity == "Smile" &&
                row.State == "選択元を確認", "disappeared same-source preset remains explicit unavailable state");
            config.PresetDefinitions = "//Neutral\nmood=new\n//Smile\nmood=new-smile\n"; await Idle(); await vm.ExpressionLoadCompletion; await Idle();
            Check(!row.SelectedChoice.IsAvailable && row.Choices.Any(c => c.IsAvailable && c.TachiePreset?.CandidateIdentity == "Smile"),
                "same label under changed configuration never silently heals an old descriptor");
            row.SelectedChoice = row.Choices.Single(c => c.IsAvailable && c.TachiePreset?.CandidateIdentity == "Smile");
            Check(row.SelectedChoice.IsAvailable, "explicit new candidate selection resolves unavailable inspection state");
            var unavailableVoice = new VoiceItem(broken) { Frame = 50, Length = 10, Layer = 20 };
            timeline.Items = timeline.Items.Add(unavailableVoice); await Idle(); await vm.ExpressionLoadCompletion; await Idle();
            var missing = vm.Rows.Single(r => ReferenceEquals(r.Target.Voice, unavailableVoice));
            Check(!missing.HasCandidates && missing.SourceNotice.Contains("P4 local unavailable", StringComparison.Ordinal) && vm.Rows.Any(r => r.HasCandidates),
                "one broken Character has a local message and does not disable valid rows");
            signature = Signature(timeline);
            view.PaletteTab.IsSelected = true; await Idle(); useEditor = true;
            vm.InvalidatePresetCapabilityCache(); var heldRows = vm.Rows.ToArray();
            P3EditorFixture.AfterBind = () => view.PaletteTab.IsSelected = true;
            view.ExpressionTab.IsSelected = true; await vm.ExpressionLoadCompletion; await Idle();
            Check(view.PaletteTab.IsSelected && !vm.IsExpressionLoading && vm.PresetCapabilityDiagnostics.ActiveEditors == 0 &&
                P3EditorFixture.Handlers.Count == 0 && vm.Rows.SequenceEqual(heldRows),
                "leaving the real root while bound cancels, clears editors and forbids stale publication");
            P3EditorFixture.AfterBind = null; useEditor = false;
            using var entered = new ManualResetEventSlim(false); using var gate = new ManualResetEventSlim(false);
            ExpressionPreparation.ProofPrepareEntered = entered; ExpressionPreparation.ProofPrepareGate = gate;
            try
            {
                view.ExpressionTab.IsSelected = true; await Idle();
                for (var i = 0; i < 200 && !entered.IsSet; i++) { await Task.Delay(10); await Idle(); }
                Check(entered.IsSet, "preset source reaches the real background worker before source-race test");
                var oldLoad = vm.ExpressionLoadCompletion;
                vm.IsTemplateExpressionSource = true; gate.Set();
                await oldLoad; await vm.ExpressionLoadCompletion; await Idle();
                Check(vm.IsTemplateExpressionSource && vm.ExpressionRowsMatchSource && vm.Rows.All(r => r.Choices.All(c => c.TachiePreset == null)) &&
                    ReferenceEquals(collection, vm.Rows), "latest source wins even when obsolete preset preparation completes later");
            }
            finally { gate.Set(); ExpressionPreparation.ProofPrepareEntered = null; ExpressionPreparation.ProofPrepareGate = null; }
            Check(vm.Rows.Single(r => ReferenceEquals(r.Target.Voice, voices[0])).SelectedChoice.Template != null &&
                Signature(timeline) == signature && JsonSerializer.Serialize(scope.Current) == saved && DiskSame(),
                "returning Template restores the exact managed source and all integration actions are zero-write");
            var templateState = vm.Rows.Single(r => ReferenceEquals(r.Target.Voice, voices[0]));
            CheckP5(templateState.SelectedChoice.Template != null && templateState.SelectedChoice.TachiePreset == null &&
                !templateState.SelectedChoice.IsCurrentOtherSource && templateState.State == "選択済み",
                "Template remains the existing explicit candidate state after returning source");
            SaveNamedView(view, "tachie-preset-p4-template-return.png");
        }
        finally
        {
            P3EditorFixture.AfterBind = null;
            ExpressionPreparation.ProofPrepareEntered = null; ExpressionPreparation.ProofPrepareGate = null;
            view.PaletteTab.IsSelected = true;
            vm.IsTemplateExpressionSource = true;
            await vm.ExpressionLoadCompletion;
            vm.InvalidatePresetCapabilityCache(); vm.PresetTargetResolver = oldResolver;
        }
        File.WriteAllText(Path.Combine(output, "tachie-preset-rows.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Rows/1", host = "YMM4 4.55.1.1 Lite", result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(output, "tachie-preset-choice-model.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Choice-Model/1", host = "YMM4 4.55.1.1 Lite", result = "PASS",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), checks = p5Checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_ROWS_P4=PASS");
        Log("TACHIE_PRESET_CHOICE_MODEL_P5=PASS");
    }
}
