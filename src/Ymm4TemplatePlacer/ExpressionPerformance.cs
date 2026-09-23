using System.Diagnostics;
using System.Text;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal sealed record ExpressionCapturedItem(IItem Item, bool IsVoice, string Character, int Group, string Remark);
internal sealed record ExpressionCandidateDescriptor(
    FaceTemplate Template,
    string Character,
    Guid PaletteId,
    Guid EntryId,
    string? GeometryHash,
    string DisplayName,
    string PaletteName,
    string TemplateName,
    bool AppliesToVoice);
internal sealed record ExpressionHostSnapshot(
    long Generation,
    Timeline Timeline,
    IReadOnlyList<VoiceSnapshot> Voices,
    IReadOnlyList<ExpressionCapturedItem> Items,
    IReadOnlyList<ExpressionCandidateDescriptor> Candidates,
    bool CandidateGenerationChanged,
    string? PreviousFingerprint,
    IReadOnlyList<VoiceSnapshot> PreviousVoices,
    IReadOnlyList<ExpressionCapturedItem> PreviousItems)
{
    public ExpressionSourceMode SourceMode { get; init; }
    public IReadOnlyList<TachiePresetCharacterCapability> PresetCapabilities { get; init; } = [];
    public IReadOnlyList<RegisteredPresetExpressionSource> RegisteredPresetCandidates { get; init; } = [];
    public IReadOnlyDictionary<VoiceItem, TachiePresetCandidateDescriptor> PreviousPresetChoices { get; init; } =
        new Dictionary<VoiceItem, TachiePresetCandidateDescriptor>(ReferenceEqualityComparer.Instance);
}
internal sealed record ExpressionPreparedRow(
    VoiceSnapshot Target,
    IReadOnlyList<TemplateChoice> Choices,
    TemplateChoice? Selected,
    string? UnavailableLabel,
    bool AssociationMatchesSelection, string? SourceNotice = null);
internal sealed record ExpressionPreparedResult(
    long Generation,
    Timeline Timeline,
    string Fingerprint,
    IReadOnlyList<ExpressionCapturedItem> Items,
    IReadOnlyList<ExpressionPreparedRow> Rows,
    bool NoSemanticChange,
    int AssociationItemsParsed,
    int CandidateKeysBuilt,
    TimeSpan PreparationElapsed,
    int PreparationThreadId)
{
    public ExpressionSourceMode SourceMode { get; init; }
}

internal sealed record CapturedManagedExpressionAssociation(
    long? Serial,
    ManagedExpressionSourceDescriptor? Descriptor,
    IReadOnlyList<IItem>? Members,
    string? Error)
{
    public bool IsError => Error != null;
}

internal sealed class ExpressionAssociationIndex
{
    private readonly Dictionary<IItem, CapturedManagedExpressionAssociation> byVoice = new(ReferenceEqualityComparer.Instance);
    public int ParsedItemCount { get; }

    private ExpressionAssociationIndex(int parsedItemCount) => ParsedItemCount = parsedItemCount;

