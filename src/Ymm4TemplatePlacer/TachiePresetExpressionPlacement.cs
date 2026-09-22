using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal sealed class TachiePresetExpressionMutation
{
    private readonly AssignmentRow row;
    private readonly TachiePresetProbeTarget target;
    private readonly TachiePresetCandidateDescriptor candidate;
    private readonly ManagedExpressionAssociation existing;

    public PlacementPlan Plan { get; }
    public TachieFaceItem Addition { get; }
    public string StateHash { get; }
    public long NextSerial { get; }

    private TachiePresetExpressionMutation(
        AssignmentRow row,
        TachiePresetProbeTarget target,
        TachiePresetCandidateDescriptor candidate,
        ManagedExpressionAssociation existing,
        PlacementPlan plan,
        TachieFaceItem addition,
        string stateHash,
        long nextSerial)
    {
        this.row = row;
        this.target = target;
        this.candidate = candidate;
        this.existing = existing;
        Plan = plan;
        Addition = addition;
        StateHash = stateHash;
        NextSerial = nextSerial;
    }

    public static async Task<TachiePresetExpressionMutation> CreateAsync(
        Timeline timeline,
        IReadOnlyList<AssignmentRow> rows,
        AssignmentRow row,
        ExpressionPreset preset,
        TachiePresetCandidateDescriptor candidate,
        Func<Character, TachiePresetProbeTarget> resolver,
        long minimumSerial,
        CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(resolver);
        if (candidate.Confidence != TachiePresetCapabilityLevel.Strong)
            throw new InvalidOperationException("状態を安定して確認できない実験候補は、まだ配置できません。");
        if (row.SelectedChoice.TachiePreset != candidate || !row.SelectedChoice.IsAvailable)
            throw new InvalidOperationException("現在の行で選択されている立ち絵プリセット候補と一致しません。候補を選び直してください。");

        ValidateRows(timeline, rows, row);
        var voice = row.Target.Voice;
        var character = voice.Character ?? throw new InvalidOperationException("対象音声のキャラクターを取得できません。");
        var target = resolver(character);
        if (target.Fingerprint != candidate.Fingerprint)
            throw new InvalidOperationException("立ち絵プリセット候補が現在のキャラクター設定と一致しません。候補を読み直してください。");

        var initialItems = timeline.Items;
        void EnsureCurrent()
        {
            TachiePresetPublicState.RequireUiThread();
            token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(timeline.Items, initialItems))
                throw new OperationCanceledException("適用中にタイムラインが変更されました。", token);
            ValidateRows(timeline, rows, row);
            if (!target.IsCurrent(token))
                throw new OperationCanceledException("適用中にキャラクターの立ち絵設定が変わりました。", token);
        }

        var existing = ManagedExpressionReader.Read(timeline, voice);
        ValidateExistingPresetState(existing.Bundle, token);
        EnsureCurrent();

        var applied = await TachiePresetCandidateApplier.ApplyAsync(target, candidate, EnsureCurrent, token);
        EnsureCurrent();

        var addition = new TachieFaceItem(character)
        {
            TachieFaceParameter = applied.Parameter
        };
        var orderedVoices = rows.OrderBy(x => x.Frame).ThenBy(x => x.Target.Layer).ThenBy(x => x.No)
            .Select(x => x.Target).ToArray();
        var span = CharacterExpressionProfile.Span(row.Target, orderedVoices, preset);
        addition.Frame = span.Frame;
        addition.Length = span.Length;

        var removals = existing.Bundle?.Members.ToArray() ?? [];
        var removing = removals.ToHashSet(ReferenceEqualityComparer.Instance);
        addition.Layer = LayerPlanner.Find(
            addition.Frame,
            addition.Length,
            addition.Layer,
            preset.Layer,
            CharacterLayerMode.Base,
            character,
            timeline.Items.Where(x => !removing.Contains(x)));

        var allocator = new IntentAssociationSerialAllocator(timeline, Math.Max(1, minimumSerial));
        var serial = existing.Serial ?? allocator.Allocate();
        PlannedItemUpdate[] updates = existing.Serial == null
            ? [PlannedItemUpdate.RemarkOnly(voice, PluginRemarks.Append(voice.Remark, AssociationTag.TargetLine(serial)))]
            : [];

        var tag = TachiePresetAssociationTag.Create(Guid.NewGuid(), 0, 1, candidate, applied.StateHash);
        addition.Remark = PluginRemarks.Append(
            PluginRemarks.Append(
                PluginRemarks.Append(PluginRemarks.WithoutAssociation(addition.Remark), PlacementEngine.Marker),
                AssociationTag.SourceLine(serial)),
            tag.Line);

        EnsureCurrent();
        var plan = PlacementPlan.Create(timeline, [addition], updates, removals);
        return new(row, target, candidate, existing, plan, addition, applied.StateHash, allocator.NextSerial);
    }

    internal void ValidateCurrent(Timeline timeline, CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ValidateRows(timeline, [row], row);
        if (target.Fingerprint != candidate.Fingerprint || !target.IsCurrent(token))
            throw new InvalidOperationException("計画後に立ち絵設定が変わりました。配置していません。");
        var current = ManagedExpressionReader.Read(timeline, row.Target.Voice);
        if (!SameAssociation(existing, current))
            throw new InvalidOperationException("計画後に現在の関連表情が変わりました。配置していません。");
        ValidateExistingPresetState(current.Bundle, token);
    }

    private static bool SameAssociation(ManagedExpressionAssociation left, ManagedExpressionAssociation right)
    {
        if (left.Serial != right.Serial) return false;
        if (left.Bundle == null || right.Bundle == null) return left.Bundle == null && right.Bundle == null;
        if (left.Bundle.Descriptor != right.Bundle.Descriptor || left.Bundle.Members.Count != right.Bundle.Members.Count) return false;
        return left.Bundle.Members.Select((item, i) => ReferenceEquals(item, right.Bundle.Members[i])).All(x => x);
    }

    private static void ValidateRows(Timeline timeline, IReadOnlyList<AssignmentRow> rows, AssignmentRow selected)
    {
        if (!rows.Contains(selected))
            throw new InvalidOperationException("選択した音声行が現在の一覧にありません。");
        PlacementEngine.ValidateSnapshot(timeline, rows.Select(x => x.Target).ToArray());
    }

    private static void ValidateExistingPresetState(ManagedExpressionBundle? bundle, CancellationToken token)
    {
        if (bundle?.Descriptor is not { Kind: ManagedExpressionSourceKind.TachiePreset, TachiePreset: { } descriptor }) return;
        if (bundle.Members.Count != 1 || bundle.Members[0] is not TachieFaceItem face || face.TachieFaceParameter == null)
            throw new InvalidOperationException("現在の立ち絵プリセット表情を安全に一意確認できません。変更していません。");
        var current = TachiePresetPublicState.TryHash(face.TachieFaceParameter, token);
        if (current == null || current != descriptor.StateHash)
            throw new InvalidOperationException("現在の立ち絵プリセット表情は配置後に変更されています。自動置換せず停止しました。");
    }
}
