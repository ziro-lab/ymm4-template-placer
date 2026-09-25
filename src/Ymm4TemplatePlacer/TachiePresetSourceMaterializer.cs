using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal static class TachiePresetSourceMaterializer
{
    public static async Task<MaterializedPlacementSource> MaterializeAsync(
        TachiePresetSourceEntry source,
        Character character,
        Func<Character, TachiePresetProbeTarget>? resolver = null,
        CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(character);
        source.Validate();
        token.ThrowIfCancellationRequested();

        if (!string.Equals(character.Name, source.CharacterName, StringComparison.Ordinal))
            throw new InvalidOperationException("立ち絵プリセットSourceのキャラクターと現在の対象が一致しません。");

        var resolve = resolver ?? TachiePresetProbeTarget.Resolve;
        var target = resolve(character);
        if (!source.Matches(target))
            throw new InvalidOperationException("立ち絵プリセットSourceが現在の立ち絵Plugin/Parameter構成と一致しません。");

        var fingerprint = target.Fingerprint;
        void EnsureCurrent()
        {
            TachiePresetPublicState.RequireUiThread();
            token.ThrowIfCancellationRequested();
            if (!source.Matches(target) || target.Fingerprint != fingerprint || !target.IsCurrent(token))
                throw new OperationCanceledException("立ち絵プリセットSourceの適用中にキャラクターまたは立ち絵設定が変わりました。", token);
        }

        EnsureCurrent();
        var candidate = new TachiePresetCandidateDescriptor(
            fingerprint,
            source.Route(),
            source.CandidateIdentity,
            source.DisplayName,
            TachiePresetCapabilityLevel.Experimental);
        var applied = await TachiePresetCandidateApplier.ApplyAsync(
            target, candidate, EnsureCurrent, token);
        EnsureCurrent();

        var item = new TachieFaceItem(character)
        {
            TachieFaceParameter = applied.Parameter,
            Frame = 0,
            Layer = 0,
            Group = 0
        };
        item.Remark = PluginRemarks.WithoutAssociation(item.Remark);
        PlacementMath.ValidateSpan(item.Frame, item.Length);

        var semanticHash = source.SemanticHash();
        return new MaterializedPlacementSource(
            source.Id,
            PlacementSourceKind.TachiePreset,
            [item],
            item.Length,
            item.Layer,
            source.CharacterName,
            semanticHash,
            EnsureCurrent);
    }
}
