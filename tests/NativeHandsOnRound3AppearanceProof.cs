using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static readonly Dictionary<string, string> round3Checks = new(StringComparer.Ordinal);
    private static void Round3Assert(bool condition, string id, string evidence)
    {
        Assert(condition, $"R3 {id}: {evidence}");
        if (!round3Checks.TryAdd(id, evidence)) throw new InvalidOperationException("Duplicate Round 3 check: " + id);
    }
    private static void Round3Phase(string group, string name)
    {
        var checks = round3Checks.Where(x => x.Key.StartsWith(group, StringComparison.Ordinal))
            .Select(x => new { id = x.Key, result = "PASS", evidence = x.Value }).ToArray();
        File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Round3-Phase/1", phase = group, host = "YMM4 4.55.1.1 Lite", result = "PASS", checks
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private sealed class Round3Fixture : IDisposable
    {
        private readonly Action restore;
        private readonly FieldInfo settingsField = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private readonly Timeline timeline;
        private readonly UndoRedoManager undo;
        public PlacerSettings Original { get; }
        public List<ItemTemplate> Templates { get; } = [];
        public PlacerSettings Current => (PlacerSettings)settingsField.GetValue(ViewModel!)!;
        public Round3Fixture(Timeline timeline, UndoRedoManager undo)
        {
            this.timeline = timeline; this.undo = undo;
            var vm = ViewModel!; var view = View!;
            Original = Current;
            var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
            var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
            var items = timeline.Items; var selection = timeline.SelectedItems; var frame = timeline.CurrentFrame;
            var legacy = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
            var window = Window.GetWindow(view)!;
            var ww = window.Width; var wh = window.Height; var wl = window.Left; var wt = window.Top; var ws = window.WindowState;
            restore = () =>
            {
                foreach (var template in Templates) ItemSettings.Default.Templates.Remove(template);
                if (disk == null) File.Delete(PlacerSettingsStore.DefaultPath); else File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk);
                store.Load(); settingsField.SetValue(vm, Original);
                timeline.Items = items; timeline.SelectedItems = selection; timeline.CurrentFrame = frame;
                timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
                view.Width = width; view.Height = height; window.WindowState = System.Windows.WindowState.Normal;
                window.Width = ww; window.Height = wh; window.Left = wl; window.Top = wt; window.WindowState = ws;
                vm.SetLegacyWorkspace(legacy); vm.Refresh(); vm.ResetIntentSettings();
            };
            window.WindowState = System.Windows.WindowState.Normal; window.Left = SystemParameters.WorkArea.Left + 8; window.Top = SystemParameters.WorkArea.Top + 8;
            window.Width = Math.Min(600, SystemParameters.WorkArea.Width - 16); window.Height = Math.Min(600, SystemParameters.WorkArea.Height - 16);
            view.Width = 360; view.Height = 400; window.Activate();
        }
        public LibraryEntry AddTemplate(string name, IItem item)
        {
            var template = Template(name, [item]); Templates.Add(template); ItemSettings.Default.Templates.Add(template);
            return TemplateResolver.Reference(template, name, null);
        }
        public void Apply(PlacerSettings settings, IItem[] items, IItem[] selection, int frame = 10)
        {
            settingsField.SetValue(ViewModel!, settings);
            timeline.Items = [.. items]; timeline.SelectedItems = [.. selection]; timeline.CurrentFrame = frame;
            timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            var vm = ViewModel!; vm.SetLegacyWorkspace(false); vm.ActivateIntentWorkspace(); vm.Refresh(); vm.ResetIntentSettings();
            View!.PaletteTab.IsSelected = true;
        }
        public void Dispose() => restore();
    }
    private static async Task VerifyHandsOnRound3Appearance(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-A face colors and atomic Set-wide shape";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var surface = View!.RelativePaletteSurface;
        var a = scope.AddTemplate("R3A/A", new TextItem { Length = 11 });
        var b = scope.AddTemplate("R3A/B", new TextItem { Length = 13 });
        var voice = new VoiceItem(new Character { Name = "R3 Appearance" }) { Frame = 100, Length = 30, Layer = 20 };
        var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))] };
        var set = new IntentPalette(Guid.NewGuid(), "表情Set", "legacy", target, new(), [new(a.Id) { Color = IntentTileColor.Blue }, new(b.Id)]);
        var generic = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "汎用Set", null, [a.Id, b.Id]);
        var fixture = PlacerSettingsStore.Copy(scope.Original); fixture.Library = [a, b]; fixture.IntentPalettes = [set]; fixture.Palettes = [generic];
        fixture.ManualStylePaletteId = generic.Id; fixture.ManualCharacterPaletteId = null;
        fixture.ExpressionBootstrapComplete = true; fixture.IntentPaletteRevision = 1;
        scope.Apply(fixture, [voice], [voice]); await Idle();
        Round3Assert(!RelativeVisuals(surface).OfType<FrameworkElement>().Any(x => x.Name is "TileColorAccent" or "TileDragHandle"), "A1", "obsolete narrow drag/color bar is absent from the live visual tree");
        var colored = Round2TileButtons()[0];
        Round3Assert(colored.Background is SolidColorBrush face && face.Color != SystemColors.ControlColor && colored.BorderThickness.Left == 2,
            "A2", "finite color changes the live tile face and full border");
        Round3Assert(colored.Foreground == SystemColors.ControlTextBrush, "A3", "live tile foreground is the system text brush");
        Round3Assert(Enum.GetValues<IntentTileColor>().All(x => ReferenceEquals(IntentTileAppearance.FaceForTheme(x, true, Colors.Black), SystemColors.ControlBrush)),
            "A4", "every finite color takes the same production High Contrast system-brush branch");
        var before = Signature(timeline);
        await NativeRound2Click(await Round2TilePoint(colored));
        Round3Assert(timeline.Items.Count == 2 && timeline.Items.Single(x => x != voice).Frame == voice.Frame, "A5", "whole-tile native short click places exactly one item");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == before, "R3-A click remains one native Undo");
        await Round2TileDrag(Round2TileButtons()[0], Round2TileButtons()[1]);
        Round3Assert(Signature(timeline) == before && scope.Current.IntentPalettes.Single().Entries[0].LibraryEntryId == b.Id, "A6", "native whole-tile drag reorders and performs zero placement");
        var allShapes = true;
        foreach (var shape in Enum.GetValues<IntentTileShape>())
        {
            vm.ChangeIntentSetShape(vm.SelectedIntentSet!, shape); await Idle();
            allShapes &= scope.Current.IntentPalettes.Single().Entries.All(x => x.Shape == shape);
        }
        vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer(); await Idle();
        foreach (var shape in Enum.GetValues<IntentTileShape>())
        {
            vm.ChangeIntentSetShape(vm.SelectedIntentSet!, shape); await Idle();
            var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().Palettes.Single();
            allShapes &= saved.LibraryEntryIds.All(x => saved.AppearanceFor(x).Shape == shape);
        }
        // R4 replaces the permanent Set menu with current-Set Settings. Exercise the new affordance.
        vm.OpenCurrentSetSettingsCommand.Execute(null); await Idle();
        var shapeButtons = View!.RelativeSettingsSurface.SetShapeButtons.Children.OfType<Button>().ToArray();
        allShapes &= shapeButtons.Length == 3 && shapeButtons.All(x => x.Command == vm.SetSettingsShapeCommand);
        var circle = shapeButtons.Single(x => Equals(x.CommandParameter, IntentTileShape.Circle));
        ((IInvokeProvider)new ButtonAutomationPeer(circle).GetPattern(PatternInterface.Invoke)).Invoke(); await Idle();
        vm.SaveIntentSettings(); View.PaletteTab.IsSelected = true; await Idle();
        Round3Assert(allShapes && scope.Current.Palettes.Single().LibraryEntryIds.All(x => scope.Current.Palettes.Single().AppearanceFor(x).Shape == IntentTileShape.Circle),
            "A7", "all three shapes bulk-write both backends and the replacement Settings affordance bulk-writes the same entry shapes");
        Round3Assert(typeof(IntentPalette).GetProperty("Shape") == null && typeof(PaletteDefinition).GetProperty("Shape") == null &&
            scope.Current.IntentPalettes.Single().Entries.Select(x => x.LibraryEntryId).SequenceEqual(new[] { b.Id, a.Id }) && Signature(timeline) == before,
            "A8", "bulk shape changes only existing entry fields; no Set shape inheritance, order or Timeline mutation");
        vm.ChangeIntentTileAppearance(vm.IntentTiles[0], x => x with { Shape = IntentTileShape.Square }); await Idle();
        Round3Assert(vm.IntentTiles[0].Shape == IntentTileShape.Square && vm.IntentTiles[1].Shape == IntentTileShape.Circle,
            "A9", "one tile remains individually editable after the Set-wide change");
        var bytes = File.ReadAllBytes(PlacerSettingsStore.DefaultPath); vm.IntentSettings!.GenericSets.Single().Name = "未保存";
        var token = vm.SelectedIntentSet!;
        RejectWithoutMutation(timeline, () => vm.ChangeIntentSetShape(token, IntentTileShape.Rounded), "R3-A staged draft blocks bulk shape");
        Round3Assert(!vm.ShapeIntentSetCommand.CanExecute(new IntentSetShapeRequest(token, IntentTileShape.Rounded)) &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(bytes) && vm.IntentSettings.HasChanges,
            "A10", "bulk command preserves the exact staged Settings draft and stored bytes");
        vm.ResetIntentSettings(); SaveNamedView(View!, "v042-round3-appearance-360.png");
        Round3Phase("A", "hands-on-round3-appearance.json"); Log("HANDS_ON_ROUND3_A=PASS");
    }
}
