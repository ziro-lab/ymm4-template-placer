using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ymm4TemplatePlacer;

public sealed record TemplateChoice(FaceTemplate? Template, string Label, string? ShortName = null, bool IsAvailable = true)
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
    public bool HasCandidates => Choices.Any(x => x.Template != null && x.IsAvailable);
    public bool UsesIntentSources { get; private set; }
    public string State => !SelectedChoice.IsAvailable ? "選択元を確認" : !HasCandidates ? "候補なし" : SelectedChoice.Template == null ? "未選択" : "選択済み";
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
    public AssignmentRow(int no, VoiceSnapshot target, IReadOnlyList<FaceTemplate> catalog, bool? intentSources = null)
    {
        No = no; Target = target; UsesIntentSources = intentSources ?? catalog.Any(x => x.IntentSource != null);
        Choices = IntentExpressionCatalog.Choices(target, catalog); selectedChoice = Choices[0];
    }
    internal void RestoreSelectedChoice(TemplateChoice? choice, string? unavailableLabel = null)
    {
        var next = choice;
        if (next == null && unavailableLabel != null)
        {
            next = Choices.FirstOrDefault(x => x.Template == null && !x.IsAvailable && x.Label == unavailableLabel);
            if (next == null)
            {
                next = new TemplateChoice(null, unavailableLabel, null, false);
                Choices = Choices.Concat([next]).ToArray();
                Changed(nameof(Choices)); Changed(nameof(HasCandidates));
            }
        }
        next ??= Choices.First();
        if (ReferenceEquals(next, selectedChoice)) return;
        selectedChoice = next; Changed(nameof(SelectedChoice)); Changed(nameof(State));
    }
    public void SetCandidateMode(bool relative) => UsesIntentSources = relative;
    public void RefreshCandidates(IReadOnlyList<FaceTemplate> catalog, PlacerSettings settings, bool? relative = null)
    {
        if (relative.HasValue) UsesIntentSources = relative.Value;
        if (UsesIntentSources)
        {
            if (catalog.Any(x => x.IntentSource == null)) catalog = IntentExpressionCatalog.Read(settings);
            var previous = selectedChoice;
            var next = IntentExpressionCatalog.Choices(Target, catalog).ToList();
            var valid = true;
            try { previous.Template?.IntentSource?.Bundle.ValidateCurrent(); } catch (InvalidOperationException) { valid = false; }
            var replacement = valid ? next.FirstOrDefault(x => SameSnapshot(x.Template, previous.Template)) : null;
            if (replacement == null && previous.Template != null)
            {
                replacement = previous with { Label = "⚠ " + previous.DisplayName + "（選択元を確認）", ShortName = null, IsAvailable = false };
                next.Add(replacement); // Retain an explicit unavailable selection; never silently discard or heal an assignment.
            }
            Choices = next; selectedChoice = replacement ?? next[0];
            Changed(nameof(Choices)); Changed(nameof(SelectedChoice)); Changed(nameof(State)); Changed(nameof(HasCandidates)); return;
        }
        var selected = SelectedChoice.Template;
        var candidates = TemplateCatalog.ForVoice(Target.Voice, catalog);
        if (selected != null && !candidates.Any(x => SameSnapshot(x, selected))) return;
        Choices = new[] { new TemplateChoice(null, candidates.Count == 0 ? "— 候補なし —" : "— 配置しない —") }
            .Concat(candidates.Select(x => new TemplateChoice(SameSnapshot(x, selected) ? selected : x, x.Name))).ToArray();
        selectedChoice = Choices.First(x => ReferenceEquals(x.Template, selected));
        PreferPalette(settings); Changed(nameof(HasCandidates));
    }
    private static bool SameSnapshot(FaceTemplate? value, FaceTemplate? selected) => selected == null ? value == null : value != null &&
        ReferenceEquals(value.Template, selected.Template) && ReferenceEquals(value.Face, selected.Face) && value.Name == selected.Name && value.Character == selected.Character;
    public void PreferPalette(PlacerSettings settings)
    {
        if (UsesIntentSources) return;
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
            var shortName = duplicateDisplayNames.Contains(pair.Entry.DisplayName) ? $"{pair.Entry.DisplayName} — {pair.Entry.Source.Name}" : pair.Entry.DisplayName;
            preferred.TryAdd(pair.Template, (preferred.Count, shortName + "（パレット）", shortName));
        }
        var candidates = Choices.Skip(1).Select(x => x.Template!).ToArray();
        Choices = new[] { Choices[0] }.Concat(candidates.OrderBy(x => preferred.TryGetValue(x, out var item) ? item.Order : int.MaxValue)
            .ThenBy(x => x.Name, StringComparer.Ordinal).Select(x => new TemplateChoice(x, preferred.TryGetValue(x, out var item) ? item.Label : x.Name,
                preferred.TryGetValue(x, out item) ? item.ShortName : null))).ToArray();
        selectedChoice = Choices.First(x => ReferenceEquals(x.Template, selected));
        Changed(nameof(Choices)); Changed(nameof(SelectedChoice)); Changed(nameof(State));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
