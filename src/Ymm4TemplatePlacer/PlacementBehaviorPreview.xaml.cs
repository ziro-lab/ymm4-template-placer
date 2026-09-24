using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Ymm4TemplatePlacer;

/// <summary>A few Timeline-like blocks. Pixel layout only; never interprets a placement rule.</summary>
public partial class PlacementBehaviorPreview : UserControl
{
    public BehaviorPreviewModel? Diagram { get; private set; }
    private IntentPaletteDraft? observed;
    private BehaviorPreviewModel? drawn;
    private double drawnWidth = -1;
    private DispatcherOperation? refreshOperation;
    private long refreshEpoch;
    public PlacementBehaviorPreview()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindDraft();
        Loaded += (_, _) => BindDraft();
        Unloaded += (_, _) => UnbindDraft();
        DiagramCanvas.SizeChanged += (_, _) => ScheduleRefresh(false);
    }

    private void UnbindDraft()
    {
        if (observed != null) observed.PropertyChanged -= DraftChanged;
        observed = null;
        refreshEpoch++;
        refreshOperation?.Abort();
        refreshOperation = null;
    }
    private void BindDraft()
    {
        UnbindDraft();
        if (IsLoaded && DataContext is IntentPaletteDraft draft)
        {
            observed = draft;
            observed.PropertyChanged += DraftChanged;
        }
        ScheduleRefresh(true);
    }
    private void DraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IntentPaletteDraft.BehaviorDescription) or nameof(IntentPaletteDraft.SelectedEntry) or nameof(IntentPaletteDraft.Entries) or null or "")
            ScheduleRefresh(true);
    }
    private void ScheduleRefresh(bool rereadDraft)
    {
        if (!IsLoaded && DataContext is not BehaviorPreviewModel) return;
        var epoch = ++refreshEpoch;
        // Never rebuild Canvas children synchronously inside ComboBox/binding
        // PropertyChanged callbacks. Coalesce the edit burst and redraw once
        // WPF has settled SelectedValue, ItemsSource and visibility bindings.
        if (refreshOperation is { Status: DispatcherOperationStatus.Pending }) return;
        refreshOperation = Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            refreshOperation = null;
            if (epoch != refreshEpoch)
            {
                ScheduleRefresh(rereadDraft);
                return;
            }
            if (rereadDraft)
            {
                Diagram = DataContext switch
                {
                    IntentPaletteDraft draft => draft.BehaviorDiagram,
                    BehaviorPreviewModel model => model,
                    _ => null
                };
            }
            RefreshDiagram();
        }));
    }
    private void RefreshDiagram()
    {
        var model = Diagram;
        var width = DiagramCanvas.ActualWidth;
        if (ReferenceEquals(drawn, model) && Math.Abs(width - drawnWidth) < .1) return;
        drawn = model; drawnWidth = width;
        DiagramCanvas.Children.Clear();
        CaptionText.Text = model?.Caption ?? "";
        SetHint(ScopeText, model?.Scope == "選択タイルの配置" ? model.Scope : "");
        SetHint(LayerText, model?.LayerHint);
        SetHint(FallbackText, model?.FallbackHint);
        SetHint(NoticeText, model?.Notice);
        AutomationProperties.SetName(this, "配置イメージ");
        AutomationProperties.SetHelpText(this, model?.AccessibleText ?? "");
        DiagramCanvas.Visibility = model?.HasDiagram == true ? Visibility.Visible : Visibility.Collapsed;
        if (model?.HasDiagram != true || width < 20) return;

        // Shared time transform: never stretch individual blocks to fit their label.
        double X(double coordinate) => 4 + (coordinate - model.Minimum) / (model.Maximum - model.Minimum) * (width - 8);
        var time = Label("時間 →", 11); time.Opacity = .65;
        time.Width = 56; Add(time, Math.Max(0, width - 56), 0);
        if (model.AbsoluteLayer)
        {
            var reference = Label("参照（レイヤー位置は省略）", 10); reference.Opacity = .7;
            Add(reference, 0, 4);
            Line(0, 56, width, 56, false);
        }
        foreach (var guide in model.Guides)
            Line(X(guide), 23, X(guide), 99, true);
        // A small anchor marker connects start/center/end alignment to the reference.
        if (model.Guides.Count > 0)
        {
            var x = X(model.Guides[0]);
            var marker = new Polygon { Points = new PointCollection([new(x - 4, 18), new(x + 4, 18), new(x, 23)]) };
            marker.SetResourceReference(Shape.FillProperty, SystemColors.ControlTextBrushKey);
            DiagramCanvas.Children.Add(marker);
        }
        foreach (var block in model.Blocks)
        {
            var x = X(block.Start); var w = Math.Max(1, X(block.End) - x);
            var y = block.Row == 0 ? 23d : 68d;
            var placed = block.Kind == PreviewBlockKind.Placed;
            var shell = new Border
            {
                Width = w, Height = 26, CornerRadius = new(2),
                BorderThickness = new(placed ? 2 : 1),
                SnapsToDevicePixels = true,
                Tag = block
            };
            shell.SetResourceReference(Border.BorderBrushProperty, placed ? SystemColors.HighlightBrushKey : SystemColors.ControlDarkBrushKey);
            shell.SetResourceReference(Border.BackgroundProperty, SystemColors.WindowBrushKey);
            var content = new Grid();
            if (placed)
            {
                var tint = new Border { Opacity = .16 };
                tint.SetResourceReference(Border.BackgroundProperty, SystemColors.HighlightBrushKey);
                content.Children.Add(tint);
            }
            var label = block.Label;
            if (w < (block.Kind == PreviewBlockKind.Neighbor ? 104 : 80)) label = block.Kind switch
            {
                PreviewBlockKind.Placed => "配置",
                PreviewBlockKind.Neighbor => block.Label.StartsWith("前", StringComparison.Ordinal) ? "前の周辺" : "次の周辺",
                _ => block.Label == "対象アイテム" ? "対象" : block.Label
            };
            if (w >= 29)
            {
                var text = Label(label, 12);
                text.FontWeight = placed ? FontWeights.SemiBold : FontWeights.Normal;
                text.TextAlignment = TextAlignment.Center;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                content.Children.Add(text);
            }
            shell.Child = content;
            AutomationProperties.SetName(shell, block.Label);
            Add(shell, x, y);
            if (w < 29)
            {
                // Preserve a 1f bar and attach its label outside instead of making it
                // look as long as the target. Small reference marks stay distinct.
                var text = Label(label, 11);
                text.Width = Math.Min(64, width); text.TextTrimming = TextTrimming.CharacterEllipsis;
                Add(text, Math.Clamp(x, 0, Math.Max(0, width - text.Width)), y + 27);
            }
        }
    }
    private static TextBlock Label(string text, double size)
    {
        var label = new TextBlock { Text = text, FontSize = size };
        label.SetResourceReference(TextBlock.ForegroundProperty, SystemColors.WindowTextBrushKey);
        return label;
    }
    private static void SetHint(TextBlock label, string? value)
    {
        label.Text = value ?? "";
        label.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    }
    private void Add(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x); Canvas.SetTop(element, y);
        DiagramCanvas.Children.Add(element);
    }
    private void Line(double x1, double y1, double x2, double y2, bool dashed)
    {
        var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, StrokeThickness = 1, Opacity = .6 };
        if (dashed) line.StrokeDashArray = new DoubleCollection([3, 3]);
        line.SetResourceReference(Shape.StrokeProperty, SystemColors.ControlDarkBrushKey);
        DiagramCanvas.Children.Add(line);
    }
}
