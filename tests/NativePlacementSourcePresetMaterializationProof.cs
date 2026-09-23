using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementSourcePresetMaterialization(Timeline timeline)
    {
        stage = "Placement Source P2 registered preset materialization";
        var signature = Signature(timeline);
        var config = new P3Config();
        var directCharacter = new Character
        {
            Name = "PS P2 Direct",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = new P3DirectFace()
        };
        var legacyCharacter = new Character
        {
            Name = "PS P2 Legacy",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = new P3LegacyFace()
        };

        var directCurrent = true;
        var legacyCurrent = true;
        TachiePresetProbeTarget DirectResolve(Character c) =>
            new(c, config, typeof(P3Config), () => new P3DirectFace(),
                () => directCurrent && ReferenceEquals(c.TachieCharacterParameter, config));
        TachiePresetProbeTarget LegacyResolve(Character c) =>
            new(c, config, typeof(P3Config), () => new P3LegacyFace(),
                () => legacyCurrent && ReferenceEquals(c.TachieCharacterParameter, config));

        TachiePresetCandidateDescriptor directCandidate;
        TachiePresetCandidateDescriptor legacyCandidate;
        using (var coordinator = new TachiePresetCapabilityCoordinator(DirectResolve))
            directCandidate = (await coordinator.ScanAsync([directCharacter], () => true)).Single().Capability!.Candidates
                .Single(x => x.CandidateIdentity == "Smile");
        using (var coordinator = new TachiePresetCapabilityCoordinator(LegacyResolve))
            legacyCandidate = (await coordinator.ScanAsync([legacyCharacter], () => true)).Single().Capability!.Candidates
                .Single(x => x.CandidateIdentity == "Smile");

        var directTarget = DirectResolve(directCharacter);
        var directSource = TachiePresetSourceEntry.From(
            directTarget, directCandidate, "登録済みSmile",
            Guid.Parse("20000000-0000-4000-8000-000000000001"));
        var direct = await TachiePresetSourceMaterializer.MaterializeAsync(
            directSource, directCharacter, DirectResolve);
        var directItem = direct.Items.Single() as TachieFaceItem;
        Assert(direct.SourceId == directSource.Id &&
            direct.Kind == PlacementSourceKind.TachiePreset &&
            direct.SemanticHash == directSource.SemanticHash() &&
            direct.CharacterName == directCharacter.Name &&
            directItem is { Frame: 0, Layer: 0, Group: 0, TachieFaceParameter: P3DirectFace { Preset: "Smile" } } &&
            !ReferenceEquals(directItem.TachieFaceParameter, directCharacter.TachieDefaultFaceParameter),
            "PLACEMENT_SOURCE P2 registered direct preset materializes one independent fresh normalized TachieFaceItem");

        config.Revision++;
        var afterConfigChange = await TachiePresetSourceMaterializer.MaterializeAsync(
            directSource, directCharacter, DirectResolve);
        Assert(afterConfigChange.Items.Single() is TachieFaceItem
            { TachieFaceParameter: P3DirectFace { Preset: "Smile" } },
            "PLACEMENT_SOURCE P2 persisted source survives ordinary Character configuration-state changes and rebinds to the current fingerprint");
        config.Revision--;

        var legacyTarget = LegacyResolve(legacyCharacter);
        var legacySource = TachiePresetSourceEntry.From(
            legacyTarget, legacyCandidate, "登録済みLegacy Smile",
            Guid.Parse("20000000-0000-4000-8000-000000000002"));
        var legacy = await TachiePresetSourceMaterializer.MaterializeAsync(
            legacySource, legacyCharacter, LegacyResolve);
        Assert(legacy.Items.Single() is TachieFaceItem
            { Frame: 0, Layer: 0, Group: 0, TachieFaceParameter: P3LegacyFace { Mood: "Smile", Amount: 7 } } &&
            P3EditorFixture.Handlers.Count == 0,
            "PLACEMENT_SOURCE P2 registered PropertyEditor preset resolves only its saved route/candidate and cleans temporary bindings");

        var wrongCharacterResolves = 0;
        var wrongCharacter = new Character
        {
            Name = "PS P2 Other",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = new P3DirectFace()
        };
        var wrongCharacterRejected = false;
        try
        {
            _ = await TachiePresetSourceMaterializer.MaterializeAsync(
                directSource, wrongCharacter,
                c => { wrongCharacterResolves++; return DirectResolve(c); });
        }
        catch (InvalidOperationException) { wrongCharacterRejected = true; }
        Assert(wrongCharacterRejected && wrongCharacterResolves == 0,
            "PLACEMENT_SOURCE P2 Character-bound source rejects another Character before plugin/editor resolution");

        var wrongSurface = directSource with { PluginModuleMvid = Guid.Parse("30000000-0000-4000-8000-000000000003") };
        var wrongSurfaceRejected = false;
        try { _ = await TachiePresetSourceMaterializer.MaterializeAsync(wrongSurface, directCharacter, DirectResolve); }
        catch (InvalidOperationException) { wrongSurfaceRejected = true; }
        Assert(wrongSurfaceRejected,
            "PLACEMENT_SOURCE P2 plugin-surface identity mismatch fails closed before preset application");

        var missing = directSource with { CandidateIdentity = "MissingPreset" };
        var missingRejected = false;
        try { _ = await TachiePresetSourceMaterializer.MaterializeAsync(missing, directCharacter, DirectResolve); }
        catch (InvalidOperationException) { missingRejected = true; }
        Assert(missingRejected,
            "PLACEMENT_SOURCE P2 missing registered candidate fails closed instead of selecting another label");

        var oldAfterBind = P3EditorFixture.AfterBind;
        try
        {
            legacyCurrent = true;
            P3EditorFixture.AfterBind = () =>
            {
                legacyCurrent = false;
                P3EditorFixture.AfterBind = null;
            };
            var cancelled = false;
            try { _ = await TachiePresetSourceMaterializer.MaterializeAsync(legacySource, legacyCharacter, LegacyResolve); }
            catch (OperationCanceledException) { cancelled = true; }
            Assert(cancelled && P3EditorFixture.Handlers.Count == 0,
                "PLACEMENT_SOURCE P2 Character/config scope change during editor application cancels and cleans temporary bindings");
        }
        finally
        {
            legacyCurrent = true;
            P3EditorFixture.AfterBind = oldAfterBind;
        }

        direct.ValidateCurrent();
        directCurrent = false;
        var staleGuardRejected = false;
        try { direct.ValidateCurrent(); }
        catch (OperationCanceledException) { staleGuardRejected = true; }
        finally { directCurrent = true; }
        Assert(staleGuardRejected,
            "PLACEMENT_SOURCE P2 materialized source retains a current-source guard through later geometry planning");

        Assert(P3EditorFixture.Handlers.Count == 0 && Signature(timeline) == signature,
            "PLACEMENT_SOURCE P2 all materialization success/failure paths are Timeline zero-write and editor-clean");
        Log("PLACEMENT_SOURCE_P2=PASS");
    }
}
