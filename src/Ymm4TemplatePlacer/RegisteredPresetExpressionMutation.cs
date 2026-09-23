using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal sealed class RegisteredPresetExpressionMutation
{
    private readonly AssignmentRow row;
    private readonly ManagedExpressionAssociation existing;
    private readonly PlacementSourceGeometry geometry;
    private readonly Guid paletteId;
    private readonly string paletteSnapshot;
    private readonly Guid sourceId;
    private readonly string sourceHash;

    public PlacementPlan Plan { get; }
    public TachieFaceItem Addition { get; }
    public long NextSerial { get; }

    private RegisteredPresetExpressionMutation(
        AssignmentRow row,
        ManagedExpressionAssociation existing,
        PlacementSourceGeometry geometry,
        Guid paletteId,
        string paletteSnapshot,
        Guid sourceId,
        string sourceHash,
        PlacementPlan plan,
        TachieFaceItem addition,
        long nextSerial)
    {
        this.row = row;
        this.existing = existing;
        this.geometry = geometry;
        this.paletteId = paletteId;
        this.paletteSnapshot = paletteSnapshot;
        this.sourceId = sourceId;
        this.sourceHash = sourceHash;
        Plan = plan;
        Addition = addition;
        NextSerial = nextSerial;
    }

    public static async Task<RegisteredPresetExpressionMutation> CreateAsync(
        Timeline timeline,
        AssignmentRow row,
        RegisteredPresetExpressionSource requested,
        PlacerSettings settings,
        Func<Character, TachiePresetProbeTarget> resolver,
        long minimumSerial,
        CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(resolver);
        ValidateRow(timeline, row);
        token.ThrowIfCancellationRequested();

        var current = requested.ResolveCurrent(settings);
        var voice = row.Target.Voice;
        var character = voice.Character
            ?? throw new InvalidOperationException("対象音声のキャラクターを取得できません。");
        if (!string.Equals(character.Name, current.Source.CharacterName, StringComparison.Ordinal))
            throw new InvalidOperationException("登録済み立ち絵プリセットSourceと対象音声のキャラクターが一致しません。");

        var existing = ManagedExpressionReader.Read(timeline, voice);
        ManagedExpressionSafety.ValidatePresetState(existing.Bundle, token);
        var removals = existing.Bundle?.Members.ToArray() ?? [];
        var removing = removals.ToHashSet(ReferenceEqualityComparer.Instance);

        var materialized = await TachiePresetSourceMaterializer.MaterializeAsync(
            current.Source, character, resolver, token);
        token.ThrowIfCancellationRequested();
        ValidateRow(timeline, row);

        var context = IntentSelectionContext.ForItems(timeline, [voice]);
        var geometry = IntentPlacementGeometry.Prepare(
            timeline,
            context,
            current.Palette,
            current.Entry,
            materialized,
            timeline.Items.Where(x => !removing.Contains(x)));
        if (geometry.Skipped)
            throw new InvalidOperationException("このSetの配置条件では配置先を決められません。現在の表情は変更していません。");
        if (geometry.Items.Count != 1 || geometry.Items[0] is not TachieFaceItem addition ||
            addition.TachieFaceParameter == null)
            throw new InvalidOperationException("登録済み立ち絵プリセットSourceから安全な単一表情アイテムを計画できません。");

        var stateHash = TachiePresetPublicState.TryHash(addition.TachieFaceParameter, token)
            ?? throw new InvalidOperationException("配置する立ち絵プリセット表情の状態を確認できません。");

        var allocator = new IntentAssociationSerialAllocator(
            timeline, Math.Max(1, minimumSerial));
        var serial = existing.Serial ?? allocator.Allocate();
        PlannedItemUpdate[] updates = existing.Serial == null
            ? [PlannedItemUpdate.RemarkOnly(
                voice,
                PluginRemarks.Append(voice.Remark, AssociationTag.TargetLine(serial)))]
            : [];

        var tag = TachiePresetSourceAssociationTag.Create(
            Guid.NewGuid(), current.Palette.Id, current.Source, stateHash);
        addition.Remark = PluginRemarks.Append(
            PluginRemarks.Append(
                PluginRemarks.Append(
                    PluginRemarks.WithoutAssociation(addition.Remark),
                    PlacementEngine.Marker),
                AssociationTag.SourceLine(serial)),
            tag.Line);

        ValidateRow(timeline, row);
        materialized.ValidateCurrent();
        var plan = PlacementPlan.Create(
            timeline, [addition], updates, removals);

        return new(
            row,
            existing,
            geometry,
            current.Palette.Id,
            JsonSerializer.Serialize(current.Palette),
            current.Source.Id,
            current.Source.SemanticHash(),
            plan,
            addition,
            allocator.NextSerial);
    }

    internal void ValidateCurrent(
        Timeline timeline,
        PlacerSettings settings,
        CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ValidateRow(timeline, row);
        geometry.Context.ValidateCurrent(timeline, false);
        geometry.Source.ValidateCurrent();

        var live = ManagedExpressionReader.Read(timeline, row.Target.Voice);
        if (!ManagedExpressionSafety.Same(existing, live))
            throw new InvalidOperationException("計画後に現在の関連表情が変わりました。配置していません。");
        ManagedExpressionSafety.ValidatePresetState(live.Bundle, token);

        var palette = settings.IntentPalettes.SingleOrDefault(x => x.Id == paletteId);
        var source = settings.TachiePresetSources.SingleOrDefault(x => x.Id == sourceId);
        if (palette == null ||
            JsonSerializer.Serialize(palette) != paletteSnapshot ||
            source == null ||
            source.SemanticHash() != sourceHash)
            throw new InvalidOperationException("計画後に表情Setまたは登録済み立ち絵プリセットSourceが変更されました。配置していません。");
    }

    internal int CommitWithinOpenRecord(
        Timeline timeline,
        PlacerSettings settings,
        CancellationToken token = default)
    {
        ValidateCurrent(timeline, settings, token);
        return Plan.CommitWithinOpenRecord(timeline);
    }

    private static void ValidateRow(Timeline timeline, AssignmentRow row)
    {
        var voice = row.Target.Voice;
        if (!timeline.Items.Contains(voice) ||
            voice.CharacterName != row.Target.Character ||
            voice.Frame != row.Target.Frame ||
            voice.Length != row.Target.Length ||
            voice.Layer != row.Target.Layer ||
            (voice.Serif ?? "") != row.Target.Serif)
            throw new InvalidOperationException(
                "対象音声が「表情をまとめて」を開いた時点から変更されています。メンテナンスから一覧を読み直してください。");
    }
}
