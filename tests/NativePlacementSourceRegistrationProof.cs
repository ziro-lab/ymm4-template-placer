using System.IO;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static async Task VerifyPlacementSourceRegistration(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Placement Source P7 explicit preset registration and Settings";
        using var scope = new Round3Fixture(timeline, undo);
        var vm = ViewModel!;
        var originalResolver = vm.PresetTargetResolver;

        var config = new P3Config();
        var character = new Character
        {
            Name = "PS P7 Register",
            TachieCharacterParameter = config,
            TachieDefaultFaceParameter = new P3DirectFace()
        };
        var resolverCalls = 0;
        TachiePresetProbeTarget Resolve(Character c)
        {
            resolverCalls++;
            return new(c, config, typeof(P3Config), () => new P3DirectFace(),
                () => ReferenceEquals(c.TachieCharacterParameter, config));
        }

        TachiePresetCandidateDescriptor candidate;
        using (var coordinator = new TachiePresetCapabilityCoordinator(Resolve))
            candidate = (await coordinator.ScanAsync([character], () => true))
                .Single().Capability!.Candidates
                .Single(x => x.CandidateIdentity == "Smile");

        var voice = new VoiceItem(character)
        {
            Frame = 120,
            Length = 40,
            Layer = 20,
            Serif = "P7 register target"
        };
        try
        {
        var fixture = PlacerSettingsStore.Copy(scope.Original);
        fixture.ExpressionBootstrapComplete = true;
        fixture.IntentPaletteRevision = 1;
        fixture.Library = [];
        fixture.TachiePresetSources = [];
        fixture.IntentPalettes = [];
        scope.Apply(fixture, [voice], [voice]);
        vm.PresetTargetResolver = Resolve;

        var snapshot = new VoiceSnapshot(
            voice, voice.CharacterName, voice.Frame, voice.Length,
            voice.Serif ?? "", voice.Layer);
        var baseline = Signature(timeline);

        var first = vm.RegisterTachiePresetSource(snapshot, candidate);
        var current = scope.Current;
        var createdSet = current.IntentPalettes.Single();
        var source = current.TachiePresetSources.Single();

        Assert(first.SourceCreated && first.SetCreated && first.MembershipAdded &&
            first.SourceId == source.Id &&
            first.PaletteId == createdSet.Id &&
            createdSet.ExpressionCandidates &&
            createdSet.Target.CharacterName == character.Name &&
            createdSet.Target.ItemTypeKeys.SequenceEqual(
                [IntentSelectionContext.TypeKey(typeof(VoiceItem))]) &&
            createdSet.Relation.Duration == IntentDuration.UntilRelated &&
            createdSet.Relation.Neighbor == IntentNeighbor.NextSameTypeAndCharacter &&
            createdSet.Entries.Single().SourceId == source.Id &&
            source.CandidateIdentity == candidate.CandidateIdentity &&
            source.CharacterName == character.Name &&
            Signature(timeline) == baseline,
            "PLACEMENT_SOURCE P7 explicit raw preset registration creates one thin Source and one Character-bound expression Set with zero Timeline writes");

        var persisted = new PlacerSettingsStore(PlacerSettingsStore.DefaultPath).Load();
        Assert(persisted.TachiePresetSources.Single() == source &&
            persisted.IntentPalettes.Single().Entries.Single().SourceId == source.Id,
            "PLACEMENT_SOURCE P7 registered Source and Set membership persist through the authoritative Portable settings store");

        var diskBeforeDuplicate = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        var duplicate = vm.RegisterTachiePresetSource(snapshot, candidate, createdSet.Id);
        Assert(!duplicate.SourceCreated && !duplicate.SetCreated && !duplicate.MembershipAdded &&
            scope.Current.TachiePresetSources.Count == 1 &&
            scope.Current.IntentPalettes.Single().Entries.Count == 1 &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(diskBeforeDuplicate) &&
            Signature(timeline) == baseline,
            "PLACEMENT_SOURCE P7 registering the same preset into the same Set is a no-op with no duplicate Source, settings write or Timeline write");

        var secondSet = createdSet with
        {
            Id = Guid.Parse("47000000-0000-4000-8000-000000000020"),
            Name = "P7 second Set",
            Entries = []
        };
        var twoSets = PlacerSettingsStore.Copy(scope.Current);
        twoSets.IntentPalettes.Add(secondSet);
        scope.Apply(twoSets, [voice], [voice]);

        var diskBeforeAmbiguous = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        var ambiguousRejected = false;
        try { _ = vm.RegisterTachiePresetSource(snapshot, candidate); }
        catch (InvalidOperationException) { ambiguousRejected = true; }
        Assert(ambiguousRejected &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(diskBeforeAmbiguous) &&
            Signature(timeline) == baseline,
            "PLACEMENT_SOURCE P7 multiple applicable Sets require an explicit target instead of guessing or writing settings");

        var secondRegistration = vm.RegisterTachiePresetSource(snapshot, candidate, secondSet.Id);
        current = scope.Current;
        Assert(!secondRegistration.SourceCreated &&
            !secondRegistration.SetCreated &&
            secondRegistration.MembershipAdded &&
            current.TachiePresetSources.Count == 1 &&
            current.IntentPalettes.Count == 2 &&
            current.IntentPalettes.All(x => x.Entries.Single().SourceId == source.Id) &&
            Signature(timeline) == baseline,
            "PLACEMENT_SOURCE P7 the same preset registered into another Set reuses one SourceId instead of duplicating source identity");

        var callsBeforeSettings = resolverCalls;
        var session = new IntentSettingsSession(
            current,
            [typeof(VoiceItem)],
            [voice]);
        Assert(resolverCalls == callsBeforeSettings,
            "PLACEMENT_SOURCE P7 opening Settings with registered preset Sources performs no preset/plugin discovery");

        var draftSet = session.Palettes.Single(x => x.Id == createdSet.Id);
        var draftEntry = draftSet.Entries.Single();
        Assert(draftEntry.SourceId == source.Id &&
            draftEntry.Name == candidate.Label &&
            draftEntry.SourceDetail.Contains("立ち絵プリセット", StringComparison.Ordinal) &&
            draftEntry.SourceDetail.Contains(character.Name, StringComparison.Ordinal) &&
            !draftEntry.SourceDetail.Contains("元の登録がありません", StringComparison.Ordinal),
            "PLACEMENT_SOURCE P7 Settings resolves registered preset tiles as first-class Sources instead of broken Template references");

        draftSet.SelectedEntry = draftEntry;
        draftSet.Entries.Remove(draftEntry);
        var removedMembership = session.Build();
        Assert(removedMembership.TachiePresetSources.Count == 1 &&
            removedMembership.TachiePresetSources.Single().Id == source.Id &&
            removedMembership.IntentPalettes.Single(x => x.Id == createdSet.Id).Entries.Count == 0 &&
            Signature(timeline) == baseline,
            "PLACEMENT_SOURCE P7 removing a Set tile keeps the registered Source identity and never deletes existing Timeline content automatically");

        var staleFingerprint = new TachiePresetCapabilityFingerprint(
            candidate.Fingerprint.HostIdentity,
            candidate.Fingerprint.CharacterIdentity,
            "stale-config",
            candidate.Fingerprint.PluginRuntimeType,
            candidate.Fingerprint.FaceParameterRuntimeType,
            candidate.Fingerprint.PluginModuleMvid);
        var wrong = new TachiePresetCandidateDescriptor(
            staleFingerprint,
            candidate.Route,
            candidate.CandidateIdentity,
            candidate.Label,
            candidate.Confidence);
        var beforeWrong = File.ReadAllBytes(PlacerSettingsStore.DefaultPath);
        var rejected = false;
        try { _ = vm.RegisterTachiePresetSource(snapshot, wrong, createdSet.Id); }
        catch (InvalidOperationException) { rejected = true; }
        Assert(rejected &&
            File.ReadAllBytes(PlacerSettingsStore.DefaultPath).SequenceEqual(beforeWrong) &&
            Signature(timeline) == baseline,
            "PLACEMENT_SOURCE P7 stale detected candidate is rejected before settings or Timeline mutation");

        Log("PLACEMENT_SOURCE_P7=PASS");
        }
        finally
        {
            vm.PresetTargetResolver = originalResolver;
        }
    }
}
