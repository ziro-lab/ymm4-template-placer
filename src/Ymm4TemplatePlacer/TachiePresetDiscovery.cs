using System.Collections;
using System.Reflection;
using System.Windows.Threading;

namespace Ymm4TemplatePlacer;

internal static class TachiePresetDiscovery
{
    private sealed record AppliedState(string? Before, string? After);

    public static async Task<TachiePresetCapabilityResult> DiscoverAsync(TachiePresetProbeTarget target,
        TachiePresetCapabilityDiagnostics diagnostics, Action ensureCurrent, CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        ensureCurrent();
        var sample = target.CreateFreshFace(token);
        var result = new List<TachiePresetCandidateDescriptor>();
        var direct = sample.GetType().GetProperty("Preset", BindingFlags.Instance | BindingFlags.Public);
        if (direct?.PropertyType == typeof(string) && direct.GetMethod?.IsPublic == true &&
            direct.SetMethod?.IsPublic == true && direct.GetIndexParameters().Length == 0)
        {
            var names = DirectNames(target.Configuration, token);
            var route = new TachiePresetRouteDescriptor(TachiePresetRouteKind.DirectNamedProperty,
                direct.DeclaringType?.FullName + "." + direct.Name);
            foreach (var name in names)
            {
                ensureCurrent();
                await Dispatcher.Yield(DispatcherPriority.Background);
                ensureCurrent();
                var (first, second) = FreshPair(target, token);
                AppliedState Apply(object face)
                {
                    var before = TachiePresetPublicState.TryHash(face, token);
                    direct.SetValue(face, name);
                    token.ThrowIfCancellationRequested();
                    if (!Equals(direct.GetValue(face), name))
                        throw new InvalidOperationException("プリセット名の適用を確認できませんでした。");
                    return new(before, TachiePresetPublicState.TryHash(face, token));
                }
                Add(result, target, route, name, Confidence(Apply(first), Apply(second)));
            }
        }

        foreach (var route in TachiePresetEditorSession.FindRoutes(sample))
        {
            ensureCurrent();
            string[] names;
            using (var editor = TachiePresetEditorSession.Open(route, target.Configuration, target.CreateFreshFace(token), diagnostics))
            {
                await editor.SettleAsync(ensureCurrent, token);
                names = editor.Choices(token).Select(c => c.Label).ToArray();
            }
            foreach (var name in names)
            {
                ensureCurrent();
                var (first, second) = FreshPair(target, token);
                var firstState = await ApplyEditorAsync(route, target.Configuration, first, name, diagnostics, ensureCurrent, token);
                var secondState = await ApplyEditorAsync(route, target.Configuration, second, name, diagnostics, ensureCurrent, token);
                Add(result, target, route.Descriptor, name, Confidence(firstState, secondState));
            }
        }
        ensureCurrent();
        if (!target.IsCurrent(token)) throw new OperationCanceledException("立ち絵設定が変わりました。", token);
        return TachiePresetCapabilityResult.Supported(target.Fingerprint, result);
    }

    private static (object First, object Second) FreshPair(TachiePresetProbeTarget target, CancellationToken token)
    {
        var first = target.CreateFreshFace(token);
        var second = target.CreateFreshFace(token);
        if (ReferenceEquals(first, second))
            throw new InvalidOperationException("独立した表情パラメータを作成できませんでした。");
        return (first, second);
    }

    private static void Add(List<TachiePresetCandidateDescriptor> result, TachiePresetProbeTarget target,
        TachiePresetRouteDescriptor route, string name, TachiePresetCapabilityLevel level)
    {
        if (result.Count >= TachiePresetCapabilityCoordinator.MaxCandidates)
            throw new InvalidOperationException("立ち絵プリセットの候補数が128件を超えています。");
        result.Add(new(target.Fingerprint, route, name, name, level));
    }

    private static TachiePresetCapabilityLevel Confidence(AppliedState first, AppliedState second) =>
        first.Before != null && first.After != null && second.Before != null && second.After != null &&
        first.Before != first.After && second.Before != second.After && first.After == second.After
            ? TachiePresetCapabilityLevel.Strong : TachiePresetCapabilityLevel.Experimental;

