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
    private static readonly bool CrowdedSetCandidate = false;
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
                    Assert(ReferenceEquals(surface.IntentSetSegments.SelectedItem, vm.SelectedIntentSet) &&
                        ReferenceEquals(surface.IntentSetPicker.SelectedItem, vm.SelectedIntentSet),
                        "CROWDED_SETS both visual selectors reference the exact selected model");
                    var cursor = window.PointToScreen(new Point(5, 5));
                    Assert(Round2Input.SetCursorPos((int)cursor.X, (int)cursor.Y), "CROWDED_SETS cursor moves to neutral scene-heading space");
                    await Idle();
                    var items = RelativeVisuals(surface.IntentSetSegments).OfType<ListBoxItem>().ToArray();
                    if (CrowdedSetCandidate && !many)
                    {
                        Assert(items.Length == 4 && items.All(x => !x.IsMouseOver) &&
                            items.All(x => Math.Abs(x.ActualWidth - items[0].ActualWidth) <= 1) &&
                            items.All(x => x.TranslatePoint(new Point(x.ActualWidth, 0), surface).X <= surface.ActualWidth + 1),
                            $"CROWDED_SETS equal-width four-Set cells fit {width}px without a hover selection ambiguity");
                        Assert(surface.SingleSetTitleText.IsVisible && surface.SingleSetTitleText.Text == vm.SelectedIntentSet!.Label,
                            "CROWDED_SETS selected long/similar Set has a full-name title");
                    }
                    var filename = $"crowded-sets-{width}-{(many ? "many" : "four")}.png";
                    images.Add(new { File = filename, Sha256 = SaveFullSettingsBitmap(capture, filename), Width = width, Height = 640,
                        Case = many ? "many" : "four", SelectedSet = vm.SelectedIntentSet!.Label,
                        SetNames = vm.IntentSets.Select(x => x.Label).ToArray(),
                        Rows = items.Select(x => new { Label = (x.DataContext as IntentSetChoice)?.Label, x.IsSelected, x.IsMouseOver, x.IsKeyboardFocusWithin,
                            x.ActualWidth, x.ActualHeight, Left = x.TranslatePoint(new Point(), surface).X }).ToArray() });
                    if (many)
                    {
                        surface.IntentSetPicker.IsDropDownOpen = true; await Idle();
                        var popup = (Popup)surface.IntentSetPicker.Template.FindName("PART_Popup", surface.IntentSetPicker);
                        var child = (FrameworkElement)popup.Child;
                        filename = $"crowded-sets-{width}-many-dropdown.png";
                        images.Add(new { File = filename, Sha256 = SaveFullSettingsBitmap(capture, filename, child), Width = width, Height = 640,
                            Case = "many-dropdown", SelectedSet = vm.SelectedIntentSet.Label,
                            SetNames = vm.IntentSets.Select(x => x.Label).ToArray() });
                        surface.IntentSetPicker.IsDropDownOpen = false; await Idle();
                    }
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
                        panel.PalettePicker.IsDropDownOpen = false; await Idle();
                    }
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
                RunAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"), CandidateLayoutChecks = CrowdedSetCandidate,
                Capture = "Native WPF proof-window composition, with native ComboBox Popup child composited at its actual screen-relative bounds; not desktop screenshots",
                Fixture = "Same synthetic Voice(霊夢), four short/long/similar Sets, then twelve same-character/common/other-character Sets", Images = images
            }, new JsonSerializerOptions { WriteIndented = true }));
            Log("CROWDED_SET_CAPTURE=PASS");
        }
        finally { window?.Close(); if (capture != null) capture.DataContext = null; View = dock; ViewModel = vm; }
    }
}
