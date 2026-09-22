using System.Reflection;
using YukkuriMovieMaker.Plugin.Tachie;

namespace Ymm4TemplatePlacer;

internal sealed record TachiePresetAppliedFace(ITachieFaceParameter Parameter, string StateHash);

internal static class TachiePresetCandidateApplier
{
    public static async Task<TachiePresetAppliedFace> ApplyAsync(
        TachiePresetProbeTarget target,
        TachiePresetCandidateDescriptor candidate,
        Action ensureCurrent,
        CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(ensureCurrent);
        ensureCurrent();
        token.ThrowIfCancellationRequested();
        if (target.Fingerprint != candidate.Fingerprint)
            throw new InvalidOperationException("立ち絵プリセット候補の互換性情報が現在のキャラクター設定と一致しません。候補を読み直してください。");

        var face = target.CreateFreshFace(token);
        if (face is not ITachieFaceParameter parameter)
            throw new InvalidOperationException("作成した表情パラメータがYMM4の立ち絵表情契約と一致しません。");

        string stateHash = candidate.Route.Kind switch
        {
            TachiePresetRouteKind.DirectNamedProperty => ApplyDirect(target, candidate, face, token),
            TachiePresetRouteKind.PropertyEditorLegacy or TachiePresetRouteKind.PropertyEditorModern =>
                await ApplyEditorAsync(target, candidate, face, ensureCurrent, token),
            _ => throw new InvalidOperationException("未対応の立ち絵プリセット適用経路です。")
        };

        ensureCurrent();
        if (!target.IsCurrent(token))
            throw new OperationCanceledException("適用中にキャラクターの立ち絵設定が変わりました。", token);
        var retained = TachiePresetPublicState.TryHash(face, token);
        if (retained == null || retained != stateHash)
            throw new InvalidOperationException("適用後の表情状態を安定して再確認できませんでした。配置していません。");
        return new(parameter, retained);
    }

    private static string ApplyDirect(
        TachiePresetProbeTarget target,
        TachiePresetCandidateDescriptor candidate,
        object face,
        CancellationToken token)
    {
        var properties = face.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => Identity(p) == candidate.Route.PropertyIdentity).ToArray();
        if (properties.Length != 1)
            throw new InvalidOperationException("プリセット適用プロパティを現在の表情パラメータから一意に再解決できません。");
        var property = properties[0];
        if (property.Name != "Preset" || property.PropertyType != typeof(string) ||
            property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true ||
            property.GetIndexParameters().Length != 0)
            throw new InvalidOperationException("現在の直接プリセット経路が候補の契約と一致しません。");
        var names = TachiePresetDiscovery.DirectNames(target.Configuration, token);
        if (names.Count(x => x == candidate.CandidateIdentity) != 1)
            throw new InvalidOperationException("選択した立ち絵プリセット候補を現在の設定から一意に再解決できません。");

        var before = TachiePresetPublicState.TryHash(face, token);
        property.SetValue(face, candidate.CandidateIdentity);
        token.ThrowIfCancellationRequested();
        if (!Equals(property.GetValue(face), candidate.CandidateIdentity))
            throw new InvalidOperationException("立ち絵プリセットの適用を確認できませんでした。");
        var after = TachiePresetPublicState.TryHash(face, token);
        if (before == null || after == null || before == after)
            throw new InvalidOperationException("立ち絵プリセット適用後の状態変化を安全に確認できませんでした。");
        return after;
    }

    private static async Task<string> ApplyEditorAsync(
        TachiePresetProbeTarget target,
        TachiePresetCandidateDescriptor candidate,
        object face,
        Action ensureCurrent,
        CancellationToken token)
    {
        var routes = TachiePresetEditorSession.FindCalibrationRoutes(face)
            .Where(r => r.Descriptor == candidate.Route).ToArray();
        if (routes.Length != 1)
            throw new InvalidOperationException("選択したPropertyEditor経路を現在の表情パラメータから一意に再解決できません。");

        var diagnostics = new TachiePresetCapabilityDiagnostics();
        string? before;
        string? measured;
        using (var editor = TachiePresetEditorSession.Open(routes[0], target.Configuration, face, diagnostics))
        {
            await editor.SettleAsync(ensureCurrent, token);
            var choices = editor.Choices(token);
            var matches = choices.Where(c => c.Label == candidate.CandidateIdentity).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("選択した立ち絵プリセット候補を現在のPropertyEditorから一意に再解決できません。");
            var selected = matches[0];
            var alternate = choices.FirstOrDefault(c => c.Label != candidate.CandidateIdentity);
            if (alternate != null)
            {
                alternate.Selector.SelectedItem = alternate.Item;
                await editor.SettleAsync(ensureCurrent, token);
                matches = editor.Choices(token).Where(c => c.Label == candidate.CandidateIdentity).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException("切り替え後の立ち絵プリセット候補を一意に再解決できません。");
                selected = matches[0];
            }

            before = TachiePresetPublicState.TryHash(face, token);
            selected.Selector.SelectedItem = selected.Item;
            await editor.SettleAsync(ensureCurrent, token);
            if (!ReferenceEquals(selected.Selector.SelectedItem, selected.Item))
                throw new InvalidOperationException("立ち絵プリセットの選択が反映されませんでした。");
            measured = TachiePresetPublicState.TryHash(face, token);
        }

        ensureCurrent();
        var retained = TachiePresetPublicState.TryHash(face, token);
        if (before == null || measured == null || retained == null ||
            before == retained || measured != retained)
            throw new InvalidOperationException("PropertyEditor終了後の表情状態を安定して確認できませんでした。");
        return retained;
    }

    private static string Identity(PropertyInfo property) =>
        (property.DeclaringType?.FullName ?? property.ReflectedType?.FullName ?? property.Name) + "." + property.Name;
}
