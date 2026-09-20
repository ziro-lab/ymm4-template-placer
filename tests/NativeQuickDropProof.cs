using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    private static async Task VerifyQuickDrop(Timeline timeline, UndoRedoManager undo)
    {
        stage = "W5 Quick Drop";
        var vm = ViewModel!; var view = View!; timeline.SelectedItems = [];
        var initial = Signature(timeline); var originalFrame = timeline.CurrentFrame;
        var ca = timeline.Items.OfType<VoiceItem>().Single(x => x.CharacterName == "TestA").Character;
        var cb = timeline.Items.OfType<VoiceItem>().Single(x => x.CharacterName == "TestB").Character;
        var source = new TachieFaceItem(ca) { Frame = 7, Length = 37, Layer = 12, Remark = "source note\nCWT_TPL:S=77;P=expression" };
        var template = Template("W5/Overlay", [source]); ItemSettings.Default.Templates.Add(template);
        vm.RefreshLibraryCommand.Execute(null); vm.SelectedSourceTemplate = template; vm.LibraryDisplayName = "手"; var entry = vm.RegisterLibrary();
        vm.ActivePaletteKind = PaletteKind.Character; vm.ManualCharacterPalette = vm.CharacterPalettes.Single(x => x.CharacterName == "TestA");
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
        vm.QuickDropMode = CharacterLayerMode.Base;
        timeline.CurrentFrame = 321; ShowTask(view, "palette"); await Idle();
        var panel = view.PaletteSurface; var row = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        panel.PaletteList.ScrollIntoView(row); await Idle(); panel.PaletteList.UpdateLayout();
        var container = panel.PaletteList.ItemContainerGenerator.ContainerFromItem(row) as ListBoxItem;
        Assert(container != null, "W5 real Palette entry has a native WPF item container");
        container!.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        await Idle();
        var added = timeline.Items.Where(x => x.Frame == 321 && x.Layer == 12).Single();
        Assert(!vm.HasError && added is TachieFaceItem && added.Length == 37, "W5 actual entry double-click places at public CurrentFrame with intrinsic Length");
        Assert(added.Remark.Contains("source note", StringComparison.Ordinal) && !added.Remark.Contains("CWT_TPL:", StringComparison.Ordinal), "W5 Quick Drop strips inherited plugin association only from its independent clone");
        Assert(source.Frame == 7 && source.Length == 37 && source.Layer == 12 && source.Remark == "source note\nCWT_TPL:S=77;P=expression", "W5 source Template body and Remark remain unchanged");
        Assert(vm.Status.Contains("321", StringComparison.Ordinal) && vm.Status.Contains("12", StringComparison.Ordinal), "W5 feedback states actual Frame and Layer");
        var after = Signature(timeline); await undo.UndoAsync(); await Idle();
        Assert(Signature(timeline) == initial, "W5 one native Undo removes the complete Quick Drop");
        await undo.RedoAsync(); await Idle(); Assert(Signature(timeline) == after, "W5 one native Redo restores the same Quick Drop");
        await undo.UndoAsync(); await Idle();
        panel.PaletteList.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        await Idle(); Assert(Signature(timeline) == initial, "W5 list background double-click never repeats a previously selected entry");
        vm.PaletteMinimumText = "invalid";
        Assert(!vm.QuickDropCommand.CanExecute(null), "W5 invalid or unsaved numeric edits disable placement instead of using stale numbers");
        RejectWithoutMutation(timeline, vm.SavePaletteLayer, "W5 invalid Layer text is rejected before saving or placement");
        RejectWithoutMutation(timeline, () => vm.QuickDrop(), "W5 direct placement also rejects unsaved Layer edits");
        vm.PaletteMinimumText = "10"; vm.PaletteMaximumText = "20"; vm.PalettePreferredText = "15"; vm.PaletteUseTemplateLayer = false; vm.SavePaletteLayer();
        var baseBlocker = new TachieFaceItem(cb) { Frame = 410, Length = 8, Layer = 15 };
        undo.Record(); timeline.Items = timeline.Items.Add(baseBlocker); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        timeline.CurrentFrame = 400; vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        var baseDrop = vm.QuickDrop();
        Assert(baseDrop.Layer == 16 && baseDrop.Length == 37, "W5 Base band skips a preferred Layer blocked only later in the full duration");
        await undo.UndoAsync(); await Idle();
        undo.Record(); timeline.Items = timeline.Items.Remove(baseBlocker); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        Assert(Signature(timeline) == initial, "W5 Base placement and native undo preserve every prior Item");
        Log("W5=PASS"); stage = "W6 Character Layer";
        IItem[] fixture = [new VoiceItem(ca) { Frame = 500, Length = 60, Layer = 10, Serif = "Layer probe" },
            new TachieFaceItem(ca) { Frame = 500, Length = 60, Layer = 18 }, new TachieFaceItem(ca) { Frame = 500, Length = 60, Layer = 22 },
            new TachieFaceItem(cb) { Frame = 500, Length = 60, Layer = 40 }, new TachieFaceItem(cb) { Frame = 540, Length = 10, Layer = 23 },
            new TachieFaceItem(cb) { Frame = 540, Length = 10, Layer = 9 }];
        undo.Record(); timeline.Items = timeline.Items.AddRange(fixture); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        var fixtureState = Signature(timeline);
        vm.PaletteMinimumText = "0"; vm.PaletteMaximumText = "50"; vm.PalettePreferredText = "15"; vm.SavePaletteLayer();
        vm.QuickDropMode = CharacterLayerMode.Front; timeline.CurrentFrame = 520;
        vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id); var front = vm.QuickDrop();
        Assert(front.Layer == 24 && front.Frame == 520 && front.Length == 37, "W6 Front uses same-Character max+1 and skips the late blocker at Layer 23");
        Assert(front.Layer < 40, "W6 unrelated Character Layer 40 is excluded from the ordering baseline");
        Assert(timeline.Items.OfType<TachieFaceItem>().Count(x => Equals(x.Character, ca) && x.Frame <= 520 && x.Frame + x.Length > 520) >= 3, "W6 multiple same-Character Faces coexist on separate Layers");
        await undo.UndoAsync(); await Idle(); Assert(Signature(timeline) == fixtureState, "W6 Front is one native Undo and leaves original occupancy intact");
        vm.QuickDropMode = CharacterLayerMode.Back; vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id); var back = vm.QuickDrop();
        Assert(back.Layer == 8 && back.Length == 37, "W6 Back uses same-Character min-1 and skips the late blocker at Layer 9");
        await undo.UndoAsync(); await Idle();
        timeline.CurrentFrame = 900; vm.QuickDropMode = CharacterLayerMode.Front; vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        Assert(vm.QuickDrop().Layer == 15, "W6 no overlapping same-Character context falls back to Base preferred Layer");
        await undo.UndoAsync(); await Idle(); timeline.CurrentFrame = 520;
        vm.PaletteMaximumText = "23"; vm.SavePaletteLayer(); vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        RejectWithoutMutation(timeline, () => vm.QuickDrop(), "W6 exhausted Front direction never wraps to a free lower Layer");
        vm.PaletteMinimumText = "9"; vm.PaletteMaximumText = "50"; vm.SavePaletteLayer(); vm.QuickDropMode = CharacterLayerMode.Back; vm.SelectedPaletteEntry = vm.PaletteEntries.Single(x => x.LibraryEntryId == entry.Id);
        RejectWithoutMutation(timeline, () => vm.QuickDrop(), "W6 exhausted Back direction never wraps to a free higher Layer");
        var first = TemplateResolver.Clone(entry); first.Frame = 700; first.Layer = 30;
        var second = TemplateResolver.Clone(entry); second.Frame = 700; second.Layer = 30;
        RejectWithoutMutation(timeline, () => PlacementPlan.Create(timeline, [first, second]), "W6 batch preflight reserves already-planned occupancy and refuses partial additions");
        vm.PaletteMinimumText = "0"; vm.SavePaletteLayer(); vm.QuickDropMode = CharacterLayerMode.Front;
        vm.ActivePaletteKind = PaletteKind.Style; vm.ManualStylePalette = vm.StylePalettes.Single(x => x.Name == "戦闘");
        vm.PaletteLibraryChoice = vm.PaletteLibraryChoices.Single(x => x.Id == entry.Id); vm.AddPaletteEntry();
        var styleDrop = vm.QuickDrop(); Assert(styleDrop.Layer == 12, "W6 Style Quick Drop always uses Base regardless of remembered Character mode");
        await undo.UndoAsync(); await Idle(); vm.ActivePaletteKind = PaletteKind.Character;
        Assert(vm.QuickDropMode == CharacterLayerMode.Front && new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load().CharacterQuickDropMode == CharacterLayerMode.Front,
            "W6 Character Front/Base/Back choice persists across Style switches and settings reload");
        undo.Record(); timeline.Items = timeline.Items.RemoveAll(x => fixture.Contains(x)); timeline.RefreshTimelineLengthAndMaxLayer(); undo.Record();
        timeline.CurrentFrame = originalFrame;
        Assert(Signature(timeline) == initial, "W6 all original Items retain geometry and remarks after placement/undo proof");
        panel.DropSurface.LayerEditor.IsExpanded = false; panel.PaletteEditor.IsExpanded = false; await Idle(); SaveView(view); ShowTask(view, "expression"); await Idle();
        Log("W6=PASS");
    }
}