    public static ExpressionAssociationIndex Build(IReadOnlyList<ExpressionCapturedItem> items, CancellationToken token)
    {
        var index = new ExpressionAssociationIndex(items.Count);
        var itemSet = items.Select(x => x.Item).ToHashSet(ReferenceEqualityComparer.Instance);
        var voiceTag = new Dictionary<IItem, (AssociationTagState State, long Serial)>(ReferenceEqualityComparer.Instance);
        var voiceGroups = new Dictionary<(long Serial, string Character), List<IItem>>();
        var sourceBySerial = new Dictionary<long, List<ExpressionCapturedItem>>();
        var managedTags = new Dictionary<IItem, (AssociationTagState State, ManagedExpressionSourceDescriptor? Descriptor)>(ReferenceEqualityComparer.Instance);
        var managedGroups = new Dictionary<Guid, List<IItem>>();

        foreach (var item in items)
        {
            token.ThrowIfCancellationRequested();
            if (item.IsVoice)
            {
                var state = AssociationTag.Voice(item.Remark, out var serial);
                voiceTag[item.Item] = (state, serial);
                if (state == AssociationTagState.Valid)
                {
                    var key = (serial, item.Character);
                    if (!voiceGroups.TryGetValue(key, out var group)) voiceGroups[key] = group = [];
                    group.Add(item.Item);
                }
            }

            var sourceState = AssociationTag.Source(item.Remark, out var source);
            if (sourceState == AssociationTagState.Valid)
            {
                if (!sourceBySerial.TryGetValue(source!.Serial, out var related)) sourceBySerial[source.Serial] = related = [];
                related.Add(item);
            }

            var managedState = ManagedExpressionSourceDescriptor.Read(item.Remark, out var descriptor);
            managedTags[item.Item] = (managedState, descriptor);
            if (managedState == AssociationTagState.Valid)
            {
                if (!managedGroups.TryGetValue(descriptor!.Group, out var group)) managedGroups[descriptor.Group] = group = [];
                group.Add(item.Item);
            }
        }

        foreach (var voice in items.Where(x => x.IsVoice))
        {
            token.ThrowIfCancellationRequested();
            if (!itemSet.Contains(voice.Item)) continue;
            var tag = voiceTag[voice.Item];
            if (tag.State == AssociationTagState.Invalid)
            {
                index.byVoice[voice.Item] = new(null, null, null, "対象音声の関連付けタグが不正または重複しています。");
                continue;
            }
            if (tag.State == AssociationTagState.None)
            {
                index.byVoice[voice.Item] = new(null, null, null, null);
                continue;
            }
            if (!voiceGroups.TryGetValue((tag.Serial, voice.Character), out var targets) || targets.Count != 1 || !ReferenceEquals(targets[0], voice.Item))
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, "関連付けIDとキャラクター名が一致する音声を一意に特定できません。");
                continue;
            }
            if (!sourceBySerial.TryGetValue(tag.Serial, out var related) || related.Count == 0)
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, null);
                continue;
            }

            var tagged = new List<(ExpressionCapturedItem Item, ManagedExpressionSourceDescriptor Descriptor)>();
            var bad = false;
            foreach (var relatedItem in related)
            {
                var state = managedTags[relatedItem.Item];
                if (state.State != AssociationTagState.Valid || state.Descriptor == null) { bad = true; break; }
                tagged.Add((relatedItem, state.Descriptor));
            }
            if (bad)
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, "対象音声のPlugin-managed Bundleに欠落または混在した関連付け情報があります。");
                continue;
            }

            var ordered = tagged.OrderBy(x => x.Descriptor.Index).ToArray();
            var descriptor = ordered[0].Descriptor;
            if (ordered.Length != descriptor.Count ||
                ordered.Any(x => !descriptor.SameGroup(x.Descriptor)) ||
                !ordered.Select(x => x.Descriptor.Index).SequenceEqual(Enumerable.Range(0, descriptor.Count)))
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, "対象音声のPlugin-managed Bundleに欠落・重複・コピーまたは設定不一致があります。");
                continue;
            }
            if (ordered.Any(x => x.Item.Group != 0))
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, "対象のPlugin-managed BundleはYMM4側でグループ化されています。");
                continue;
            }
            if (ordered.Any(x => !HasPlacementMarker(x.Item.Remark)))
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, "対象音声の関連アイテムをTemplate Placer生成物として確認できません。");
                continue;
            }
            var sameGroup = managedGroups.GetValueOrDefault(descriptor.Group) ?? [];
            var orderedRefs = ordered.Select(x => x.Item.Item).ToHashSet(ReferenceEqualityComparer.Instance);
            if (sameGroup.Count != ordered.Length || sameGroup.Any(x => !orderedRefs.Contains(x)))
            {
                index.byVoice[voice.Item] = new(tag.Serial, null, null, "同じBundle IDを持つコピーまたは別アイテムがあります。");
                continue;
            }
            index.byVoice[voice.Item] = new(tag.Serial, descriptor, ordered.Select(x => x.Item.Item).ToArray(), null);
        }
        return index;
    }

    private static bool HasPlacementMarker(string remark) =>
        remark.Split('\n').Any(line => line.TrimEnd('\r') == PlacementEngine.Marker);

    public CapturedManagedExpressionAssociation Read(VoiceItem voice) => byVoice.TryGetValue(voice, out var value)
        ? value : new(null, null, null, "対象音声が現在の式一覧Snapshotにありません。");
}

