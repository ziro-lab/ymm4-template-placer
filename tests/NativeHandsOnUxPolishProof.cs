using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyHandsOnAppearance(Timeline timeline, UndoRedoManager undo)
    {
        stage = "H1/H2 hands-on Item-first Settings and placement tiles";
        var vm = ViewModel!; var view = View!;
        var field = typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = (PlacerSettingsStore)typeof(PlacerViewModel).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var original = (PlacerSettings)field.GetValue(vm)!; var items = timeline.Items; var selection = timeline.SelectedItems;
        var mode = vm.UseLegacyWorkspace; var width = view.Width; var height = view.Height;
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var character = new Character { Name = "Hands-on Character" };
        var voice = new VoiceItem(character) { Frame = 120, Length = 60, Layer = 20 };
        var text = new TextItem { Frame = 300, Length = 40, Layer = 25 };
        var sources = new[] {
            Template("小夜/表情/うれしい", [new TachieFaceItem(character) { Length = 15, Layer = 4 }]),
            Template("小夜/別セット/うれしい", [new TachieFaceItem(character) { Length = 15, Layer = 4 }]),
            Template("小夜/表情/とても長い名前の候補が折り返しても隣のタイルを押し出さない", [new TachieFaceItem(character) { Length = 15, Layer = 4 }])
        };
        foreach (var source in sources) ItemSettings.Default.Templates.Add(source);
        try
        {
            var library = sources.Select(x => TemplateResolver.Reference(x, x.Name, character.Name)).ToList();
            var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = character.Name };
            var palette = new IntentPalette(Guid.NewGuid(), "表情セット", "表情", target, new(), library.Select(x => new IntentEntry(x.Id)).ToList()) { ExpressionCandidates = true };
            var textPalette = palette with { Id = Guid.NewGuid(), Name = "文字用", Intent = "文字の演出", ExpressionCandidates = false,
                Target = new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(TextItem))] }, Entries = [] };
            var fixture = PlacerSettingsStore.Copy(original); fixture.Library = library; fixture.IntentPalettes = [palette, textPalette];
            fixture.LegacyWorkspace = false; fixture.ExpressionBootstrapComplete = true; fixture.IntentPaletteRevision = 1;
            field.SetValue(vm, fixture); timeline.Items = [voice, text]; timeline.SelectedItems = [voice]; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            vm.ActivateIntentWorkspace(); vm.SetLegacyWorkspace(false); vm.Refresh(); vm.ResetIntentSettings();
            vm.OpenIntentSettingsCommand.Execute(null); await Idle();
            var panel = view.RelativeSettingsSurface; var session = vm.IntentSettings!; var signature = Signature(timeline);
            Assert(panel.SettingsItemPicker.SelectedItem is IntentSettingsItemContext { IsCurrentSelection: true } context && context.Label.Contains("ボイス", StringComparison.Ordinal) &&
                session.Intents.SequenceEqual(new[] { "表情" }) && session.VisiblePalettes.Cast<IntentPaletteDraft>().Single().Id == palette.Id,
                "H1 real Item-first Settings start at the selected Voice and expose only its intent and Sets");
            Assert(!panel.TargetAdvanced.IsExpanded && !panel.TargetTypeChoices.IsVisible && !session.HasChanges,
                "H1 the runtime type checkbox matrix is hidden and opening Settings is not a draft edit");
            session.SelectedItemContext = session.ItemContexts.Single(x => !x.IsCurrentSelection && x.TypeKeys.Contains(IntentSelectionContext.TypeKey(typeof(TextItem)))); await Idle();
            Assert(session.Intents.SequenceEqual(new[] { "文字の演出" }) && session.SelectedPalette?.Id == textPalette.Id && !session.HasChanges,
                "H1 Item navigation filters the Set without making a clean draft dirty");
            session.ShowAllSets = true; await Idle();
            Assert(session.VisiblePalettes.Cast<IntentPaletteDraft>().Count() == 2 && view.MainTabs.Items.Count == 3 && !session.HasChanges,
                "H1 Set management stays inside Settings, retains all Sets and adds no fourth task");
            session.ShowAllSets = false; session.SelectedItemContext = session.ItemContexts.Single(x => x.IsCurrentSelection); await Idle();
            var draft = session.SelectedPalette!; draft.SelectedEntry = draft.Entries[0]; await Idle();
            Assert(!session.HasChanges && panel.EntryAppearanceEditor.IsVisible && Signature(timeline) == signature,
                "H1/H2 choosing an entry for alias/color editing is navigation only and writes zero Timeline items");
            var locators = library.Select(x => x.Source).ToArray();
            draft.Entries[0].DisplayAlias = "にっこり"; draft.Entries[0].Color = IntentTileColor.Rose;
            var built = session.Build();
            Assert(built.IntentPalettes[0].Entries[0].DisplayAlias == "にっこり" && built.IntentPalettes[0].Entries[0].Color == IntentTileColor.Rose &&
                built.Library.Select(x => x.Source).SequenceEqual(locators) && sources[0].Name == "小夜/表情/うれしい" && Signature(timeline) == signature,
                "H2 alias and finite color are draft-only entry metadata; live Template names and locators do not change");
            await InvokeSelectionButton(panel.DiscardButton); session = vm.IntentSettings!;
            Assert(session.SelectedPalette!.Entries[0].DisplayAlias == "" && session.SelectedPalette!.Entries[0].Color == IntentTileColor.Neutral && !session.HasChanges,
                "H2 Discard restores neutral/no-alias metadata without writing the Timeline");
            session.SelectedPalette!.Entries[0].DisplayAlias = "にっこり"; session.SelectedPalette!.Entries[0].Color = IntentTileColor.Rose;
            await InvokeSelectionButton(panel.SaveButton); await Idle();
            var saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            Assert(!vm.HasError && saved.IntentPalettes[0].Entries[0].DisplayAlias == "にっこり" && saved.IntentPalettes[0].Entries[0].Color == IntentTileColor.Rose && Signature(timeline) == signature,
                "H2 actual native Save roundtrips alias/color through the existing protected store with Timeline zero-write");
            view.PaletteTab.IsSelected = true; view.Width = 360; view.Height = 440; await Idle();
            var surface = view.RelativePaletteSurface; surface.UpdateLayout();
            Assert(view.PaletteTab.Header?.ToString() == "配置" && vm.IntentTiles[0].Label == "にっこり" && vm.IntentTiles[1].Label == "うれしい" &&
                vm.IntentTiles.All(x => !x.Label.Contains('/')),
                "H2 placement wording, explicit alias and last-segment fallback appear on the real tile surface");
            var labels = IntentTileAppearance.Distinguish(["うれしい", "うれしい", "うれしい (1)"]);
            Assert(labels.Distinct(StringComparer.Ordinal).Count() == 3 && labels[2] == "うれしい (1)",
                "H2 duplicate short names remain distinct even when a real alias already contains the generated suffix");
            var buttons = RelativeVisuals(surface.IntentTileItems).OfType<Button>().Where(x => x.CommandParameter is IntentTileChoice).ToArray();
            var cells = buttons.Select(x => (Grid)VisualTreeHelper.GetParent(x)).ToArray();
            Assert(cells.Length == 3 && cells.All(x => Math.Abs(x.ActualWidth - cells[0].ActualWidth) < 1 && Math.Abs(x.ActualHeight - cells[0].ActualHeight) < 1 &&
                Math.Abs(x.ActualWidth - x.ActualHeight) < 12 && x.TranslatePoint(new Point(x.ActualWidth, 0), surface).X <= surface.ActualWidth + 1),
                "H2 native 360px layout uses uniform near-square cells regardless of long names");
            var handle = RelativeVisuals(cells[0]).OfType<Border>().Single(x => x.Name == "TileDragHandle");
            Assert(handle.Cursor == Cursors.SizeAll && buttons.All(x => !x.IsAncestorOf(handle)) && handle.BorderBrush != null,
                "H2 reorder has a dedicated sibling handle; label colors do not replace action-button text/background semantics");
            var down = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent };
            var up = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent };
            handle.RaiseEvent(down); handle.RaiseEvent(up);
            Assert(down.Handled && up.Handled && Signature(timeline) == signature,
                "H2 native handle down/up are consumed and never invoke placement");
            var oldTiles = vm.IntentTiles.ToArray();
            // WPF exposes no public fixture constructor. This test-only constructor creates routed drag input;
            // production uses only public DragDrop events and never reflection.
            var ctor = typeof(DragEventArgs).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                [typeof(IDataObject), typeof(DragDropKeyStates), typeof(DragDropEffects), typeof(DependencyObject), typeof(Point)], null)!;
            var drop = (DragEventArgs)ctor.Invoke([new DataObject(typeof(IntentTileChoice), oldTiles[0]), DragDropKeyStates.LeftMouseButton,
                DragDropEffects.Move, cells[2], new Point(5, 5)]);
            drop.RoutedEvent = DragDrop.DropEvent; cells[2].RaiseEvent(drop); await Idle();
            saved = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
            Assert(drop.Handled && !vm.HasError && saved.IntentPalettes[0].Entries.Select(x => x.LibraryEntryId).SequenceEqual(new[] { library[1].Id, library[2].Id, library[0].Id }) &&
                Signature(timeline) == signature && saved.IntentPalettes[0].Entries.Last().DisplayAlias == "にっこり" && saved.IntentPalettes[0].Entries.Last().Color == IntentTileColor.Rose,
                "H2 routed native drop persists existing Entries order, retains appearance metadata and performs zero placement");
            Assert(!vm.ReorderIntentTileCommand.CanExecute(new IntentTileReorderRequest(oldTiles[0], oldTiles[2])),
                "H2 a stale drag object cannot reorder a refreshed or changed Set");
            vm.ResetIntentSettings(); vm.IntentSettings!.SelectedPalette!.Entries[0].DisplayAlias = "未保存";
            Assert(!vm.ReorderIntentTileCommand.CanExecute(new IntentTileReorderRequest(vm.IntentTiles[0], vm.IntentTiles[1])),
                "H2 direct reorder cannot overwrite a pending Settings draft");
            vm.ResetIntentSettings();
            var defaults = JsonSerializer.Deserialize<IntentEntry>($"{{\"LibraryEntryId\":\"{library[0].Id}\"}}")!;
            Assert(defaults.DisplayAlias == null && defaults.Color == IntentTileColor.Neutral, "H2 old entry metadata loads as no alias and neutral without a schema bump");
            foreach (var pair in new (Type Type, string Name)[] { (typeof(TransitionItem), "場面切り替え"), (typeof(FrameBufferItem), "画面の複製"), (typeof(EffectItem), "エフェクト"),
                (typeof(VoiceItem), "ボイス"), (typeof(VideoItem), "動画"), (typeof(ImageItem), "画像"), (typeof(AudioItem), "音声"), (typeof(TextItem), "テキスト"), (typeof(TachieItem), "立ち絵"), (typeof(ShapeItem), "図形") })
                Assert(ItemDisplayNames.For(pair.Type) == pair.Name, "H1/H5 pinned Japanese Item vocabulary: " + pair.Name);
            Assert(ItemDisplayNames.For(typeof(NativeProof)) == "追加アイテム", "H1/H5 unknown types never expose namespace/CLR metadata to the normal UI");
            SaveNamedView(view, "v042-hands-on-tiles-360.png");
            Log("HANDS_ON_H1_H2=PASS");
        }
        finally
        {
            foreach (var source in sources) ItemSettings.Default.Templates.Remove(source);
            if (disk != null) File.WriteAllBytes(PlacerSettingsStore.DefaultPath, disk); else if (File.Exists(PlacerSettingsStore.DefaultPath)) File.Delete(PlacerSettingsStore.DefaultPath);
            store.Load(); field.SetValue(vm, original); timeline.Items = items; timeline.SelectedItems = selection; timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
            view.Width = width; view.Height = height; vm.SetLegacyWorkspace(mode); vm.Refresh(); vm.ResetIntentSettings();
        }
    }
}
