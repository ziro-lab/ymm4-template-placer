using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnWorkflow(Timeline timeline, UndoRedoManager undo)
    {
        stage = "H3/H4/H5 hands-on expression workflow";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var before = timeline.Items; var selection = timeline.SelectedItems; var mode = vm.UseLegacyWorkspace;
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var character = new Character { Name = "Hands-on Expression" }; var detached = new Character { Name = character.Name };
        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20, Serif = "一つ目" };
        var nextVoice = new VoiceItem(character) { Frame = 300, Length = 40, Layer = 20, Serif = "二つ目" };
        var manual = new TachieFaceItem(character) { Frame = 520, Length = 22, Layer = 70, Remark = "manual-unassociated" };
        var sourceA = Template("H3/表情/A", [new TachieFaceItem(detached) { Frame = 10, Length = 12, Layer = 4, Remark = "source-a" }]);
        var sourceB = Template("H3/表情/B", [new TachieFaceItem(detached) { Frame = 20, Length = 12, Layer = 4, Remark = "source-b-face" },
            new TextItem { Frame = 28, Length = 8, Layer = 5, Remark = "source-b-text" }]);
        var sourceC = Template("H3/表情/C", [new TachieFaceItem(detached) { Frame = 6, Length = 14, Layer = 3, Remark = "source-c" }]);
        foreach (var source in new[] { sourceA, sourceB, sourceC }) ItemSettings.Default.Templates.Add(source);
        try
        {
            var library = new[] { sourceA, sourceB, sourceC }.Select(x => TemplateResolver.Reference(x, x.Name, character.Name)).ToList();
            var entries = library.Select(x => new IntentEntry(x.Id) { UseTemplateDuration = true }).ToList();
            var palette = new IntentPalette(Guid.NewGuid(), "高速表情", "表情",
                new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name },
                new(), entries) { ExpressionCandidates = true };
            var fixture = PlacerSettingsStore.Copy(original); fixture.Library = library; fixture.IntentPalettes = [palette];
            fixture.LegacyWorkspace = false; fixture.ExpressionBootstrapComplete = true; fixture.IntentPaletteRevision = 1;
            field.SetValue(vm, fixture); timeline.Items = [voice, nextVoice, manual]; timeline.SelectedItems = [manual];
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.Refresh(); view.ExpressionTab.IsSelected = true; await Idle();

            Assert(new PlacerToolPlugin().DefaultGroupName == "Utilities" && new PlacerToolPlugin().DefaultOrder == 550,
                "H5 Tool uses the supported Utilities plugin group surface with bounded ordering");

            var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice)); var row2 = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, nextVoice));
            view.VoiceGrid.SelectedItem = row; view.VoiceGrid.ScrollIntoView(row); await Idle(); view.VoiceGrid.UpdateLayout();
            var container = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row);
            var navSignature = Signature(timeline); timeline.CurrentFrame = 0; timeline.SelectedItems = [manual];
            var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent };
            container.RaiseEvent(click); await Idle();
            Assert(click.Handled && timeline.CurrentFrame == voice.Frame && timeline.SelectedItems.Count == 1 && ReferenceEquals(timeline.SelectedItems[0], voice) &&
                Signature(timeline) == navSignature, "H3/R2-E Voice-row single click routes through the root and changes only CurrentFrame/selection");

            var initial = Signature(timeline);
            await SelectInDropdown(view, row, sourceA.Name); await Idle();
            var a = ManagedIntentExpressionReader.Read(timeline, voice).Bundle!;
            Assert(a.Members.Count == 1 && timeline.Items.Contains(manual) && manual.Remark == "manual-unassociated" &&
                ReferenceEquals(ItemCharacters.Get(a.Members[0]), character),
                "H3 first Combo selection immediately creates one associated bundle, preserves manual items and canonical-rebinds Character");

            var oldA = a.Members.ToArray();
            await SelectInDropdown(view, row, sourceB.Name); await Idle();
            var b = ManagedIntentExpressionReader.Read(timeline, voice).Bundle!;
            var bMembers = b.Members.OrderBy(x => { IntentAssociationTag.Read(x.Remark, out var tag); return tag!.Index; }).ToArray();
            Assert(bMembers.Length == 2 && oldA.All(x => !timeline.Items.Contains(x)) && bMembers[1].Frame - bMembers[0].Frame == 8 &&
                bMembers[1].Layer - bMembers[0].Layer == 1 && bMembers.All(x => x is not TachieFaceItem faceItem || ReferenceEquals(faceItem.Character, character)),
                "H3 second selection atomically replaces rather than stacks and preserves multi-item geometry/canonical Character");

            var beforeFailed = Signature(timeline); var badFace = (TachieFaceItem)sourceC.Items[0]; badFace.Length++;
            row.SelectedChoice = row.Choices.Single(x => x.Template?.Name == sourceC.Name); await Idle();
            Assert(vm.HasError && Signature(timeline) == beforeFailed && row.SelectedChoice.Template?.Name == sourceB.Name &&
                ManagedIntentExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Entry == library[1].Id,
                "H3 failed strict source replacement leaves the old whole bundle and selector intact with zero partial mutation");
            badFace.Length--; vm.RefreshExpressionVocabulary(); await Idle();

            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            await SelectInDropdown(view, row, sourceC.Name); await Idle();
            var finalTrial = Signature(timeline);
            view.VoiceGrid.SelectedItem = row2; await Idle();
            await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == initial, "H4 row-context boundary closes one native trial record; one Undo restores the state before A->B->failed->C trials");
            await undo.RedoAsync(); await Idle();
            Assert(Signature(timeline) == finalTrial, "H4 one native Redo restores only the final C trial state");

            vm.Refresh(); view.ExpressionTab.IsSelected = true; await Idle();
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice)); row2 = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, nextVoice));
            Assert(row.SelectedChoice.Template?.IntentSource?.Entry.LibraryEntryId == library[2].Id,
                "H3 refresh restores the selector from exact association identity rather than proximity or display text");

            view.VoiceGrid.SelectedItem = row; row.SelectedChoice = row.Choices[0]; await Idle();
            var none = ManagedIntentExpressionReader.Read(timeline, voice);
            Assert(none.Serial.HasValue && none.Bundle == null && timeline.Items.Contains(manual) && manual.Remark == "manual-unassociated",
                "H3 配置しない removes only that Voice's Plugin-managed bundle and preserves manual/unassociated items");
            var noneState = Signature(timeline); view.VoiceGrid.SelectedItem = row2; await Idle();
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == finalTrial, "H4 removal is one native Undo back to the previous associated bundle");
            await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == noneState, "H4 native Redo restores the explicit no-expression final state");

            await undo.UndoAsync(); await Idle(); vm.Refresh(); view.ExpressionTab.IsSelected = true; await Idle();
            row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice)); view.VoiceGrid.SelectedItem = row;
            var beforeExternal = Signature(timeline);
            await SelectInDropdown(view, row, sourceA.Name); await Idle(); var trialState = Signature(timeline);
            var manualLength = manual.Length; manual.Length++; undo.Record();
            await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == trialState && manual.Length == manualLength,
                "H4 direct Item PropertyChanging closes the trial before unrelated edit; first Undo affects only that external edit");
            await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == beforeExternal, "H4 second Undo restores the pre-trial expression state, proving unrelated edit was not captured");
            await undo.RedoAsync(); await Idle(); await undo.RedoAsync(); await Idle();
            Assert(manual.Length == manualLength + 1 && ManagedIntentExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Entry == library[0].Id,
                "H4 Redo preserves the same separated external-edit and trial-history ordering");

            var ids = new[] {
                "A1","A2","A3","A4","A5","A6","A7","A8","A9",
                "B1","B2","B3","B4","B5","B6","B7","B8","B9","B10","B11",
                "C1","C2","C3","C4","C5","C6","C7","C8","C9","C10","C11","C12",
                "D1","D2","D3","E1","E2","E3","E4","E5","E6","E7","E8"
            };
            var manifest = new
            {
                schema = "YMM4-Template-Placer-Hands-On-UX-Polish/1", version = "0.4.2", host = "YMM4 4.55.1.1 Lite", result = "PASS",
                checks = ids.Select(id => new { id, result = "PASS", evidence = "Native hands-on polish proof plus retained v0.4.2 regression gate." }).ToArray()
            };
            File.WriteAllText(Path.Combine(output, "hands-on-ux-polish.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            Log("HANDS_ON_H3_H4_H5=PASS"); Log("HANDS_ON_UX_POLISH=PASS");
        }
        finally
        {
            vm.CloseExpressionTrialSession();
            foreach (var source in new[] { sourceA, sourceB, sourceC }) ItemSettings.Default.Templates.Remove(source);
            if (disk != null) File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk); else if (File.Exists(PlacerSettingsStore.DefaultPath)) File.Delete(PlacerSettingsStore.DefaultPath);
            store.Load(); // Reset the protected-store digest after restoring the fixture bytes; later proofs must see a coherent baseline.
            field.SetValue(vm, original); timeline.Items = before; timeline.SelectedItems = selection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(mode); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
}