internal static partial class ExpressionPreparation
{
#if YMM4_PROOF
    internal static ManualResetEventSlim? ProofPrepareEntered;
    internal static ManualResetEventSlim? ProofPrepareGate;
#endif
    public static string Fingerprint(IReadOnlyList<ExpressionCapturedItem> items, IReadOnlyList<VoiceSnapshot> voices)
    {
        using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        static void Add(System.Security.Cryptography.IncrementalHash hash, string text) => hash.AppendData(Encoding.UTF8.GetBytes(text));
        foreach (var voice in voices.OrderBy(x => x.Frame).ThenBy(x => x.Layer))
            Add(hash, $"V|{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(voice.Voice)}|{voice.Character}|{voice.Frame}|{voice.Length}|{voice.Layer}|{voice.Serif}\n");
        foreach (var item in items)
            Add(hash, $"I|{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item.Item)}|{item.Group}|{item.Remark}\n");
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    public static ExpressionPreparedResult Prepare(ExpressionHostSnapshot snapshot, CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        token.ThrowIfCancellationRequested();
#if YMM4_PROOF
        ProofPrepareEntered?.Set();
        ProofPrepareGate?.Wait(token);
#endif
        var preparationThreadId = Environment.CurrentManagedThreadId;
        var sorted = snapshot.Voices.OrderBy(x => x.Frame).ThenBy(x => x.Layer).ToArray();
        var fingerprint = Fingerprint(snapshot.Items, sorted);
        // Voice Rows are a Voice-list/candidate projection. Non-Voice Timeline item
        // insertions/removals (including our managed expression bundle) must not tear
        // down the row model by themselves. Exact live association validation remains
        // on mutation paths, and the newest captured item snapshot is still retained.
        var voicesSame = snapshot.PreviousVoices.Count == sorted.Length &&
            snapshot.PreviousVoices.Select((x, i) => SameVoice(x, sorted[i])).All(x => x);
        if (voicesSame && !snapshot.CandidateGenerationChanged)
            return new(snapshot.Generation, snapshot.Timeline, fingerprint, snapshot.Items, [], true, 0, 0, sw.Elapsed, preparationThreadId) { SourceMode = snapshot.SourceMode };

        var associations = ExpressionAssociationIndex.Build(snapshot.Items, token);
        if (snapshot.SourceMode == ExpressionSourceMode.TachiePreset)
            return PreparePresetChoices(snapshot, sorted, fingerprint, associations, sw, preparationThreadId, token);
        var candidates = BuildCandidateIndex(snapshot.Candidates, sorted, token, out var candidateKeys);
        var candidateMetadata = snapshot.Candidates.ToDictionary(x => x.Template, (IEqualityComparer<FaceTemplate>)ReferenceEqualityComparer.Instance);
        var prepared = new List<ExpressionPreparedRow>(sorted.Length);
        foreach (var voice in sorted)
        {
            token.ThrowIfCancellationRequested();
            var choices = candidates.GetValueOrDefault(voice.Character) ?? EmptyChoices();
            var association = associations.Read(voice.Voice);
            TemplateChoice? selected = choices.FirstOrDefault(x => x.Template == null);
            string? unavailable = null;
            string? sourceNotice = null;
            if (association.IsError)
            {
                selected = new(null, "⚠ 関連付けを確認", null, false) { IsInvalidAssociation = true };
            }
            else if (association.Descriptor is { Kind: ManagedExpressionSourceKind.Template, Template: { } descriptor })
            {
                selected = choices.FirstOrDefault(x => x.Template != null &&
                    candidateMetadata.TryGetValue(x.Template, out var metadata) &&
                    metadata.PaletteId == descriptor.Palette && metadata.EntryId == descriptor.Entry &&
                    metadata.GeometryHash == descriptor.GeometryHash);
                if (selected == null) unavailable = "⚠ 現在の関連表情（選択元を確認）";
            }
            else if (association.Descriptor is { Kind: ManagedExpressionSourceKind.TachiePreset })
            {
                selected = new(null, "現在：立ち絵プリセット由来の表情") { IsCurrentOtherSource = true };
                sourceNotice = "現在の表情は立ち絵プリセットから配置されています。表示切替では変更しません。";
            }
            prepared.Add(new(voice, choices, selected, unavailable, true, sourceNotice));
        }
        sw.Stop();
        return new(snapshot.Generation, snapshot.Timeline, fingerprint, snapshot.Items, prepared, false,
            associations.ParsedItemCount, candidateKeys, sw.Elapsed, preparationThreadId);
    }

    private static Dictionary<string, IReadOnlyList<TemplateChoice>> BuildCandidateIndex(
        IReadOnlyList<ExpressionCandidateDescriptor> descriptors,
        IReadOnlyList<VoiceSnapshot> voices,
        CancellationToken token,
        out int keysBuilt)
    {
        var result = new Dictionary<string, IReadOnlyList<TemplateChoice>>(StringComparer.Ordinal);
        var characters = voices.Select(x => x.Character).Distinct(StringComparer.Ordinal).ToArray();
        keysBuilt = characters.Length;
        foreach (var character in characters)
        {
            token.ThrowIfCancellationRequested();
            var applicable = descriptors.Where(x => x.AppliesToVoice && x.Character == character).ToArray();
            var duplicateNames = applicable.GroupBy(x => x.DisplayName, StringComparer.Ordinal).Where(x => x.Count() > 1)
                .Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
            var list = new List<TemplateChoice>
            {
                new(null, applicable.Length == 0 ? "— 候補なし —" : "— 配置しない —")
            };
            foreach (var item in applicable)
            {
                var label = duplicateNames.Contains(item.DisplayName)
                    ? $"{item.DisplayName} — {item.PaletteName} / {item.TemplateName}" : item.DisplayName;
                list.Add(new(item.Template, label, label));
            }
            result[character] = list.ToArray();
        }
        return result;
    }

    private static IReadOnlyList<TemplateChoice> EmptyChoices() => [new(null, "— 候補なし —")];
    private static bool SameVoice(VoiceSnapshot left, VoiceSnapshot right) => ReferenceEquals(left.Voice, right.Voice) &&
        left.Character == right.Character && left.Frame == right.Frame && left.Length == right.Length && left.Serif == right.Serif && left.Layer == right.Layer;
    private static bool SameItems(IReadOnlyList<ExpressionCapturedItem> left, IReadOnlyList<ExpressionCapturedItem> right)
    {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
            if (!ReferenceEquals(left[i].Item, right[i].Item) || left[i].IsVoice != right[i].IsVoice || left[i].Character != right[i].Character ||
                left[i].Group != right[i].Group || left[i].Remark != right[i].Remark) return false;
        return true;
    }
}

internal sealed class ExpressionPerformanceDiagnostics
{
    public int HostCaptures;
    public int TimelineItemsCaptured;
    public int FullVoiceReconciles;
    public int IncrementalVoiceReconciles;
    public int AssociationIndexBuilds;
    public int AssociationItemsParsed;
    public int CandidateCatalogBuilds;
    public int CandidateKeyBuilds;
    public int FullRowPublishes;
    public int IncrementalRowUpdates;
    public int BatchCollectionPublishes;
    public int CancelledLoads;
    public int StaleResultsDiscarded;
    public long LastCaptureMilliseconds;
    public long LastPrepareMilliseconds;
    public long LastPublishMilliseconds;
    public int LastCaptureThreadId;
    public int LastPrepareThreadId;
    public int LastPublishThreadId;

    public ExpressionPerformanceDiagnostics Snapshot() => (ExpressionPerformanceDiagnostics)MemberwiseClone();
}
