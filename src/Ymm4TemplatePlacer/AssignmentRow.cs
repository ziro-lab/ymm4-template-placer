using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ymm4TemplatePlacer;

public sealed record TemplateChoice(FaceTemplate? Template, string Label);

public sealed class AssignmentRow : INotifyPropertyChanged
{
    public int No { get; }
    public VoiceSnapshot Target { get; }
    public string Character => Target.Character;
    public string Serif => Target.Serif;
    public int Frame => Target.Frame;
    public int Length => Target.Length;
    public IReadOnlyList<TemplateChoice> Choices { get; }
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
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
