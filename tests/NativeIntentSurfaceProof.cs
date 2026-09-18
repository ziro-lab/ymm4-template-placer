using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyIntentSurface(Timeline timeline, UndoRedoManager undo)
    {
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var originalSettings = (PlacerSettings)field.GetValue(vm)!;
        var before = timeline.Items; var selection = timeline.SelectedItems;
        var originalMode = vm.UseLegacyWorkspace;
        var character = new Character { Name = "R7 Character" };
        var voice = new VoiceItem(character) { Frame = 100, Length = 40, Layer = 20 };
        var face = new TachieFaceItem(character) { Frame = 10, Length = 10, Layer = 8 };
        var source = Template("R7/Face", [face]); ItemSettings.Default.Templates.Add(source);
        try
        {
            var entry = TemplateResolver.Reference(source, "笑顔", character.Name);
            var tile = new IntentEntry(entry.Id);
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var palette = new IntentPalette(Guid.NewGuid(), "セット1", "表情", target, new(), [tile]) { ExpressionCandidates = true };
            var second = palette with { Id = Guid.NewGuid(), Name = "セット2" };
            var otherIntent = palette with { Id = Guid.NewGuid(), Intent = "リアクション" };
            var facePalette = palette with { Id = Guid.NewGuid(), Intent = "装飾", Target = target with { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(TachieFaceItem))] } };
            var fixture = PlacerSettingsStore.Copy(originalSettings);
            fixture.IntentPaletteRevision = 1; fixture.ExpressionBootstrapComplete = true;
            fixture.Library = [entry]; fixture.IntentPalettes = [palette, second, otherIntent, facePalette];
            field.SetValue(vm, fixture);
            timeline.Items = [voice]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.RefreshIntentWorkspace();
            view.PaletteTab.IsSelected = true; await Idle();
            Assert(ReferenceEquals(view.PaletteTab.Content, view.RelativePaletteSurface) && !ReferenceEquals(view.SelectionTab.Content, view.SelectionSurface),
                "R7 normal workspace uses intent actions; legacy Selection/Library decisions are not the normal action surface");
            Assert(vm.IntentSets.Select(x => x.Targeted!.Intent).SequenceEqual(new[] { "表情", "表情", "リアクション" }), "R7/R2-B runtime Voice context directly exposes every matching Set, retaining compatibility Intent values");
            Assert(vm.IntentSets.Count == 3 && vm.HasIntentSets && vm.IntentTiles.Count == 1, "R7/R2-B distinct purposes remain independent Sets with concrete tiles on one surface");
            var signature = Signature(timeline); var settingsJson = JsonSerializer.Serialize(fixture);
            view.RelativePaletteSurface.IntentSetPicker.SelectedIndex = 1; await Idle();
            Assert(vm.SelectedIntentSet?.Targeted?.Id == second.Id && Signature(timeline) == signature && JsonSerializer.Serialize(fixture) == settingsJson,
                "R7 native set selection changes neither Timeline nor persisted settings model");
            view.RelativePaletteSurface.IntentSetSegments.SelectedIndex = 2; await Idle();
            Assert(vm.SelectedIntentSet?.Id == otherIntent.Id && Signature(timeline) == signature, "R7/R2-B native Set selection crosses old Intent categories without any Timeline write");
            view.RelativePaletteSurface.IntentSetSegments.SelectedIndex = 0; await Idle();
            view.RelativePaletteSurface.UpdateLayout();
            Button? FindButton(DependencyObject root)
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                {
                    var child = VisualTreeHelper.GetChild(root, i);
                    if (child is Button button && button.CommandParameter is IntentTileChoice) return button;
                    var found = FindButton(child); if (found != null) return found;
                }
                return null;
            }
            var button = FindButton(view.RelativePaletteSurface.IntentTileItems) ?? throw new InvalidOperationException("R7 tile button was not realized");
            Assert(button.Command == vm.ExecuteIntentTileCommand && button.IsEnabled, "R7 native tile button is wired to the single-click placement command");
            new System.Windows.Automation.Peers.ButtonAutomationPeer(button).Invoke(); await Idle();
            var added = timeline.Items.Except(new IItem[] { voice }).ToArray();
            Assert(added.Length == 1 && added[0].Frame == 100 && added[0].Length == 40 && added[0].Layer == 19,
                "R7 native one-click tile invokes saved relative span/up relation without profile/layer input");
            await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == signature, "R7 one native Undo restores a complete tile action");
            timeline.Items = timeline.Items.Add(face); timeline.SelectedItems = [face]; await Idle();
            Assert(vm.IntentSets.Count == 1 && vm.IntentSets[0].Targeted?.Intent == "装飾", "R7/R2-B changing runtime type replaces applicable Sets without showing unrelated actions");
            timeline.SelectedItems = [voice, face]; await Idle();
            Assert(vm.IntentSets.Count == 0 && vm.IntentTiles.Count == 0, "R7 mixed selection never guesses a common palette");
            timeline.SelectedItems = []; await Idle();
            Assert(vm.IntentTiles.Count == 0 && vm.IntentNotice.Contains("選択"), "R7 no selection presents a useful empty state, not the full Library");
            timeline.SelectedItems = [voice]; await Idle();
            var bounds = view.RelativePaletteSurface.RenderSize;
            Assert(bounds.Width > 0 && bounds.Height > 0, "R7 native relative action surface has nonzero layout bounds");
            var bitmap = new RenderTargetBitmap(Math.Max(1, (int)bounds.Width), Math.Max(1, (int)bounds.Height), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view.RelativePaletteSurface); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(Path.Combine(output, "r7-intent-surface.png"))) encoder.Save(stream);
            Log("R7=PASS"); Log("R9_UI=PASS");
        }
        finally
        {
            ItemSettings.Default.Templates.Remove(source); field.SetValue(vm, originalSettings);
            timeline.Items = before; timeline.SelectedItems = selection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.SetLegacyWorkspace(originalMode); vm.RefreshIntentWorkspace();
        }
    }
}
