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
    private static async Task VerifyHandsOnRound3Settings(Timeline timeline, UndoRedoManager undo)
    {
        stage = "R3-C direct targets and non-destructive hidden compatibility UI";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var view = View!; var surface = view.RelativeSettingsSurface;
        var source = scope.AddTemplate("R3C/legacy source", new TextItem { Length = 12 });
        var voice = new VoiceItem(new Character { Name = "R3-C" }) { Frame = 50, Layer = 20, Length = 40 };
        var set = new IntentPalette(Guid.NewGuid(), "保存済みSet", "  legacy/intent  ",
            new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))] }, new(), [new(source.Id)]);
        var multi = new IntentPalette(Guid.NewGuid(), "以前の複数種類Set", "legacy/multi",
            new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem)), IntentSelectionContext.TypeKey(typeof(TextItem))],
                TypeMatch = IntentTypeMatch.ExactMixedTypes, MinimumCount = 2, MaximumCount = 2 }, new(), [new(source.Id)]);
        var palette = new PaletteDefinition(Guid.NewGuid(), PaletteKind.Style, "古いパレット", null, [source.Id])
            { Layer = new() { UseTemplateLayer = false, Preferred = 9, Minimum = 2, Maximum = 80 } };
        var fixture = PlacerSettingsStore.Copy(scope.Original); fixture.Library = [source]; fixture.Palettes = [palette]; fixture.IntentPalettes = [set, multi];
        fixture.ExpressionBootstrapComplete = true; fixture.LegacyWorkspace = true;
        fixture.ManualStylePaletteId = palette.Id; fixture.ManualCharacterPaletteId = null;
        scope.Apply(fixture, [voice], [voice]); vm.BeginIntentSettings(); view.SelectionTab.IsSelected = true; await Idle();
        var buttons = RelativeVisuals(surface.SettingsTargetButtons).OfType<Button>().ToArray();
        var session = vm.IntentSettings!; var direct = session.DirectItemContexts.ToArray();
        Round3Assert(direct.Length == session.KnownTypes.Count + 2 && buttons.Length == direct.Length && buttons.All(x => x.IsVisible) &&
            direct.All(x => buttons.Any(b => ReferenceEquals(b.CommandParameter, x))) && direct.Count(x => x.IsMultiTypeCompatibility) == 1 &&
            surface.FindName("OtherSettingsTargets") == null,
            "C1", "every known target type plus Generic is direct, and existing multi-type data adds one bounded compatibility parent");
        Round3Assert(new[] { "ボイス", "テキスト", "画像", "図形", "音声", "動画", "表情", "立ち絵" }.All(x => direct.Any(c => c.Label == x)) &&
            direct.All(x => !x.Label.EndsWith("Item", StringComparison.Ordinal)),
            "C2", "current built-in types use Japanese display names, never CLR identifiers");
        session.SelectedItemContext = direct.Single(x => x.IsMultiTypeCompatibility); await Idle();
        Round3Assert(!surface.TargetAdvanced.IsExpanded && surface.FindName("TargetTypeChoices") == null && surface.FindName("TypeMatchPanel") == null &&
            direct.Where(x => !x.IsMultiTypeCompatibility).All(x => x.TypeKeys.Count <= 1) && surface.TargetAdvanced.Header?.ToString() == "対象の詳細" &&
            session.VisiblePalettes.Cast<IntentPaletteDraft>().Single().Id == multi.Id && session.SelectedPalette!.IsLegacyMultiType && !session.CanCreateForContext,
            "C3", "normal Settings has one Item owner while old multi-type data is preserved only in a non-creatable bounded parent");
        var compatibilityCopy = new IntentSettingsSession(fixture, new[] { typeof(VoiceItem), typeof(TextItem) });
        compatibilityCopy.SelectedItemContext = compatibilityCopy.ItemContexts.Single(x => x.IsMultiTypeCompatibility);
        compatibilityCopy.SelectedCopyDestination = compatibilityCopy.ItemContexts.Single(x => x.IsRealItemType && x.Key == IntentSelectionContext.TypeKey(typeof(TextItem)));
        compatibilityCopy.CopySelectedToOtherItem();
        var copiedSettings = compatibilityCopy.Build();
        var legacyRoundtrip = copiedSettings.IntentPalettes.Single(x => x.Id == multi.Id);
        var singleOwnerCopy = copiedSettings.IntentPalettes.Single(x => x.Id == compatibilityCopy.SelectedPalette!.Id);
        Assert(JsonSerializer.Serialize(legacyRoundtrip.Target) == JsonSerializer.Serialize(multi.Target) && singleOwnerCopy.Target.TypeMatch == IntentTypeMatch.UniformType &&
            singleOwnerCopy.Target.ItemTypeKeys.SequenceEqual(new[] { IntentSelectionContext.TypeKey(typeof(TextItem)) }),
            "R3-C existing multi-type Set can be snapshot-copied into one Item owner without rewriting the legacy source");
        var legacyCommands = new[] { vm.OpenLegacyWorkspaceCommand, vm.CloseLegacyWorkspaceCommand };
        Round3Assert(!RelativeVisuals(view).OfType<Button>().Any(x => legacyCommands.Contains(x.Command)) &&
            !RelativeVisuals(surface).OfType<Expander>().Any(x => x.Header?.ToString()?.Contains("互換", StringComparison.Ordinal) == true),
            "C4", "normal Settings has no compatibility workspace entry or toggle command");
        view.PaletteTab.IsSelected = true; await Idle();
        Round3Assert(ReferenceEquals(view.PaletteTab.Content, view.RelativePaletteSurface) &&
            !RelativeVisuals(view).OfType<Button>().Any(x => x.Content?.ToString() == "相対パレットへ戻る" || legacyCommands.Contains(x.Command)),
            "C5", "normal placement has no legacy return/mode affordance");
        var path = Path.Combine(output, "round3-legacy-settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(fixture)); var bytes = File.ReadAllBytes(path);
        var store = new PlacerSettingsStore(path); var loaded = store.Load();
        var sameData = JsonSerializer.Serialize(loaded) == JsonSerializer.Serialize(fixture);
        store.Save(loaded); var saved = new PlacerSettingsStore(path).Load();
        Round3Assert(sameData && JsonSerializer.Serialize(saved) == JsonSerializer.Serialize(fixture) &&
            saved.ExpressionPresets.Count > 0 && saved.SelectionPresets.Count > 0 && saved.IntentPalettes.Single(x => x.Id == set.Id).Intent == set.Intent &&
            JsonSerializer.Serialize(saved.IntentPalettes.Single(x => x.Id == multi.Id).Target) == JsonSerializer.Serialize(multi.Target) && saved.Palettes.Single().Layer == palette.Layer,
            "C6", "old Library/Palette/Preset/Intent/QuickDrop and multi-type target data retain exact meanings across real protected load/save");
        var currentSignature = Signature(timeline); var currentSettings = JsonSerializer.Serialize(scope.Current);
        PlacerViewModel? fresh = null; var startsCurrent = false;
        try
        {
            fresh = new PlacerViewModel();
            typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(fresh, loaded);
            fresh.ActivateIntentWorkspace(); startsCurrent = !fresh.UseLegacyWorkspace && loaded.LegacyWorkspace;
        }
        finally { fresh?.Dispose(); ViewModel = vm; }
        Round3Assert(startsCurrent, "C7", "a fresh root starts in current workspace even while the loaded LegacyWorkspace flag remains true");
        var untouched = Path.Combine(output, "round3-legacy-readonly.json"); File.WriteAllBytes(untouched, bytes);
        _ = new PlacerSettingsStore(untouched).Load();
        Round3Assert(File.ReadAllBytes(untouched).SequenceEqual(bytes) && Signature(timeline) == currentSignature &&
            JsonSerializer.Serialize(scope.Current) == currentSettings, "C8", "startup/navigation/load do not migrate or rewrite old bytes, templates or Timeline");
        view.SelectionTab.IsSelected = true; view.Height = 440; await Idle(); SaveNamedView(view, "v042-round3-settings-direct-360.png");
        Round3Phase("C", "hands-on-round3-settings.json"); Log("HANDS_ON_ROUND3_C=PASS");
    }
}