    private static async Task<AppliedState> ApplyEditorAsync(TachiePresetEditorRoute route, object configuration,
        object face, string name, TachiePresetCapabilityDiagnostics diagnostics, Action ensureCurrent, CancellationToken token)
    {
        AppliedState applied;
        using (var editor = TachiePresetEditorSession.Open(route, configuration, face, diagnostics))
        {
            await editor.SettleAsync(ensureCurrent, token);
            var choices = editor.Choices(token);
            var selected = choices.SingleOrDefault(c => c.Label == name)
                ?? throw new InvalidOperationException("確認中にプリセット候補が消えました。");
            // A preset equal to the default is still testable: first select a different
            // coherent named candidate on this fresh object, then apply the requested one.
            var alternate = choices.FirstOrDefault(c => c.Label != name);
            if (alternate != null)
            {
                alternate.Selector.SelectedItem = alternate.Item;
                await editor.SettleAsync(ensureCurrent, token);
                selected = editor.Choices(token).SingleOrDefault(c => c.Label == name)
                    ?? throw new InvalidOperationException("切り替え後のプリセット候補を一意に解決できません。");
            }
            var before = TachiePresetPublicState.TryHash(face, token);
            selected.Selector.SelectedItem = selected.Item;
            await editor.SettleAsync(ensureCurrent, token);
            if (!ReferenceEquals(selected.Selector.SelectedItem, selected.Item))
                throw new InvalidOperationException("プリセットの選択が反映されませんでした。");
            applied = new(before, TachiePresetPublicState.TryHash(face, token));
        }
        ensureCurrent();
        // ClearBindings/DataContext teardown may itself change a plugin's face state.
        // Never promote a result measured only while the editor was alive to Strong.
        var retained = TachiePresetPublicState.TryHash(face, token);
        return applied with { After = applied.After != null && applied.After == retained ? retained : null };
    }

    internal static IReadOnlyList<string> DirectNames(object configuration, CancellationToken token)
    {
        var sources = new List<IReadOnlyList<string>>();
        foreach (var property in configuration.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            token.ThrowIfCancellationRequested();
            if (property.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0 ||
                property.Name is not ("Preset" or "Presets" or "PresetDefinitions" or "PresetNames")) continue;
            var value = property.GetValue(configuration);
            var names = new List<string>();
            if (value is string definitions)
            {
                if (definitions.Length > TachiePresetPublicState.MaxCharacters)
                    throw new InvalidOperationException("プリセット定義が確認上限を超えています。");
                string? header = null;
                var hasBody = false;
                using var reader = new System.IO.StringReader(definitions);
                while (reader.ReadLine() is { } line)
                {
                    token.ThrowIfCancellationRequested();
                    line = line.Trim();
                    var next = line.StartsWith("//", StringComparison.Ordinal) ? line[2..].Trim() :
                        line.StartsWith('[') && line.EndsWith(']') ? line[1..^1].Trim() : null;
                    if (next != null)
                    {
                        if (header != null && hasBody) AddName(names, header);
                        header = next;
                        hasBody = false;
                    }
                    else if (header != null && line.Contains('=')) hasBody = true;
                }
                if (header != null && hasBody) AddName(names, header);
            }
            else if (value is IEnumerable sequence)
            {
                var count = 0;
                foreach (var entry in sequence)
                {
                    token.ThrowIfCancellationRequested();
                    if (++count > TachiePresetCapabilityCoordinator.MaxCandidates || entry is not string name)
                        throw new InvalidOperationException("プリセット名一覧の形式または件数が未対応です。");
                    AddName(names, name);
                }
            }
            if (names.Count > 0) sources.Add(names);
        }
        if (sources.Count > 1) throw new InvalidOperationException("プリセット定義が複数あり、一意に特定できません。");
        var result = sources.SingleOrDefault() ?? Array.Empty<string>();
        if (result.GroupBy(n => n, StringComparer.Ordinal).Any(g => g.Count() != 1))
            throw new InvalidOperationException("プリセット定義に同名の候補が複数あります。");
        return result;
    }

    private static void AddName(List<string> names, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 512)
            throw new InvalidOperationException("立ち絵プリセット名が空か、512文字を超えています。");
        if (names.Count >= TachiePresetCapabilityCoordinator.MaxCandidates)
            throw new InvalidOperationException("立ち絵プリセットの候補数が128件を超えています。");
        names.Add(name); // Do not silently Distinct() duplicate identities.
    }
}
