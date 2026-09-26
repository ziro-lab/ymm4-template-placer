using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnRound3Layers(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-D numeric target layer and bounded directional collision policy";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!; var surface = view.RelativePaletteSurface.GenericLayerSurface;
        var source = scope.AddTemplate("R3D/exact", new TextItem { Layer = 60, Length = 20 });
        var palette = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "数値指定", null, [source.Id])
            { Layer = new() { UseTemplateLayer = false, Preferred = 8, Minimum = 4, Maximum = 12, SearchMode = LayerSearchMode.DoNotPlace } };
        var fixture = PlacerSettingsStore.Copy(scope.Original); fixture.Library = [source]; fixture.Palettes = [palette]; fixture.IntentPalettes = [];
        fixture.ExpressionBootstrapComplete = true; fixture.ManualStylePaletteId = palette.Id; fixture.ManualCharacterPaletteId = null;
        void Apply(LayerPolicy policy, params IItem[] items)
        {
            var next = PlacerSettingsStore.Copy(fixture); next.Palettes[0] = palette with { Layer = policy };
            scope.Apply(next, items, [], 100);
            // Clearing selection intentionally does not change the remembered context.
            // Enter Generic through the same bounded background-intent route as the UI.
            vm.ObserveTimelinePointer(TimelinePointerOrigin.TimelineBackground); vm.EndTimelinePointer();
        }
        TextItem Occupied(int layer) => new() { Layer = layer, Frame = 109, Length = 30 };
        void Tile() => vm.ExecuteIntentTileCommand.Execute(vm.IntentTiles.Single());
        async Task<bool> PlacesAt(int layer)
        {
            var signature = Signature(timeline); var originals = timeline.Items.ToArray();
            Tile(); await Idle();
            var placed = timeline.Items.Except(originals).ToArray();
            var good = !vm.HasError && placed.Length == 1 && placed[0].Frame == 100 && placed[0].Length == 20 && placed[0].Layer == layer;
            await undo.UndoAsync(); await Idle();
            Assert(Signature(timeline) == signature, "R3-D placement is one native Undo with exact original items retained");
            return good;
        }
        Apply(palette.Layer); await Idle();
        Log($"R3-D surface: context={vm.PlacementContext}; set={vm.SelectedIntentSet?.Id}; draft={vm.GenericLayerTarget?.Target}; visible={surface.IsVisible}; box={surface.GenericTargetBox.IsVisible}; choices={surface.GenericOccupiedPicker.Items.Count}");
        Round3Assert(surface.IsVisible && surface.GenericTargetBox.IsVisible && surface.GenericOccupiedPicker.Items.Count == 2 &&
            GenericLayerTargetDraft.Behaviors.Select(x => x.Value).SequenceEqual(new[] { LayerSearchMode.SearchUp, LayerSearchMode.SearchDown }),
            "D1", "live Generic surface exposes only Up/Down escape directions; layer collision no longer offers a do-not-place choice");
        var before = Signature(timeline);
        surface.GenericTargetBox.Text = "9"; surface.GenericOccupiedPicker.SelectedValue = LayerSearchMode.SearchUp; await Idle();
        Assert(vm.GenericLayerTarget?.Target == "9" && !vm.ExecuteIntentTileCommand.CanExecute(vm.IntentTiles.Single()), "R3-D unapplied numeric draft disables placement instead of using the old saved target");
        Window.GetWindow(view)!.Activate(); Keyboard.Focus(surface.GenericTargetBox); await Idle();
        Log($"R3-D before Enter: focus={Keyboard.FocusedElement?.GetType().FullName}; exactFocus={ReferenceEquals(Keyboard.FocusedElement, surface.GenericTargetBox)}; target={vm.GenericLayerTarget?.Target}; behavior={vm.GenericLayerTarget?.OccupiedBehavior}; canApply={vm.ApplyGenericLayerTargetCommand.CanExecute(null)}; settingsDirty={vm.IntentSettings?.HasChanges}");
        Assert(ReferenceEquals(Keyboard.FocusedElement, surface.GenericTargetBox), "R3-D native Enter is delivered to the actual numeric editor");
        await Round3PressKey(Key.Enter);
        var savedLayer = scope.Current.Palettes.Single().Layer;
        Log($"R3-D after Enter: saved={JsonSerializer.Serialize(savedLayer)}; target={vm.GenericLayerTarget?.Target}; dirty={vm.GenericLayerTarget?.HasChanges}; error={vm.HasError}; status={vm.Status}; unchanged={Signature(timeline) == before}");
        Assert(savedLayer == palette.Layer with { Preferred = 9, SearchMode = LayerSearchMode.SearchUp }, "R3-D Enter persisted the exact target/policy and unchanged bounds");
        Assert(vm.GenericLayerTarget?.HasChanges == false, "R3-D Enter replaced the applied draft with a clean snapshot");
        Round3Assert(Signature(timeline) == before, "D2", "actual Enter saves the direct target and occupied policy atomically, preserving bounds without Timeline mutation");
        surface.GenericTargetBox.Text = "10"; await Idle(); Keyboard.Focus(surface.GenericTargetBox); await Round3PressKey(Key.Escape);
        Assert(vm.GenericLayerTarget is { Target: "9", HasChanges: false }, "R3-D actual Escape resets the quick layer draft without saving");
        Apply(palette.Layer); await Idle();
        Round3Assert(await PlacesAt(8), "D3", "free target is used exactly, not the source template layer or another free layer");
        Apply(palette.Layer, Occupied(8)); await Idle();
        Round3Assert(await PlacesAt(7), "D4", "legacy saved DoNotPlace is normalized to Up escape so an explicit placement request still places when the first layer is occupied");
        Apply(palette.Layer with { SearchMode = LayerSearchMode.SearchUp }, Occupied(8), Occupied(7)); await Idle();
        Round3Assert(await PlacesAt(6), "D5", "SearchUp checks only smaller numbers and skips whole-duration occupancy");
        Apply(palette.Layer with { SearchMode = LayerSearchMode.SearchDown }, Occupied(8), Occupied(9)); await Idle();
        Round3Assert(await PlacesAt(10), "D6", "SearchDown checks only larger numbers even though lower numbers are free");
        var sourceLayerPolicy = palette.Layer with { UseTemplateLayer = true, SearchMode = LayerSearchMode.SearchUp, Minimum = 0, Maximum = 99 };
        Apply(sourceLayerPolicy, Occupied(60)); await Idle();
        Assert(vm.GenericLayerTarget is { Target: "", OccupiedBehavior: LayerSearchMode.SearchUp },
            "LAYER_UX P2 blank Generic target represents the template Source layer with an explicit escape direction");
        Round3Assert(await PlacesAt(59), "D12", "blank Generic layer uses the template's original layer first, then escapes Up when occupied");
        var noWrap = true;
        foreach (var mode in new[] { LayerSearchMode.SearchUp, LayerSearchMode.SearchDown })
        {
            var edge = mode == LayerSearchMode.SearchUp ? 4 : 12;
            Apply(palette.Layer with { Preferred = edge, SearchMode = mode }, Occupied(edge)); await Idle();
            before = Signature(timeline); Tile(); await Idle(); noWrap &= vm.HasError && Signature(timeline) == before;
        }
        Round3Assert(noWrap, "D7", "both directions stop at saved bounds and never wrap toward an available opposite-side layer");
        var original = Occupied(8); Apply(palette.Layer with { SearchMode = LayerSearchMode.SearchUp }, original); await Idle();
        Tile(); await Idle();
        Round3Assert(timeline.Items.Contains(original) && original.Frame == 109 && original.Layer == 8 && original.Length == 30 && timeline.Items.Count == 2,
            "D8", "collision escape adds the clone only; the existing object is not moved, shortened, replaced or deleted");
        await undo.UndoAsync(); await Idle();
        var planned = Occupied(7);
        Round3Assert(LayerPlanner.Find(100, 20, 60, palette.Layer with { SearchMode = LayerSearchMode.SearchUp }, CharacterLayerMode.Base, null, new IItem[] { original, planned }) == 6,
            "D9", "shared LayerPlanner respects existing and already-planned occupancy under the same directional search");
        Apply(palette.Layer); await Idle(); before = Signature(timeline); var settingsBefore = JsonSerializer.Serialize(scope.Current);
        surface.GenericTargetBox.Text = "13"; await Idle();
        var invalid = !vm.ApplyGenericLayerTargetCommand.CanExecute(null) && !vm.ExecuteIntentTileCommand.CanExecute(vm.IntentTiles.Single());
        RejectWithoutMutation(timeline, vm.ApplyGenericLayerTarget, "R3-D out-of-bounds target rejects before saving or placement");
        invalid &= settingsBefore == JsonSerializer.Serialize(scope.Current);
        vm.ResetGenericLayerTargetCommand.Execute(null); vm.BeginIntentSettings(); vm.IntentSettings!.GenericSets.Single().Name = "未保存";
        vm.GenericLayerTarget!.Target = "9";
        invalid &= !vm.ApplyGenericLayerTargetCommand.CanExecute(null);
        RejectWithoutMutation(timeline, vm.ApplyGenericLayerTarget, "R3-D direct target save cannot overwrite another dirty Settings session");
        foreach (var policy in new[] { palette.Layer with { Minimum = 9 }, palette.Layer with { Maximum = 10000 }, palette.Layer with { SearchMode = (LayerSearchMode)999 } })
            RejectWithoutMutation(timeline, () => LayerPlanner.Find(100, 20, 60, policy, CharacterLayerMode.Base, null, timeline.Items), "R3-D invalid bounds/search enum reject before mutation");
        Round3Assert(invalid && Signature(timeline) == before && settingsBefore == JsonSerializer.Serialize(scope.Current), "D10", "invalid input and staged-settings conflict fail closed with original settings and Timeline unchanged");
        vm.ResetIntentSettings(); vm.ResetGenericLayerTargetCommand.Execute(null);
        var old = JsonSerializer.Deserialize<LayerPolicy>("{\"UseTemplateLayer\":false,\"Minimum\":4,\"Maximum\":8,\"Preferred\":8}")!;
        var templateLayer = old with { UseTemplateLayer = true };
        Round3Assert(old.SearchMode == LayerSearchMode.Legacy &&
            LayerPlanner.Find(100, 20, 60, old, CharacterLayerMode.Base, null, new[] { Occupied(8) }) == 4 &&
            LayerPlanner.Find(100, 20, 60, templateLayer, CharacterLayerMode.Base, null, Array.Empty<IItem>()) == 60 &&
            !JsonSerializer.Serialize(old).Contains("SearchMode", StringComparison.Ordinal),
            "D11", "absent search field preserves legacy range wrap and template-layer semantics; default serialization is unchanged");
        Apply(palette.Layer); await Idle(); SaveNamedView(view, "v042-round3-layer-360.png");
        Round3Phase("D", "hands-on-round3-layer.json"); Log("HANDS_ON_ROUND3_D=PASS"); Log("LAYER_UX_P2=PASS");
    }
}
