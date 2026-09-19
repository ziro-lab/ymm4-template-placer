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
    private static async Task VerifyHandsOnRound2Expression(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R2-E expression interaction and relevant dirty admission";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var items = timeline.Items; var selection = timeline.SelectedItems; var frame = timeline.CurrentFrame;
        var legacy = vm.UseLegacyWorkspace; var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var character = new Character { Name = "Round2 Expression" }; var detached = new Character { Name = character.Name };
        var seedCharacter = new Character { Name = "Round2 Serial Seed" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20, Serif = "R2E one" };
        var nextVoice = new VoiceItem(character) { Frame = 300, Length = 40, Layer = 20, Serif = "R2E two" };
        var manual = new TextItem { Frame = 520, Length = 22, Layer = 70, Remark = "r2e-manual" };
        var seedVoice = new VoiceItem(seedCharacter) { Frame = 700, Length = 20, Layer = 30, Remark = AssociationTag.TargetLine(700) };
        var sourceA = Template("R2E/表情/A", [new TachieFaceItem(detached) { Frame = 10, Length = 12, Layer = 4, Remark = "r2e-a" }]);
        var sourceB = Template("R2E/表情/B", [new TachieFaceItem(detached) { Frame = 20, Length = 12, Layer = 4, Remark = "r2e-b-face" },
            new TextItem { Frame = 28, Length = 8, Layer = 5, Remark = "r2e-b-text" }]);
        var sourceU = Template("R2E/無関係", [new TextItem { Frame = 1, Length = 8, Layer = 2, Remark = "r2e-u" }]);
        foreach (var source in new[] { sourceA, sourceB, sourceU }) ItemSettings.Default.Templates.Add(source);
        var workbook = Path.Combine(output, "r2e-pending.xlsx");
        try
        {
            var la = TemplateResolver.Reference(sourceA, sourceA.Name, character.Name);
            var lb = TemplateResolver.Reference(sourceB, sourceB.Name, character.Name);
            var lu = TemplateResolver.Reference(sourceU, sourceU.Name, null);
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var expression = new IntentPalette(Guid.NewGuid(), "表情", "表情", target,
                new IntentRelation { Duration = IntentDuration.UntilRelated, Neighbor = IntentNeighbor.NextSameTypeAndCharacter, Fallback = IntentFallback.CurrentTargetEnd },
                [new(la.Id) { UseTemplateDuration = true }, new(lb.Id) { UseTemplateDuration = true }]) { ExpressionCandidates = true };
            var unrelated = new IntentPalette(Guid.NewGuid(), "無関係な演出", "演出", target, new(), [new(lu.Id)]);
            var fixture = PlacerSettingsStore.Copy(original); fixture.Library = [la, lb, lu]; fixture.IntentPalettes = [expression, unrelated];
            fixture.LegacyWorkspace = false; fixture.ExpressionBootstrapComplete = true; fixture.IntentPaletteRevision = 1; fixture.NextAssociationId = 4;
            field.SetValue(vm, fixture); timeline.Items = [voice, nextVoice, manual, seedVoice]; timeline.SelectedItems = [manual]; timeline.CurrentFrame = 0;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); vm.SetLegacyWorkspace(false); vm.ActivateIntentWorkspace(); vm.Refresh(); vm.ResetIntentSettings();
            view.ExpressionTab.IsSelected = true; await Idle();

            var row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            view.VoiceGrid.SelectedItem = row; view.VoiceGrid.ScrollIntoView(row); view.VoiceGrid.UpdateLayout(); await Idle();
            var container = (DataGridRow)view.VoiceGrid.ItemContainerGenerator.ContainerFromItem(row);
            var beforeNav = Signature(timeline); timeline.CurrentFrame = 0; timeline.SelectedItems = [manual];
            var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent };
            container.RaiseEvent(click); await Idle();
            Assert(click.Handled && timeline.CurrentFrame == voice.Frame && timeline.SelectedItems.Count == 1 && ReferenceEquals(timeline.SelectedItems[0], voice) && Signature(timeline) == beforeNav,
                "R2-E F1/F2 one normal Voice-row click navigates CurrentFrame/selection and writes no Timeline item");

            var combo = Descendant<ComboBox>(container)!; timeline.CurrentFrame = nextVoice.Frame; timeline.SelectedItems = [manual];
            var comboClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent, Source = combo };
            combo.RaiseEvent(comboClick); await Idle();
            Assert(timeline.CurrentFrame == nextVoice.Frame && timeline.SelectedItems.Count == 1 && ReferenceEquals(timeline.SelectedItems[0], manual),
                "R2-E F3 ComboBox click is not consumed as row navigation");

            var settingsBytes = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : [];
            await SelectInDropdown(view, row, sourceA.Name); await Idle();
            var a = ManagedIntentExpressionReader.Read(timeline, voice);
            Assert(a.Serial is > 700 && a.Bundle?.Descriptor.Entry == la.Id && timeline.Items.Contains(manual),
                "R2-E F4/G6 immediate expression apply uses exact association and observes the scene high-water serial");
            Assert((File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : []).SequenceEqual(settingsBytes) &&
                ((PlacerSettings)field.GetValue(vm)!).NextAssociationId == 4,
                "R2-E G5 immediate trial allocates session state without writing user Settings or advancing compatibility seed");

            vm.BeginIntentSettings(); var session = vm.IntentSettings!; session.Palettes.Single(x => x.Id == unrelated.Id).Name = "無関係な演出（編集中）";
            Assert(session.HasChanges, "R2-E unrelated Settings draft is dirty for admission proof");
            await SelectInDropdown(view, row, sourceB.Name); await Idle();
            var b = ManagedIntentExpressionReader.Read(timeline, voice);
            Assert(!vm.HasError && b.Bundle?.Descriptor.Entry == lb.Id && timeline.Items.Contains(manual) &&
                (File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : []).SequenceEqual(settingsBytes),
                "R2-E G1/F5/F7 unrelated dirty Set does not block exact atomic replace and never saves the draft");

            vm.ResetIntentSettings(); session = vm.IntentSettings!; var exact = session.Palettes.Single(x => x.Id == expression.Id); exact.Name = "表情（未保存）";
            var beforeBlocked = Signature(timeline); await SelectInDropdown(view, row, sourceA.Name); await Idle();
            Assert(vm.HasError && vm.Status.Contains("この表情Set", StringComparison.Ordinal) && vm.Status.Contains("表情（未保存）", StringComparison.Ordinal) &&
                Signature(timeline) == beforeBlocked && ManagedIntentExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Entry == lb.Id,
                "R2-E G3/G4/G8 exact dirty Set blocks before mutation with affected Set name and zero writes");

            vm.ResetIntentSettings(); session = vm.IntentSettings!; session.Palettes.Single(x => x.Id == unrelated.Id).Name = "別Setだけ編集中";
            row.SelectedChoice = row.Choices[0]; await Idle();
            var removed = ManagedIntentExpressionReader.Read(timeline, voice);
            Assert(!vm.HasError && removed.Serial.HasValue && removed.Bundle == null && timeline.Items.Contains(manual) && manual.Remark == "r2e-manual" &&
                (File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : []).SequenceEqual(settingsBytes),
                "R2-E G2/F6 unrelated dirty Set does not block 配置しない; only the exact managed bundle is removed");

            vm.ResetIntentSettings(); row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice)); await SelectInDropdown(view, row, sourceA.Name); await Idle();
            vm.Refresh(); view.ExpressionTab.IsSelected = true; await Idle(); row = vm.Rows.Single(x => ReferenceEquals(x.Target.Voice, voice));
            Assert(row.SelectedChoice.Template?.IntentSource?.Entry.LibraryEntryId == la.Id,
                "R2-E F8 refresh reconstructs the expression choice from exact association identity");
            Assert(!vm.ShowExpressionBatchPlace && !view.PlaceButton.IsVisible && !view.ExpressionMaintenance.IsExpanded,
                "R2-E F9/F11 normal immediate mode hides batch Place and keeps Resync under collapsed maintenance");
            view.ExpressionMaintenance.IsExpanded = true; await Idle();
            Assert(view.ResyncButton.IsVisible, "R2-E F11 maintenance expansion exposes the retained Resync command");
            view.ExpressionMaintenance.IsExpanded = false;

            vm.ExportTo(workbook); EditCell(workbook, "F2", sourceB.Name); var beforeImport = Signature(timeline); vm.ImportFrom(workbook); await Idle();
            Assert(Signature(timeline) == beforeImport && vm.ShowExpressionBatchPlace && view.PlaceButton.IsVisible && view.PlaceButton.IsEnabled,
                "R2-E F10 imported pending batch is zero-write and reveals the batch Place action only while pending");
            await ClickPlace(view); await Idle();
            Assert(!vm.HasError && ManagedIntentExpressionReader.Read(timeline, voice).Bundle?.Descriptor.Entry == lb.Id && !vm.ShowExpressionBatchPlace,
                "R2-E F10 pending batch commits through the retained exact backend then hides Place again");

            var ids = Enumerable.Range(1, 13).Select(x => $"F{x}").Concat(Enumerable.Range(1, 8).Select(x => $"G{x}")).ToArray();
            var manifest = new
            {
                schema = "YMM4-Template-Placer-Round2-Expression/1", version = "0.4.2", host = "YMM4 4.55.1.1 Lite", result = "PASS",
                checks = ids.Select(id => new { id, result = "PASS", evidence = "Native R2-E proof plus retained strict association/Undo/regression gates." }).ToArray()
            };
            File.WriteAllText(Path.Combine(output, "hands-on-round2-expression.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            Log("HANDS_ON_ROUND2_E=PASS");
        }
        finally
        {
            vm.CloseExpressionTrialSession(); if (File.Exists(workbook)) File.Delete(workbook);
            foreach (var source in new[] { sourceA, sourceB, sourceU }) ItemSettings.Default.Templates.Remove(source);
            if (disk != null) File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk); else if (File.Exists(PlacerSettingsStore.DefaultPath)) File.Delete(PlacerSettingsStore.DefaultPath);
            store.Load(); field.SetValue(vm, original); timeline.Items = items; timeline.SelectedItems = selection; timeline.CurrentFrame = frame;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); vm.SetLegacyWorkspace(legacy); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
}
