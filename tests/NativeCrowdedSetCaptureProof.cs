using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;
internal static partial class NativeProof
{
    // First capture the existing crowded surface; promote the explicit layout
    // checks with the product candidate, without changing this fixture.
    private static readonly bool CrowdedSetCandidate = true;
    private static void VerifyCrowdedPopupBounds(FrameworkElement popup, Window host, int width)
    {
        var screen = popup.PointToScreen(new Point()); var origin = host.PointToScreen(new Point());
        Assert(screen.X - origin.X >= -1 && screen.X - origin.X + popup.ActualWidth <= width + 1 &&
            screen.Y - origin.Y >= -1 && screen.Y - origin.Y + popup.ActualHeight <= host.ActualHeight + 1,
            "CROWDED_SETS native dropdown stays inside the exact captured viewport without horizontal clipping");
    }
    private static async Task VerifyCrowdedSetCapture(Timeline timeline, UndoRedoManager undo)
    {
        stage = "crowded Set visual evidence";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!; var dock = View!;
        var source = scope.AddTemplate("混雑確認/演出", new TextItem { Length = 30, Layer = 5 });
        var voice = new VoiceItem(new Character { Name = "霊夢" }) { Frame = 100, Length = 60, Layer = 20 };
        const string sharedPrefix = "同じ長い名前で始まる配置セット・ボイスに合わせる演出";
        string[] names = ["短い", sharedPrefix + "／前半", sharedPrefix + "／後半", "画面の印象を強める、とても長い名前のセット"];
        var target = new IntentTargetContext { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))], CharacterName = "霊夢" };
        var sets = names.Select(name => new IntentPalette(Guid.NewGuid(), name, "演出", target, new(), [new(source.Id)])).ToArray();
        var all = sets.Concat(Enumerable.Range(5, 8).Select(i => new IntentPalette(Guid.NewGuid(),
            $"{(i <= 7 ? "霊夢" : i <= 9 ? "共通" : "魔理沙")}・多数セット{i:00}・長い名前の比較用", "演出",
            target with { CharacterName = i <= 7 ? "霊夢" : i <= 9 ? null : "魔理沙" }, new(), [new(source.Id)]))).ToArray();
        var images = new List<object>();
        PlacerView? capture = null; Window? window = null;
        try
        {
            capture = new PlacerView { DataContext = vm, Width = 360, Height = 640 };
            window = new Window { Title = "Template Placer crowded Set capture", Content = capture,
                SizeToContent = SizeToContent.WidthAndHeight, WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
                Left = SystemParameters.WorkArea.Left + 16, Top = SystemParameters.WorkArea.Top + 16 };
            foreach (var many in new[] { false, true })
            {
                var fixture = PlacerSettingsStore.Copy(scope.Original);
                fixture.Library = [source]; fixture.IntentPalettes = (many ? all : sets).ToList();
                fixture.Palettes = []; fixture.ExpressionBootstrapComplete = true;
                scope.Apply(fixture, [voice], [voice]);
                vm.ActivateIntentWorkspace(); capture.PaletteTab.IsSelected = true;
                if (!window.IsVisible) window.Show();
                await Idle();
                vm.SelectedIntentSet = vm.IntentSets.Single(x => x.Id == sets[2].Id);
                await Idle();
                var savedBefore = JsonSerializer.Serialize(typeof(PlacerViewModel).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm));
                var timelineBefore = Signature(timeline);
                foreach (var width in new[] { 360, 640 })
                {
                    capture.Width = width; capture.PaletteTab.IsSelected = true; await Idle();
                    window.UpdateLayout(); capture.UpdateLayout();
                    var surface = capture.RelativePaletteSurface;
                    Assert(vm.IntentSets.Select(x => x.Id).SequenceEqual((many ? all : sets)
                        .Where(x => x.Target.CharacterName != "魔理沙").Select(x => x.Id)),
                        "CROWDED_SETS runtime filtering retains saved order and excludes other-character Sets");
                    Assert(surface.IntentSetSegments is TabControl && ReferenceEquals(surface.IntentSetSegments.SelectedItem, vm.SelectedIntentSet) &&
                        capture.PaletteTab.IsSelected && surface.FindName("IntentSetPicker") == null,
                        "CROWDED_SETS native tabs retain the exact selected model at every Set count and never switch MainTabs");
                    var scroll = surface.IntentSetHeaderScroll!;
                    var cursor = window.PointToScreen(new Point(5, 5));
                    Assert(Round2Input.SetCursorPos((int)cursor.X, (int)cursor.Y), "CROWDED_SETS cursor moves to neutral scene-heading space");
                    await Idle();
                    var items = RelativeVisuals(surface.IntentSetSegments).OfType<TabItem>().ToArray();
                    Assert(items.Length == vm.IntentSets.Count && items.All(x => !x.IsMouseOver) &&
                        items.All(x => Math.Abs(x.ActualWidth - items[0].ActualWidth) <= 1) &&
                        surface.IntentSetSegments.ActualWidth <= surface.ActualWidth + 1 &&
                        surface.IntentSetContentBorder.ActualWidth <= surface.ActualWidth + 1,
                        $"CROWDED_SETS equal-width native headers stay in one bounded strip at {width}px");
                    Assert(items.All(x => x.ToolTip?.ToString() == (x.DataContext as IntentSetChoice)?.Label &&
                        System.Windows.Automation.AutomationProperties.GetName(x) == (x.DataContext as IntentSetChoice)?.Label),
                        "CROWDED_SETS abbreviated similar names retain exact full tooltip/accessibility names");
                    Assert(surface.SingleSetTitleText.IsVisible && surface.SingleSetTitleText.Text == vm.SelectedIntentSet!.Label,
                        "CROWDED_SETS selected long/similar Set has a full-name title");
                    void AssertSelectedVisible()
                    {
                        var tab = items.Single(x => x.IsSelected);
                        var bounds = tab.TransformToAncestor(scroll).TransformBounds(new Rect(tab.RenderSize));
                        Assert(ReferenceEquals(tab.DataContext, vm.SelectedIntentSet) && bounds.Left >= -1 && bounds.Right <= scroll.ViewportWidth + 1,
                            "CROWDED_SETS actual selected native header is fully exposed after selection or viewport change");
                    }
                    void CaptureTabs(string suffix)
                    {
                        var filename = $"crowded-sets-{width}-{suffix}.png";
                        images.Add(new { File = filename, Sha256 = SaveFullSettingsBitmap(capture, filename), Width = width, Height = 640,
                            Case = suffix, SelectedSet = vm.SelectedIntentSet!.Label,
                            SetNames = vm.IntentSets.Select(x => x.Label).ToArray(), scroll.HorizontalOffset, scroll.ViewportWidth, scroll.ScrollableWidth,
                            Rows = items.Select(x => new { Label = (x.DataContext as IntentSetChoice)?.Label, x.IsSelected, x.IsMouseOver, x.IsKeyboardFocusWithin,
                                x.ActualWidth, x.ActualHeight, Left = x.TranslatePoint(new Point(), scroll).X }).ToArray() });
                    }
                    AssertSelectedVisible();
                    CaptureTabs(many ? "many" : "four");
                    if (scroll.ScrollableWidth > 0)
                    {
                        var selectedBefore = vm.SelectedIntentSet;
                        scroll.ScrollToLeftEnd(); await Idle();
                        Assert(surface.IntentSetScrollRightButton.IsEnabled && !surface.IntentSetScrollLeftButton.IsEnabled,
                            "CROWDED_SETS browse arrows reflect the left boundary");
                        await InvokeSelectionButton(surface.IntentSetScrollRightButton); await Idle();
                        Assert(scroll.HorizontalOffset > 0 && ReferenceEquals(vm.SelectedIntentSet, selectedBefore) && capture.PaletteTab.IsSelected,
                            "CROWDED_SETS right arrow browses headers without selecting or changing the main task");
                        await InvokeSelectionButton(surface.IntentSetScrollLeftButton); await Idle();
                        Assert(scroll.HorizontalOffset < 1 && ReferenceEquals(vm.SelectedIntentSet, selectedBefore),
                            "CROWDED_SETS left arrow returns to the boundary without selecting a Set");
                    }
                    // Select a native header outside the initial viewport, then return
                    // through the real selector. Both paths must reveal the current tab.
                    surface.IntentSetSegments.SelectedIndex = vm.IntentSets.Count - 1; await Idle();
                    AssertSelectedVisible();
                    Assert(capture.PaletteTab.IsSelected && vm.SelectedIntentSet!.Id == vm.IntentSets.Last().Id &&
                        surface.SingleSetTitleText.Text == vm.SelectedIntentSet.Label,
                        "CROWDED_SETS native last-header selection updates the full name and leaves MainTabs alone");
                    if (many) CaptureTabs("many-last-selected");
                    surface.IntentSetSegments.SelectedIndex = 2; await Idle(); AssertSelectedVisible();
                }
                if (many)
                {
                    // Use the real Set-opening coordinator path in both versions.
                    typeof(PlacerViewModel).GetMethod("SelectSettingsForSet", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(vm, [vm.SelectedIntentSet, null]);
                    capture.SelectionTab.IsSelected = true; await Idle();
                    var session = vm.IntentSettings!;
                    var draftBefore = JsonSerializer.Serialize(session.Build());
                    foreach (var width in new[] { 360, 640 })
                    {
                        capture.Width = width; await Idle(); capture.UpdateLayout();
                        var panel = capture.RelativeSettingsSurface;
                        panel.PalettePicker.IsDropDownOpen = true; await Idle();
                        var popup = (Popup)panel.PalettePicker.Template.FindName("PART_Popup", panel.PalettePicker);
                        var filename = $"crowded-settings-{width}-character-dropdown.png";
                        images.Add(new { File = filename, Sha256 = SaveFullSettingsBitmap(capture, filename, (FrameworkElement)popup.Child),
                            Width = width, Height = 640, Case = "settings-character-dropdown",
                            SelectedSet = session.SelectedPalette!.Name,
                            SetNames = session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Name).ToArray(),
                            CharacterContext = (panel.FindName("SettingsReferenceCharacterText") as TextBlock)?.Text });
                        VerifyCrowdedPopupBounds((FrameworkElement)popup.Child, window, width);
                        Assert(session.HasSettingsReferenceCharacter && !session.ShowOtherCharacterSets &&
                            session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(all.Where(x => x.Target.CharacterName != "魔理沙").Select(x => x.Id)),
                            "CROWDED_SETS real Set-opening coordinator shows reference-character plus common Sets in saved order");
                        panel.PalettePicker.IsDropDownOpen = false; await Idle();
                        panel.ShowOtherCharacterSetsCheck.IsChecked = true; await Idle();
                        Assert(session.ShowOtherCharacterSets && session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Id).SequenceEqual(all.Select(x => x.Id)),
                            "CROWDED_SETS actual checkbox reveals all same-type Sets without changing order");
                        panel.PalettePicker.IsDropDownOpen = true; await Idle();
                        popup = (Popup)panel.PalettePicker.Template.FindName("PART_Popup", panel.PalettePicker);
                        filename = $"crowded-settings-{width}-all-characters-dropdown.png";
                        VerifyCrowdedPopupBounds((FrameworkElement)popup.Child, window, width);
                        images.Add(new { File = filename, Sha256 = SaveFullSettingsBitmap(capture, filename, (FrameworkElement)popup.Child),
                            Width = width, Height = 640, Case = "settings-all-characters-dropdown", SelectedSet = session.SelectedPalette!.Name,
                            SetNames = session.VisiblePalettes.Cast<IntentPaletteDraft>().Select(x => x.Name).ToArray(),
                            CharacterContext = panel.SettingsReferenceCharacterText.Text });
                        panel.PalettePicker.IsDropDownOpen = false; panel.ShowOtherCharacterSetsCheck.IsChecked = false; await Idle();
                    }
                    if (session.HasChanges) vm.SaveIntentSettings();
                    Assert(JsonSerializer.Serialize(session.Build()) == draftBefore,
                        "CROWDED_SETS opening dropdowns preserves the complete Settings draft");
                }
                Assert(Signature(timeline) == timelineBefore && JsonSerializer.Serialize(typeof(PlacerViewModel)
                    .GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)) == savedBefore,
                    "CROWDED_SETS navigation/rendering is Timeline and saved-Settings zero-write");
            }
            File.WriteAllText(Path.Combine(output, "crowded-set-capture.json"), JsonSerializer.Serialize(new {
                SourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
                CheckoutCommit = Environment.GetEnvironmentVariable("GITHUB_SHA"), RunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
                RunAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"), CandidateLayoutChecks = CrowdedSetCandidate, SetSelector = "Native one-row TabControl headers with bounded browsing and shared content",
                Capture = "Native WPF proof-window composition, with native ComboBox Popup child composited at its actual screen-relative bounds; not desktop screenshots",
                Fixture = "Same synthetic Voice(霊夢), four short/long/similar Sets, then twelve same-character/common/other-character Sets", Images = images
            }, new JsonSerializerOptions { WriteIndented = true }));
            Log("CROWDED_SET_CAPTURE=PASS");
        }
        finally { window?.Close(); if (capture != null) capture.DataContext = null; View = dock; ViewModel = vm; }
    }
}
