using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnRound2Sets(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R2-B unified Set surface";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (PlacerSettings)field.GetValue(vm)!;
        var items = timeline.Items; var selection = timeline.SelectedItems; var frame = timeline.CurrentFrame;
        var mode = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
        var character = new Character { Name = "Round2 Sets" };
        var voice = new VoiceItem(character) { Frame = 50, Length = 30, Layer = 20 };
        var sources = new[] {
            Template("R2B/表情", [new TachieFaceItem(character) { Length = 10, Layer = 4 }]),
            Template("R2B/汎用テロップ", [new TextItem { Length = 17, Layer = 5 }]),
            Template("R2B/複合は対象用", [new TextItem { Length = 10, Layer = 4 }, new TextItem { Length = 10, Layer = 5 }]) };
        foreach (var source in sources) ItemSettings.Default.Templates.Add(source);
        try
        {
            var library = sources.Select(x => TemplateResolver.Reference(x, x.Name, null)).ToArray();
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var targeted = new IntentPalette(Guid.NewGuid(), "表情", "preserved/exact compatibility value", target, new(), [new(library[0].Id)]);
            var generic = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "汎用テロップ", null, [library[1].Id, library[2].Id]);
            var fixture = PlacerSettingsStore.Copy(original);
            fixture.Library = [.. library]; fixture.IntentPalettes = [targeted]; fixture.Palettes = [generic];
            fixture.ManualCharacterPaletteId = null; fixture.ManualStylePaletteId = null;
            fixture.ExpressionBootstrapComplete = true; fixture.LegacyWorkspace = false;
            field.SetValue(vm, fixture); timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.CurrentFrame = 177;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); vm.SetLegacyWorkspace(false); vm.ActivateIntentWorkspace();
            vm.EndTimelinePointer(); timeline.SelectedItems = [voice]; view.PaletteTab.IsSelected = true; await Idle();
            var surface = view.RelativePaletteSurface; var signature = Signature(timeline); var savedJson = JsonSerializer.Serialize(fixture);
            Assert(vm.PlacementContext == PlacementContext.Selection && vm.IntentSets.Single().Targeted?.Id == targeted.Id &&
                vm.IntentTiles.Single().TargetedEntry?.LibraryEntryId == library[0].Id,
                "R2-B B1/B2 selection uses the existing targeted Set and entry, not a copied Generic model");
            Assert(surface.FindName("IntentTabStrip") == null && !surface.IntentSetRow.IsVisible && surface.SingleSetTitleText.Text == targeted.Name,
                "R2-B B1/B5 normal surface has no Intent control and a lone Set hides redundant selection UI");
            var targetTile = vm.IntentTiles.Single();
            vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer(); await Idle();
            Assert(vm.PlacementContext == PlacementContext.Generic && vm.IntentSets.Single().Generic?.Id == generic.Id &&
                vm.IntentTiles.Count == 2 && vm.IntentTiles.All(x => x.IsGeneric) && timeline.SelectedItems.Single() == voice,
                "R2-B B3/B10 background displays only Style Sets without clearing YMM4 selection");
            Assert(!surface.IntentSetRow.IsVisible && Signature(timeline) == signature && JsonSerializer.Serialize(fixture) == savedJson,
                "R2-B B4/B5 context switching is Timeline/settings zero-write and lone Generic Set has no selector");
            var unavailable = vm.IntentTiles.Single(x => x.LibraryEntryId == library[2].Id);
            Assert(!unavailable.Available && !vm.ExecuteIntentTileCommand.CanExecute(unavailable),
                "R2-B B8 Generic accurately retains QuickDrop single-item compatibility instead of inventing a bundle engine");
            var button = RelativeVisuals(surface.IntentTileItems).OfType<Button>().Single(x => x.CommandParameter is IntentTileChoice t && t.LibraryEntryId == library[1].Id);
            await InvokeSelectionButton(button);
            var placed = timeline.Items.Except(new IItem[] { voice }).Single();
            Assert(placed is TextItem && placed.Frame == 177 && placed.Length == 17 && timeline.SelectedItems.Single() == voice,
                "R2-B B8 Generic native tile delegates to QuickDrop CurrentFrame/intrinsic-duration placement and retains Selection");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == signature, "R2-B Generic tile is reverted by one native Undo");
            var staleRejected = false;
            try { vm.ExecuteIntentTile(targetTile); } catch (InvalidOperationException) { staleRejected = true; }
            Assert(staleRejected && Signature(timeline) == signature, "R2-B stale targeted tile cannot execute from the Generic surface");
            vm.EndTimelinePointer(); timeline.SelectedItems = [voice]; await Idle();
            Assert(vm.SelectedIntentSet?.Id == targeted.Id && vm.IntentSets.Single().Targeted?.Intent == targeted.Intent,
                "R2-B context return restores its targeted Set without changing legacy Intent metadata");
            var count = vm.ExecuteIntentTile(vm.IntentTiles.Single());
            Assert(count == 1 && timeline.Items.Except(new IItem[] { voice }).Single().Frame == voice.Frame,
                "R2-B B7 targeted tile still delegates to saved relative relation rather than QuickDrop time");
            await undo.UndoAsync(); await Idle();

            var otherStyle = generic with { Id = Guid.NewGuid(), Name = "もう一つ", LibraryEntryIds = [library[1].Id] };
            var two = PlacerSettingsStore.Copy(fixture); two.Palettes.Add(otherStyle); field.SetValue(vm, two);
            vm.ObserveTimelinePointer(TimelinePointerOrigin.Ruler); vm.EndTimelinePointer(); await Idle();
            Assert(vm.IntentSets.Count == 2 && surface.IntentSetSegments.IsVisible && !surface.IntentSetPicker.IsVisible,
                "R2-B B6 small Generic Set choices use the same visible segments as targeted Sets");
            surface.IntentSetSegments.SelectedIndex = 1; await Idle();
            vm.EndTimelinePointer(); timeline.SelectedItems = [voice]; await Idle();
            vm.ObserveTimelinePointer(TimelinePointerOrigin.Ruler); vm.EndTimelinePointer(); await Idle();
            Assert(vm.SelectedIntentSet?.Id == otherStyle.Id && Signature(timeline) == signature,
                "R2-B the last Generic Set is restored independently of the targeted context");
            var many = PlacerSettingsStore.Copy(two);
            many.Palettes = Enumerable.Range(0, 5).Select(i => generic with { Id = Guid.NewGuid(), Name = "汎用" + i }).ToList();
            field.SetValue(vm, many); vm.RefreshIntentWorkspace(); view.Width = 360; view.Height = 440; await Idle();
            Assert(surface.IntentSetPicker.IsVisible && !surface.IntentSetSegments.IsVisible && vm.IntentSets.Count == 5,
                "R2-B only many Generic Sets use a picker; no Intent level is reintroduced");
            SaveNamedView(view, "v042-round2-generic-360.png");
            File.WriteAllText(Path.Combine(output, "hands-on-round2-sets.json"), JsonSerializer.Serialize(new {
                schema = "YMM4-Template-Placer-Round2-Sets/1", result = "PASS", targeted = "IntentExecutionPlan", generic = "QuickDropPlanner" }));
            Log("HANDS_ON_ROUND2_B=PASS");
        }
        finally
        {
            foreach (var source in sources) ItemSettings.Default.Templates.Remove(source);
            field.SetValue(vm, original); vm.EndTimelinePointer(); timeline.Items = items; timeline.SelectedItems = selection; timeline.CurrentFrame = frame;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record(); vm.SetLegacyWorkspace(mode);
            view.Width = width; view.Height = height; vm.Refresh();
        }
    }
}
