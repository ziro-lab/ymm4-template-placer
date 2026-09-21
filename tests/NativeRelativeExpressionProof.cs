using System.IO;
using System.Reflection;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyRelativeExpressions(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R10/R13 relative expression integration";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var before = timeline.Items; var selection = timeline.SelectedItems; var mode = vm.UseLegacyWorkspace;
        var character = new Character { Name = "R10 Logical" }; var detached = new Character { Name = character.Name };
        var voice = new VoiceItem(character) { Frame = 100, Length = 50, Layer = 20 };
        var nextVoice = new VoiceItem(character) { Frame = 240, Length = 40, Layer = 20 };
        var face = new TachieFaceItem(detached) { Frame = 30, Length = 20, Layer = 5, Remark = "user face remark" };
        var text = new TextItem { Frame = 40, Length = 12, Layer = 6, Remark = "user text remark" };
        var source = Template("R10/Bundle", [face, text]); var single = Template("R10/Single", [new TachieFaceItem(detached) { Length = 10 }]);
        ItemSettings.Default.Templates.Add(source); ItemSettings.Default.Templates.Add(single);
        try
        {
            var bundleEntry = TemplateResolver.Reference(source, "笑顔", character.Name);
            var singleEntry = TemplateResolver.Reference(single, "笑顔", character.Name);
            var bundleTile = new IntentEntry(bundleEntry.Id) { UseTemplateDuration = true }; var singleTile = new IntentEntry(singleEntry.Id);
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var relation = new IntentRelation { Duration = IntentDuration.UntilRelated, Neighbor = IntentNeighbor.NextSameTypeAndCharacter };
            var first = new IntentPalette(Guid.NewGuid(), "セット1", "表情", target, relation, [bundleTile]) { ExpressionCandidates = true };
            var second = first with { Id = Guid.NewGuid(), Name = "セット2", Entries = [singleTile, bundleTile] };
            var fixture = PlacerSettingsStore.Copy(original); fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true;
            fixture.IntentPalettes = [first, second]; fixture.Library = [bundleEntry, singleEntry]; fixture.LegacyWorkspace = false;
            field.SetValue(vm, fixture); timeline.Items = [voice, nextVoice]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.Refresh(); view.ExpressionTab.IsSelected = true; await Idle();
            Assert(!view.PresetSurface.IsVisible && vm.UsesRelativeExpressions, "R10 normal expression assignment does not ask for an independent profile/layer decision");
            var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            Assert(row.Choices.Count == 3 && row.Choices[1].Template!.Name == source.Name && row.Choices[2].Template!.Name == single.Name,
                "R10 candidate union follows Palette and entry order, deduplicating repeated source membership");
            Assert(row.Choices.Skip(1).Select(x => x.DisplayName).Distinct().Count() == 2 && !ReferenceEquals(row.Choices[1].Template!.Face.Character, voice.Character),
                "R10 same-name detached Character candidates work and duplicate aliases receive only needed set/source context");
            var baseline = Signature(timeline); var sourceBaseline = (face.Frame, face.Length, face.Layer, face.Remark, text.Frame, text.Layer);
            await SelectInDropdown(view, row, source.Name); await Idle();
            var immediateMembers = timeline.Items.Where(x => x != voice && x != nextVoice).ToArray();
            Assert(immediateMembers.Length == 2 && ManagedIntentExpressionReader.Read(timeline, voice).Bundle?.Members.Count == 2,
                "R10 selecting a relative expression immediately applies exactly one managed whole bundle");
            Assert(IntentExecutionPlan.Create(timeline, first, bundleTile, fixture.Library).Count == 2,
                "R10 tile path accepts the same complete bundle as expression assignment");
            var excel = Path.Combine(output, "r10-relative-expression.xlsx"); vm.ExportTo(excel);
            await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == baseline, "R10 closing the trial before Excel export leaves one native Undo back to the pre-trial state");
            vm.ImportFrom(excel); row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            Assert(row.SelectedChoice.Template?.IntentSource?.Entry.LibraryEntryId == bundleEntry.Id && Signature(timeline) == baseline && vm.PlaceCommand.CanExecute(null),
                "R10 Excel Bridge import restores a pending Palette-backed assignment without Timeline mutation");
            await Idle();
            await ClickPlace(view); Assert(!vm.HasError, "R10 Excel pending assignment commits through the retained batch bridge: " + vm.Status);
            var members = timeline.Items.Where(x => x != voice && x != nextVoice).OrderBy(x => { IntentAssociationTag.Read(x.Remark, out var tag); return tag?.Index ?? int.MaxValue; }).ToArray();
            Assert(members.Length == 2 && members[0].Frame == 100 && members[1].Frame == 110 && members[0].Length == 20 && members[1].Length == 12 && members[1].Layer - members[0].Layer == 1,
                "R10 expression placement preserves complete bundle frame/layer/duration geometry");
            Assert(members.All(x => IntentAssociationTag.Read(x.Remark, out _) == AssociationTagState.Valid) && AssociationTag.Voice(voice.Remark, out _) == AssociationTagState.Valid &&
                sourceBaseline == (face.Frame, face.Length, face.Layer, face.Remark, text.Frame, text.Layer), "R13 complete generated membership is identifiable; source templates remain unchanged");
            var placed = Signature(timeline);
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == baseline, "R13 one native Undo removes every expression bundle member and restores Voice remark");
            await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == placed, "R13 native Redo restores the exact whole associated bundle");
            undo.Record(); voice.Frame = 140; undo.Record(); timeline.SelectedItems = [members[1]];
            var beforeResync = Signature(timeline); var result = vm.Resync(); await Idle();
            Assert(result.Skipped.Count == 0 && result.Plan.UpdateCount == 2 && members[0].Frame == 140 && members[1].Frame == 150,
                "R13 selecting a non-Face member resyncs the entire bundle against saved Palette relation");
            Assert(members[0].Remark.Contains("user face remark") && members[1].Remark.Contains("user text remark"), "R13 Resync preserves all existing member contents and user remarks");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == beforeResync, "R13 one native Undo restores every updated bundle member");
            var duplicate = members[1].GetClone(); duplicate.Layer += 5;
            timeline.Items = timeline.Items.Add(duplicate); timeline.SelectedItems = [members[0]]; var ambiguous = Signature(timeline); var refused = vm.Resync();
            Assert(refused.Plan.UpdateCount == 0 && refused.Skipped.Count == 1 && Signature(timeline) == ambiguous, "R13 copied member identity makes the whole bundle zero-write; no first-match reconstruction");
            timeline.Items = timeline.Items.Remove(duplicate).Remove(members[1]); timeline.SelectedItems = [voice]; var missing = Signature(timeline); refused = vm.Resync();
            Assert(refused.Plan.UpdateCount == 0 && refused.Skipped.Count == 1 && Signature(timeline) == missing, "R13 a missing member never causes partial Resync or silent member regeneration");
            timeline.Items = timeline.Items.Add(members[1]); timeline.SelectedItems = [voice];
            var legacyGuard = ResyncPlan.Create(timeline, fixture.ExpressionPresets.Single(x => x.Id == fixture.CurrentExpressionPresetId));
            Assert(legacyGuard.Plan.UpdateCount == 0 && legacyGuard.Skipped.Count == 1, "R13 retained legacy Resync cannot resize only the Face member of a relative bundle");
            face.Length++; var changed = Signature(timeline); refused = vm.Resync();
            Assert(refused.Plan.UpdateCount == 0 && refused.Skipped.Count == 1 && Signature(timeline) == changed, "R13 changed source topology is rejected without guessing member mappings"); face.Length--;
            timeline.Items = [voice, nextVoice]; voice.Remark = ""; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            field.SetValue(vm, fixture); vm.Refresh(); row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            row.SelectedChoice = row.Choices.Single(x => x.Template?.Name == single.Name); await Idle(); vm.CloseExpressionTrialSession();
            var active = (PlacerSettings)field.GetValue(vm)!; var secondIndex = active.IntentPalettes.FindIndex(x => x.Id == second.Id);
            active.IntentPalettes[secondIndex] = active.IntentPalettes[secondIndex] with { Entries = [bundleTile] }; vm.RefreshExpressionVocabulary();
            Assert(!row.SelectedChoice.IsAvailable && !vm.PlaceCommand.CanExecute(null), "R10 removing a chosen membership reconstructs an unavailable association and disables batch placement");
            var staleSignature = Signature(timeline);
            Assert(vm.Place() == 0 && Signature(timeline) == staleSignature, "R10 an unavailable association is never guessed or silently rebuilt by direct batch invocation");
            active.IntentPalettes[secondIndex] = second; vm.RefreshExpressionVocabulary();
            CharacterSettings.Default.Characters.Add(character); CharacterSettings.Default.Characters.Add(detached);
            try { RejectWithoutMutation(timeline, () => IntentExecutionPlan.Create(timeline, first, bundleTile, active.Library), "R10 actual duplicate registered Character names fail closed"); }
            finally { CharacterSettings.Default.Characters.Remove(character); CharacterSettings.Default.Characters.Remove(detached); }
            // A detached pending batch row still validates the complete Voice-list snapshot and saved relation at commit.
            var stagedCatalog = IntentExpressionCatalog.Read(active);
            var stagedRows = vm.Rows.Select(x =>
            {
                var clone = new AssignmentRow(x.No, x.Target, stagedCatalog, true);
                if (ReferenceEquals(x.Target.Voice, voice)) clone.SelectedChoice = clone.Choices.Single(c => c.Template?.IntentSource?.Entry.LibraryEntryId == bundleEntry.Id);
                return clone;
            }).ToArray();
            var staged = IntentExpressionPlacement.Create(timeline, stagedRows, active);
            var firstIndex = active.IntentPalettes.FindIndex(x => x.Id == first.Id);
            active.IntentPalettes[firstIndex] = active.IntentPalettes[firstIndex] with { Relation = active.IntentPalettes[firstIndex].Relation with { StartOffset = 1 } };
            RejectWithoutMutation(timeline, () => staged.Commit(timeline, undo, active), "R10 changed saved relation invalidates a staged expression replacement commit");
            Log("R10=PASS"); Log("R13=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(source); ItemSettings.Default.Templates.Remove(single); store.Save(original); field.SetValue(vm, original);
            timeline.Items = before; timeline.SelectedItems = selection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(mode); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
}
