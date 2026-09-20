using System.Collections.ObjectModel;
using System.Globalization;
using YukkuriMovieMaker.Commons;

namespace Ymm4TemplatePlacer;
public sealed class PositionShortcutDraft : IntentEditable
{
    private string position, gesture;
    public string Position { get => position; set { if (position == value) return; position = value; Notify(); } }
    public string Gesture { get => gesture; set { if (gesture == value) return; gesture = value; Notify(); } }
    public PositionShortcutDraft(int slot, string gesture) { position = (slot + 1).ToString(CultureInfo.InvariantCulture); this.gesture = gesture; }
    public PositionShortcut Build() => PositionShortcut.Parse(checked(Number(Position, "位置") - 1), Gesture);
}
public sealed class PalettePresentationDraft : IntentEditable
{
    private PaletteLayoutMode layout;
    private string columns;
    private readonly int rowHeight;
    private bool enabled;
    private ExpressionViewportFollow follow;
    public PaletteLayoutMode LayoutMode { get => layout; set { if (layout == value) return; layout = value; Notify(); Raise(nameof(IsFixed)); } }
    public string FixedColumns { get => columns; set { if (columns == value) return; columns = value; Notify(); } }
    public bool ShortcutsEnabled { get => enabled; set { if (enabled == value) return; enabled = value; Notify(); } }
    public bool IsFixed => LayoutMode == PaletteLayoutMode.Fixed;
    public ExpressionViewportFollow ViewportFollow { get => follow; set { if (follow == value) return; follow = value; Notify(); } }
    public ObservableCollection<PositionShortcutDraft> Shortcuts { get; } = [];
    public ActionCommand AddShortcutCommand { get; }
    public ActionCommand RemoveShortcutCommand { get; }
    public IReadOnlyList<IntentOption<PaletteLayoutMode>> LayoutModes { get; } = [new(PaletteLayoutMode.Auto,"自動（Auto）"), new(PaletteLayoutMode.Fixed,"列数を固定")];
    public IReadOnlyList<IntentOption<ExpressionViewportFollow>> ViewportModes { get; } = [new(ExpressionViewportFollow.Off,"追従しない"), new(ExpressionViewportFollow.WhenOutside,"画面外の時だけ追従"), new(ExpressionViewportFollow.Always,"常に追従")];
    public PalettePresentationDraft(PalettePresentationSettings source)
    {
        rowHeight = source.ExpressionRowHeight; layout = source.LayoutMode; columns = source.FixedColumns.ToString(CultureInfo.InvariantCulture); enabled = source.ShortcutsEnabled; follow = source.ViewportFollow;
        foreach (var binding in source.PositionShortcuts) Add(new(binding.SlotIndex, binding.Gesture));
        AddShortcutCommand = new(_ => Shortcuts.Count < 64, _ =>
        {
            var used = Shortcuts.Select(x => x.Position).ToHashSet(StringComparer.Ordinal);
            var slot = Enumerable.Range(0, 256).First(x => !used.Contains((x + 1).ToString(CultureInfo.InvariantCulture)));
            Add(new(slot, "")); Notify(); AddShortcutCommand!.RaiseCanExecuteChanged();
        });
        RemoveShortcutCommand = new(x => x is PositionShortcutDraft row && Shortcuts.Contains(row), x =>
        {
            var row = (PositionShortcutDraft)x!; row.Edited -= ChildEdited; Shortcuts.Remove(row); Notify(); AddShortcutCommand.RaiseCanExecuteChanged();
        });
    }
    private void Add(PositionShortcutDraft row) { row.Edited += ChildEdited; Shortcuts.Add(row); }
    private void ChildEdited(object? sender, EventArgs e) => Notify(nameof(Shortcuts));
    public PalettePresentationSettings Build()
    {
        var result = new PalettePresentationSettings { LayoutMode = LayoutMode, FixedColumns = Number(FixedColumns,"固定列数"),
            ShortcutsEnabled = ShortcutsEnabled, PositionShortcuts = Shortcuts.Select(x => x.Build()).ToList(), ViewportFollow = ViewportFollow, ExpressionRowHeight = rowHeight };
        result.Validate(); return result;
    }
}
