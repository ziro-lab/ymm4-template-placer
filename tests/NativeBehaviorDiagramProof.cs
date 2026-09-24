using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyBehaviorPreviewDiagram(Timeline timeline)
    {
        stage = "BEHAVIOR_PREVIEW v2 placement-result diagram";
        var key = IntentSelectionContext.TypeKey(typeof(TextItem));
        var target = new IntentTargetContext { ItemTypeKeys = [key] };
        IntentPaletteDraft Draft(IntentRelation relation, IntentTargetContext? context = null, IntentEntry? entry = null) =>
            new(new(Guid.NewGuid(), "図の確認", "preview", context ?? target, relation, entry == null ? [] : [entry]),
                Array.Empty<LibraryEntry>(), new Dictionary<string, string> { [key] = "テキスト" });
        PreviewBlock Placed(BehaviorPreviewModel model) => model.Blocks.Single(x => x.Kind == PreviewBlockKind.Placed);
        PreviewBlock Target(BehaviorPreviewModel model) => model.Blocks.First(x => x.Kind == PreviewBlockKind.Target);
        PreviewBlock Neighbor(BehaviorPreviewModel model) => model.Blocks.Single(x => x.Kind == PreviewBlockKind.Neighbor);
        var same = Draft(new()).BehaviorDiagram;
        Assert(Placed(same).Start == Target(same).Start && Placed(same).End == Target(same).End && Placed(same).Row < Target(same).Row,
            "BEHAVIOR_PREVIEW v2 equal target duration has coincident edges; Up means visually above");
        var down = Draft(new() { Layer = new() { Direction = RelativeLayerDirection.Down } }).BehaviorDiagram;
        Assert(Placed(down).Row > Target(down).Row, "BEHAVIOR_PREVIEW v2 Down reverses vertical order without changing time");
        var relation = new IntentRelation { Duration = IntentDuration.UntilRelated, Neighbor = IntentNeighbor.NextSameType, Fallback = IntentFallback.DoNotPlace };
        var head = Draft(relation).BehaviorDiagram;
        var tail = Draft(relation with { NeighborEdge = IntentNeighborEdge.End }).BehaviorDiagram;
        Assert(Placed(head).End == Neighbor(head).Start && Placed(tail).End == Neighbor(tail).End && Placed(tail).End > Placed(head).End,
            "BEHAVIOR_PREVIEW v2 next-item head and tail have different exact endpoint alignments");
        var center = Draft(new() { Anchor = IntentAnchor.SelectedCenter, Alignment = IntentAlignment.CenterAtAnchor, Duration = IntentDuration.Fixed, FixedDuration = 48 }).BehaviorDiagram;
        var end = Draft(new() { Anchor = IntentAnchor.SelectedEnd, Alignment = IntentAlignment.EndAtAnchor, Duration = IntentDuration.Fixed, FixedDuration = 48 }).BehaviorDiagram;
        Assert((Placed(center).Start + Placed(center).End) / 2 == (Target(center).Start + Target(center).End) / 2 && Placed(end).End == Target(end).End,
            "BEHAVIOR_PREVIEW v2 center/center and end/end align; alignment is not confused with anchor");
        var before = Draft(new() { Anchor = IntentAnchor.RelatedStart, Neighbor = IntentNeighbor.PreviousSameType, Duration = IntentDuration.Fixed, FixedDuration = 48 }).BehaviorDiagram;
        var reversed = Draft(relation with { Neighbor = IntentNeighbor.PreviousSameType }).BehaviorDiagram;
        Assert(Neighbor(before).Start < Target(before).Start && Placed(before).Start == Neighbor(before).Start &&
            reversed.Blocks.All(x => x.Kind != PreviewBlockKind.Placed) && reversed.Notice.Contains("終了が開始以前", StringComparison.Ordinal),
            "BEHAVIOR_PREVIEW v2 previous neighbor is not blindly mirrored into a fake valid interval");
        var multi = target with { MinimumCount = 2, MaximumCount = 2 };
        var pair = Draft(new() { Anchor = IntentAnchor.PairBoundary, Duration = IntentDuration.Fixed }, multi).BehaviorDiagram;
        var range = Draft(new() { Anchor = IntentAnchor.SelectionRangeStart }, multi).BehaviorDiagram;
        Assert(pair.Blocks.Count(x => x.Kind == PreviewBlockKind.Target) == 2 && Placed(pair).Start == Target(pair).End &&
            pair.Guides.Contains(Placed(pair).Start) && !pair.Notice.Contains("区切り線", StringComparison.Ordinal) &&
            Placed(range).Start == range.Blocks.Where(x => x.Kind == PreviewBlockKind.Target).Min(x => x.Start) &&
            Placed(range).End == range.Blocks.Where(x => x.Kind == PreviewBlockKind.Target).Max(x => x.End) &&
            range.Caption == "対象範囲と同じ長さ",
            "BEHAVIOR_PREVIEW v2 pair boundary keeps its separator guide without duplicate explanatory text");
        var incomplete = Draft(new() { Duration = IntentDuration.Fixed }); incomplete.FixedDuration = "-";
        Assert(!incomplete.BehaviorDiagram.HasDiagram && incomplete.FixedDuration == "-",
            "BEHAVIOR_PREVIEW v2 incomplete active length suppresses made-up geometry and retains Draft text");
        incomplete.Duration = IntentDuration.TargetSpan;
        Assert(incomplete.BehaviorDiagram.HasDiagram, "BEHAVIOR_PREVIEW v2 inactive invalid length does not block a valid target-span diagram");
        var tile = new IntentEntry(Guid.NewGuid()) { UseTemplateDuration = true, FixedDurationOverride = 30, StartOffsetDelta = 6, EndOffsetDelta = 9 };
        var tileDraft = Draft(new(), entry: tile); tileDraft.SelectedEntry = tileDraft.Entries[0];
        var tileModel = tileDraft.BehaviorDiagram;
        Assert(Placed(tileModel).End - Placed(tileModel).Start == BehaviorPreviewModel.SourceLength + 3 && tileModel.Scope == "選択タイルの配置",
            "BEHAVIOR_PREVIEW v2 selected tile intrinsic duration wins over fixed override and applies both edge deltas");
        var notifications = 0;
        tileDraft.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IntentPaletteDraft.Entries)) notifications++; };
        tileDraft.SelectedEntry!.StartOffset = "7";
        Assert(notifications > 0 && Placed(tileDraft.BehaviorDiagram).Start == Placed(tileModel).Start + 1,
            "BEHAVIOR_PREVIEW v2 editing selected tile deltas immediately refreshes the diagram");

        // Differential check against the real pure resolver, using isolated synthetic
        // fixtures only in this proof. Production Preview never receives a Timeline.
        var originalItems = timeline.Items; var originalSelection = timeline.SelectedItems;
        var mismatches = new List<string>(); var checkedCases = 0;
        try
        {
            foreach (var anchor in new[] { IntentAnchor.SelectedStart, IntentAnchor.SelectedCenter, IntentAnchor.SelectedEnd })
            foreach (var alignment in Enum.GetValues<IntentAlignment>())
            foreach (var duration in new[] { IntentDuration.TargetSpan, IntentDuration.Template, IntentDuration.Fixed })
                Compare(new() { Anchor = anchor, Alignment = alignment, Duration = duration, FixedDuration = 67, StartOffset = 3, EndOffset = 8 });
            Compare(relation); Compare(relation with { NeighborEdge = IntentNeighborEdge.End });
            Compare(relation with { Anchor = IntentAnchor.SelectedEnd });
            Compare(relation with { Neighbor = IntentNeighbor.PreviousSameType });
            Compare(new() { Anchor = IntentAnchor.RelatedStart, Neighbor = IntentNeighbor.PreviousSameType, Duration = IntentDuration.Fixed });

            void Compare(IntentRelation rule)
            {
                var selection = new TextItem { Frame = 200, Length = 120, Layer = 20 };
                var previous = rule.Neighbor == IntentNeighbor.PreviousSameType;
                var neighbor = new TextItem { Frame = previous ? 30 : 400, Length = 90, Layer = 7 };
                timeline.Items = [selection, neighbor]; timeline.SelectedItems = [selection];
                var model = Draft(rule).BehaviorDiagram;
                var outputBlock = model.Blocks.SingleOrDefault(x => x.Kind == PreviewBlockKind.Placed);
                try
                {
                    var actual = IntentRelationResolver.Resolve(IntentSelectionContext.Capture(timeline), rule, new(Guid.NewGuid()), 72);
                    if (outputBlock == null || actual.Frame != outputBlock.Start || actual.Frame + actual.Length != outputBlock.End)
                        mismatches.Add($"{rule.Anchor}/{rule.Alignment}/{rule.Duration}");
                }
                catch (InvalidOperationException) { if (outputBlock != null) mismatches.Add("unexpected positive diagram"); }
                checkedCases++;
            }
        }
        finally { timeline.Items = originalItems; timeline.SelectedItems = originalSelection; }
        Assert(mismatches.Count == 0 && checkedCases == 32,
            "BEHAVIOR_PREVIEW v2 32 illustrative mappings agree with the existing resolver, including edge offsets and odd-frame center rounding: " + string.Join(",", mismatches));

        var signature = Signature(timeline);
        var disk = File.Exists(PlacerSettingsStore.DefaultPath) ? File.ReadAllBytes(PlacerSettingsStore.DefaultPath) : null;
        var window = new Window { Width = 420, Height = 360, ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow };
        try
        {
            var cases = new[] { ("same-up", same), ("same-down", down), ("neighbor-head", head), ("neighbor-end", tail),
                ("center", center), ("end", end), ("previous", before), ("pair", pair), ("range", range),
                ("one-frame", Draft(new() { Duration = IntentDuration.Fixed, FixedDuration = 1 }).BehaviorDiagram),
                ("absolute", Draft(new() { Layer = new() { Mode = LayerPlacementMode.Absolute, AbsoluteLayer = 42 } }).BehaviorDiagram) };
            window.Show();
            foreach (var (name, model) in cases)
            foreach (var width in new[] { 260d, 360d })
            {
                var preview = new PlacementBehaviorPreview { DataContext = model, Width = width };
                var border = new Border { Child = preview, Padding = new(10), Background = SystemColors.WindowBrush, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                window.Content = border; await Idle(); window.UpdateLayout();
                var bars = preview.DiagramCanvas.Children.OfType<Border>().ToArray();
                double Pixel(double value) => 4 + (value - model.Minimum) / (model.Maximum - model.Minimum) * (preview.DiagramCanvas.ActualWidth - 8);
                Assert(bars.Length == model.Blocks.Count && bars.All(x => x.Tag is PreviewBlock b &&
                    Math.Abs(Canvas.GetLeft(x) - Pixel(b.Start)) < .1 &&
                    Math.Abs(x.Width - Math.Max(1, Pixel(b.End) - Pixel(b.Start))) < .1 &&
                    Canvas.GetTop(x) == (b.Row == 0 ? 23 : 68) &&
                    Canvas.GetLeft(x) >= 0 && Canvas.GetLeft(x) + x.Width <= preview.DiagramCanvas.ActualWidth + .1) && !preview.IsHitTestVisible,
                    $"BEHAVIOR_PREVIEW v2 {name}/{width}: actual WPF start/end and row positions preserve the model without clipping or interaction");
                if (name == "pair")
                    Assert(!preview.DiagramCanvas.Children.OfType<TextBlock>().Any(x => x.Text.Contains("区切り線", StringComparison.Ordinal)) &&
                        preview.DiagramCanvas.Children.OfType<System.Windows.Shapes.Line>().Any(),
                        $"BEHAVIOR_PREVIEW v2 pair/{width}: separator is conveyed by the guide line without duplicate text");
                var image = new RenderTargetBitmap((int)Math.Ceiling(border.ActualWidth), (int)Math.Ceiling(border.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                image.Render(border); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                using var file = File.Create(Path.Combine(output, $"behavior-preview-v2-{name}-{width}.png")); encoder.Save(file);
            }
        }
        finally { window.Close(); }
        var diskUnchanged = disk == null ? !File.Exists(PlacerSettingsStore.DefaultPath) : File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(disk);
        Assert(Signature(timeline) == signature && diskUnchanged, "BEHAVIOR_PREVIEW v2 rendering does not write Timeline or Settings");
        Log("BEHAVIOR_PREVIEW_V2=PASS");
        Log("BEHAVIOR_PREVIEW_P1=PASS"); // The current P1 contract is v2, not the superseded cards.
    }
}
