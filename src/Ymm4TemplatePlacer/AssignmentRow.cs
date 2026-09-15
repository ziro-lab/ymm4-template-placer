using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ymm4TemplatePlacer;

public sealed record TemplateChoice(FaceTemplate? Template, string Label, string? ShortName = null)
{
    public string DisplayName => ShortName ?? Label;
}

public sealed class AssignmentRow : INotifyPropertyChanged
{
    public int No { get; }
    public VoiceSnapshot Target { get; }
    public string Character => Target.Character;
    public string Serif => Target.Serif;
    public int Frame => Target.Frame;
    public int Length => Target.Length;
    public IReadOnlyList<TemplateChoice> Choices { get; private set; }
    public bool HasCandidates => Choices.Count > 1;
    public string State => !HasCandidates ? "候補なし" : SelectedChoice.Template == null ? "未選択" : "選択済み";
    private TemplateChoice selectedChoice;
    public TemplateChoice SelectedChoice
    {
        get => selectedChoice;
        set
        {
            if (value == null || !Choices.Contains(value) || ReferenceEquals(value, selectedChoice)) return;
            selectedChoice = value; Changed(); Changed(nameof(State));
        }
    }
    public AssignmentRow(int no, VoiceSnapshot target, IReadOnlyList<FaceTemplate> catalog)
    {
        No = no; Target = target;
        var candidates = TemplateCatalog.ForVoice(target.Voice, catalog);
        Choices = new[] { new TemplateChoice(null, candidates.Count == 0 ? "— 候補なし —" : "— 配置しない —") }
            .Concat(candidates.Select(x => new TemplateChoice(x, x.Name))).ToArray();
        selectedChoice = Choices[0];
    }
    public void RefreshCandidates(IReadOnlyList<FaceTemplate> catalog, PlacerSettings settings)
    {
        var selected = SelectedChoice.Template;
        var candidates = TemplateCatalog.ForVoice(Target.Voice, catalog);
        bool SameSnapshot(FaceTemplate value) => selected != null && ReferenceEquals(value.Template, selected.Template) &&
            ReferenceEquals(value.Face, selected.Face) && value.Name == selected.Name && value.Character == selected.Character;
        // Adding a source is not permission to heal a changed/missing assignment. Keep its original snapshot for the existing guard.
        if (selected != null && !candidates.Any(SameSnapshot)) return;
        Choices = new[] { new TemplateChoice(null, candidates.Count == 0 ? "— 候補なし —" : "— 配置しない —") }
            .Concat(candidates.Select(x => new TemplateChoice(SameSnapshot(x) ? selected : x, x.Name))).ToArray();
        selectedChoice = Choices.First(x => ReferenceEquals(x.Template, selected));
        PreferPalette(settings);
        Changed(nameof(HasCandidates));
    }
    public void PreferPalette(PlacerSettings settings)
    {
        var selected = SelectedChoice.Template;
        var palette = settings.Palettes.SingleOrDefault(x => x.Kind == PaletteKind.Character && x.CharacterName == Character);
        var resolved = new List<(LibraryEntry Entry, FaceTemplate Template)>();
        foreach (var id in palette?.LibraryEntryIds ?? [])
        {
            var entry = settings.Library.SingleOrDefault(x => x.Id == id);
            if (entry == null || (entry.CharacterName != null && entry.CharacterName != Character)) continue;
            var resolution = TemplateResolver.Resolve(entry);
            if (resolution.State != TemplateReferenceState.Resolved) continue;
            var choice = Choices.FirstOrDefault(x => x.Template != null && ReferenceEquals(x.Template.Template, resolution.Template));
            if (choice?.Template is FaceTemplate template && !resolved.Any(x => ReferenceEquals(x.Template, template))) resolved.Add((entry, template));
        }
        var duplicateDisplayNames = resolved.GroupBy(x => x.Entry.DisplayName, StringComparer.Ordinal).Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var preferred = new Dictionary<FaceTemplate, (int Order, string Label, string ShortName)>();
        foreach (var pair in resolved)
        {
            var shortName = duplicateDisplayNames.Contains(pair.Entry.DisplayName)
                ? $"{pair.Entry.DisplayName} — {pair.Entry.Source.Name}"
                : pair.Entry.DisplayName;
            preferred.TryAdd(pair.Template, (preferred.Count, shortName + "（パレット）", shortName));
        }
        var candidates = Choices.Skip(1).Select(x => x.Template!).ToArray();
        Choices = new[] { Choices[0] }.Concat(candidates
            .OrderBy(x => preferred.TryGetValue(x, out var item) ? item.Order : int.MaxValue)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => new TemplateChoice(x, preferred.TryGetValue(x, out var item) ? item.Label : x.Name,
                preferred.TryGetValue(x, out item) ? item.ShortName : null))).ToArray();
        selectedChoice = Choices.First(x => ReferenceEquals(x.Template, selected));
        Changed(nameof(Choices)); Changed(nameof(SelectedChoice)); Changed(nameof(State));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
