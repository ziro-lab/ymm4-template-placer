using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    // Evidence only. A real-sized native window avoids the dock ancestor's layout
    // clip, which is not enlarged by assigning Width/Height to its child view.
    // Product XAML, view models, persistence and placement paths are unchanged.
    private static async Task VerifyFullSettingsCapture(Timeline timeline, UndoRedoManager undo)
    {
        stage = "full Settings visual evidence";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var dockView = View!;
        var source = scope.AddTemplate("設定確認/強調テキスト", new TextItem { Length = 30, Layer = 5 });
        var second = scope.AddTemplate("設定確認/補足テキスト", new TextItem { Length = 45, Layer = 6 });
        var voice = new VoiceItem(new Character { Name = "設定確認" }) { Frame = 100, Length = 60, Layer = 20 };
        var set = new IntentPalette(Guid.NewGuid(), "ボイスに合わせる演出", "演出",
            new() { ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))] }, new(),
            [new(source.Id) { DisplayAlias = "強調", Color = IntentTileColor.Blue }, new(second.Id) { DisplayAlias = "補足" }]);
        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.Library = [source, second]; fixture.IntentPalettes = [set];
        fixture.Palettes = []; fixture.ManualStylePaletteId = null; fixture.ManualCharacterPaletteId = null;
        fixture.ExpressionBootstrapComplete = true;
        scope.Apply(fixture, [voice], [voice]);
        vm.BeginIntentSettings(); dockView.SelectionTab.IsSelected = true; await Idle();
        var session = vm.IntentSettings!;
        session.SelectedItemContext = session.ItemContexts.Single(x => x.Key == IntentSelectionContext.TypeKey(typeof(VoiceItem)));
        session.SelectedPalette = session.Palettes.Single(x => x.Id == set.Id);
        session.SelectedPalette.SelectedEntry = session.SelectedPalette.Entries[0];
        session.SourceSearch = "設定確認/";
        await Idle();
        var beforeTimeline = Signature(timeline);
        var beforeSettings = JsonSerializer.Serialize(session.Build());
        var beforeTemplates = string.Join("|", scope.Templates.Select(x => x.Name + ":" + x.Items.Count));
        var images = new List<object>();
        PlacerView? capture = null;
        Window? window = null;
        try
        {
            capture = new PlacerView { DataContext = vm, Width = 360, Height = 640 };
            window = new Window
            {
                Title = "Template Placer Settings capture proof",
                Content = capture, SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Left = SystemParameters.WorkArea.Left + 16, Top = SystemParameters.WorkArea.Top + 16
            };
            // Select Settings before Loaded so the second view cannot close and
            // replace the shared session through its default Palette tab.
            capture.SelectionTab.IsSelected = true;
            window.Show(); await Idle();
            Assert(ReferenceEquals(session, vm.IntentSettings), "SETTINGS_CAPTURE uses the configured live Settings session");
            var panel = capture.RelativeSettingsSurface;
            var entryDetails = RelativeVisuals(panel.EntryEditor).OfType<Expander>()
                .Single(x => x.Header?.ToString() == "選択した演出だけの微調整");
            foreach (var width in new[] { 360, 640 })
            {
                capture.Width = width;
                foreach (var expanded in new[] { false, true })
                {
                    panel.TargetAdvanced.IsExpanded = expanded;
                    panel.RelationAdvanced.IsExpanded = expanded;
                    entryDetails.IsExpanded = expanded;
                    panel.PresentationSettingsSurface.PresentationExpander.IsExpanded = expanded;
                    var presentationScroll = (ScrollViewer)panel.PresentationSettingsSurface.PresentationExpander.Content;
                    presentationScroll.ScrollToHome();
                    panel.SettingsScroll.ScrollToHome();
                    await Idle(); window.UpdateLayout(); capture.UpdateLayout();
                    Assert(capture.IsLoaded && capture.IsVisible && Math.Abs(capture.ActualWidth - width) < 1 && Math.Abs(capture.ActualHeight - 640) < 1 &&
                        Math.Abs(VisualTreeHelper.GetOffset(capture).X) < 1 && Math.Abs(VisualTreeHelper.GetOffset(capture).Y) < 1,
                        $"SETTINGS_CAPTURE exact unclipped {width}x640 native root");
                    var scroll = panel.SettingsScroll;
                    Assert(scroll.ViewportHeight > 200 && scroll.ScrollableHeight > 0 &&
                        scroll.ExtentWidth <= scroll.ViewportWidth + 1,
                        $"SETTINGS_CAPTURE usable vertical-only viewport at {width}px");
                    var mode = expanded ? "expanded" : "default";
                    var sectionPositions = new FrameworkElement[] { panel.SetManagement, panel.TargetEditor, panel.RelationEditor,
                        panel.EntryEditor, panel.EntryAppearanceEditor, panel.SourceEditor, panel.PresentationSettingsSurface, panel.SetShapeButtons }
                        .Select(x => new { x.Name, Top = x.TranslatePoint(new Point(), panel.SettingsScrollContent).Y, Height = x.ActualHeight }).ToArray();
                    var page = 0;
                    var offset = 0d;
                    var coveredUntil = 0d;
                    while (true)
                    {
                        scroll.ScrollToVerticalOffset(offset); await Idle(); capture.UpdateLayout();
                        Assert(scroll.VerticalOffset <= coveredUntil + 1,
                            "SETTINGS_CAPTURE scroll pages have no content gaps");
                        var filename = $"settings-full-{width}-{mode}-{++page:00}.png";
                        var hash = SaveFullSettingsBitmap(capture, filename);
                        images.Add(new { File = filename, Sha256 = hash, Width = width, Height = 640, Mode = mode,
                            ScrollOffset = scroll.VerticalOffset, scroll.ViewportHeight, scroll.ExtentHeight,
                            Sections = sectionPositions });
                        coveredUntil = scroll.VerticalOffset + scroll.ViewportHeight;
                        if (scroll.VerticalOffset >= scroll.ScrollableHeight - 1) break;
                        var next = Math.Min(scroll.ScrollableHeight, scroll.VerticalOffset + scroll.ViewportHeight - 64);
                        Assert(next > scroll.VerticalOffset, "SETTINGS_CAPTURE scrolling makes forward progress");
                        offset = next;
                    }
                    Assert(coveredUntil >= scroll.ExtentHeight - 1, "SETTINGS_CAPTURE complete top-to-bottom Settings coverage");
                    if (expanded && presentationScroll.ScrollableHeight > 0)
                    {
                        presentationScroll.ScrollToEnd(); await Idle(); capture.UpdateLayout();
                        var filename = $"settings-full-{width}-presentation-inner-bottom.png";
                        images.Add(new { File = filename, Sha256 = SaveFullSettingsBitmap(capture, filename),
                            Width = width, Height = 640, Mode = "expanded-inner-bottom",
                            ScrollOffset = scroll.VerticalOffset, InnerOffset = presentationScroll.VerticalOffset,
                            InnerViewportHeight = presentationScroll.ViewportHeight, InnerExtentHeight = presentationScroll.ExtentHeight });
                    }
                }
            }
            Assert(ReferenceEquals(session, vm.IntentSettings) && Signature(timeline) == beforeTimeline &&
                JsonSerializer.Serialize(vm.IntentSettings!.Build()) == beforeSettings &&
                string.Join("|", scope.Templates.Select(x => x.Name + ":" + x.Items.Count)) == beforeTemplates,
                "SETTINGS_CAPTURE navigation/rendering preserves fixture Timeline, Settings Draft and Template identities");
            File.WriteAllText(Path.Combine(output, "settings-full-capture.json"), JsonSerializer.Serialize(new
            {
                SourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
                CheckoutCommit = Environment.GetEnvironmentVariable("GITHUB_SHA"),
                CheckoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
                RunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
                RunAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"),
                HostVersion = typeof(Timeline).Assembly.GetName().Version?.ToString(),
                Capture = "Unmodified product PlacerView in a proof-only native WPF window; 96-DPI render, not a desktop screenshot",
                Fixture = "Synthetic Voice-owned Set with two Text Template tiles; default and expanded disclosures",
                Images = images
            }, new JsonSerializerOptions { WriteIndented = true }));
            Log("SETTINGS_FULL_CAPTURE=PASS");
        }
        finally
        {
            window?.Close();
            if (capture != null) capture.DataContext = null;
            View = dockView; ViewModel = vm;
        }
    }

    private static string SaveFullSettingsBitmap(PlacerView view, string filename)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Round(view.ActualWidth), (int)Math.Round(view.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        // Capture the actual native host composition, including its background.
        // Rendering only a UserControl can omit the host-painted background even
        // when the control's logical size is correct. This borderless window has
        // exactly the same client dimensions as the unmodified product view.
        var host = Window.GetWindow(view)!;
        Assert(Math.Abs(host.ActualWidth - view.ActualWidth) < 1 && Math.Abs(host.ActualHeight - view.ActualHeight) < 1,
            "SETTINGS_CAPTURE native host and product viewport dimensions match");
        var bounds = new Rect(0, 0, host.ActualWidth, host.ActualHeight);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
            drawing.DrawRectangle(new VisualBrush(host)
            {
                ViewboxUnits = BrushMappingMode.Absolute, Viewbox = bounds,
                Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top
            }, null, bounds);
        bitmap.Render(visual);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        var opaque = 0; var transparent = 0;
        for (var i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] == 255) opaque++;
            else if (pixels[i] == 0) transparent++;
        }
        var path = Path.Combine(output, filename);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(path)) encoder.Save(file);
        // Save even rejected output, so a failed capture is diagnosable.
        var pixelCount = bitmap.PixelWidth * bitmap.PixelHeight;
        Log($"SETTINGS_CAPTURE pixels {filename}: opaque={opaque}/{pixelCount}; transparent={transparent}; background={view.Background}; hostBackground={host.Background}; opacity={view.Opacity}; dpi={VisualTreeHelper.GetDpi(view)}; visualClip={VisualTreeHelper.GetClip(view)?.Bounds}; descendants={VisualTreeHelper.GetDescendantBounds(view)}; layoutClip={System.Windows.Controls.Primitives.LayoutInformation.GetLayoutClip(view)}");
        Assert(opaque == pixelCount,
            "SETTINGS_CAPTURE requested image has no transparent padded region: " + filename);
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }
}
