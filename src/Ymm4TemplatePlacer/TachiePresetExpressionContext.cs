using System.ComponentModel;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerViewModel
{
    private TachiePresetCapabilityCoordinator? tachiePresetCoordinator;
    private readonly HashSet<INotifyPropertyChanged> presetContextWatchers = new(ReferenceEqualityComparer.Instance);
    internal Func<Character, TachiePresetProbeTarget> PresetTargetResolver { get; set; } = TachiePresetProbeTarget.Resolve;
    private TachiePresetCapabilityCoordinator PresetCoordinator => tachiePresetCoordinator ??= new(c => PresetTargetResolver(c));
    internal TachiePresetCapabilityDiagnostics PresetCapabilityDiagnostics => tachiePresetCoordinator?.Diagnostics ?? new();

    internal void InvalidatePresetCapabilityCache()
    {
        CancelTachiePresetApply();
        tachiePresetCoordinator?.Invalidate();
    }

    private bool PresetSnapshotStillCurrent(ExpressionHostSnapshot snapshot, IReadOnlyList<Character> characters, CancellationToken token)
    {
        foreach (var voice in snapshot.Voices)
        {
            token.ThrowIfCancellationRequested();
            var item = voice.Voice;
            if (item.CharacterName != voice.Character || item.Frame != voice.Frame || item.Length != voice.Length ||
                item.Layer != voice.Layer || (item.Serif ?? "") != voice.Serif) return false;
        }
        var currentCharacters = snapshot.Voices.Select(x => x.Voice.Character).Where(x => x != null).Cast<Character>()
            .ToHashSet((IEqualityComparer<Character>)ReferenceEqualityComparer.Instance);
        if (!currentCharacters.SetEquals(characters)) return false;
        var results = snapshot.PresetCapabilities.ToDictionary(x => x.CharacterIdentity, StringComparer.Ordinal);
        foreach (var character in characters)
        {
            token.ThrowIfCancellationRequested();
            if (results.GetValueOrDefault(character.Name)?.Capability is not { } result) continue;
            try
            {
                var target = PresetTargetResolver(character);
                if (target.Fingerprint != result.Fingerprint || !target.IsCurrent(token)) return false;
            }
            catch (OperationCanceledException) { throw; }
            catch { return false; }
        }
        return true;
    }

    private void ReconcilePresetContextWatchers(IReadOnlyList<Character> characters)
    {
        ClearPresetContextWatchers();
        if (!IsTachiePresetExpressionSource || !voiceFreshnessActive) return;
        foreach (var character in characters)
        {
            Watch(character);
            if (character.TachieCharacterParameter is INotifyPropertyChanged configuration) Watch(configuration);
        }
        void Watch(INotifyPropertyChanged value)
        {
            if (presetContextWatchers.Add(value)) value.PropertyChanged += PresetContextChanged;
        }
    }

    private void PresetContextChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!IsTachiePresetExpressionSource || !voiceFreshnessActive || voiceFreshnessDisposed) return;
        CancelTachiePresetApply();
        expressionCacheDirty = true;
        fullVoiceReconcilePending = true;
        RequestVoiceFreshnessCheck();
    }

    private void ClearPresetContextWatchers()
    {
        foreach (var value in presetContextWatchers) value.PropertyChanged -= PresetContextChanged;
        presetContextWatchers.Clear();
    }

    private void DisposePresetDiscovery()
    {
        ClearPresetContextWatchers();
        tachiePresetCoordinator?.Dispose();
        tachiePresetCoordinator = null;
        expressionPresetCapabilities = [];
    }
}
