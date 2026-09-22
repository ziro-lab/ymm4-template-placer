using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal enum ManagedExpressionSourceKind
{
    Template,
    TachiePreset
}

internal sealed record ManagedExpressionSourceDescriptor
{
    public ManagedExpressionSourceKind Kind { get; }
    public IntentAssociationTag? Template { get; }
    public TachiePresetAssociationTag? TachiePreset { get; }

    private ManagedExpressionSourceDescriptor(
        ManagedExpressionSourceKind kind,
        IntentAssociationTag? template,
        TachiePresetAssociationTag? tachiePreset)
    {
        Kind = kind;
        Template = template;
        TachiePreset = tachiePreset;
    }

    public Guid Group => Kind == ManagedExpressionSourceKind.Template ? Template!.Group : TachiePreset!.Group;
    public int Index => Kind == ManagedExpressionSourceKind.Template ? Template!.Index : TachiePreset!.Index;
    public int Count => Kind == ManagedExpressionSourceKind.Template ? Template!.Count : TachiePreset!.Count;

    public static ManagedExpressionSourceDescriptor FromTemplate(IntentAssociationTag value) =>
        new(ManagedExpressionSourceKind.Template, value ?? throw new ArgumentNullException(nameof(value)), null);

    public static ManagedExpressionSourceDescriptor FromTachiePreset(TachiePresetAssociationTag value) =>
        new(ManagedExpressionSourceKind.TachiePreset, null, value ?? throw new ArgumentNullException(nameof(value)));

    public static AssociationTagState Read(string? remark, out ManagedExpressionSourceDescriptor? descriptor)
    {
        descriptor = null;
        var templateState = IntentAssociationTag.Read(remark, out var template);
        var presetState = TachiePresetAssociationTag.Read(remark, out var preset);
        if (templateState == AssociationTagState.Invalid || presetState == AssociationTagState.Invalid)
            return AssociationTagState.Invalid;
        if (templateState == AssociationTagState.Valid && presetState == AssociationTagState.Valid)
            return AssociationTagState.Invalid;
        if (templateState == AssociationTagState.Valid)
        {
            descriptor = FromTemplate(template!);
            return AssociationTagState.Valid;
        }
        if (presetState == AssociationTagState.Valid)
        {
            descriptor = FromTachiePreset(preset!);
            return AssociationTagState.Valid;
        }
        return AssociationTagState.None;
    }

    public bool SameGroup(ManagedExpressionSourceDescriptor other)
    {
        if (Kind != other.Kind) return false;
        return Kind == ManagedExpressionSourceKind.Template
            ? Template!.SameGroup(other.Template!)
            : TachiePreset!.SameGroup(other.TachiePreset!);
    }
}

internal sealed record ManagedExpressionBundle(
    long Serial,
    ManagedExpressionSourceDescriptor Descriptor,
    IReadOnlyList<IItem> Members);

internal sealed record ManagedExpressionAssociation(long? Serial, ManagedExpressionBundle? Bundle);

internal static class ManagedExpressionReader
{
    public static ManagedExpressionAssociation Read(Timeline timeline, VoiceItem voice)
    {
        if (!timeline.Items.Contains(voice))
            throw new InvalidOperationException("対象音声が現在のシーンにありません。メンテナンスから一覧を読み直してください。");
        var voiceState = AssociationTag.Voice(voice.Remark, out var serial);
        if (voiceState == AssociationTagState.Invalid)
            throw new InvalidOperationException("対象音声の関連付けタグが不正または重複しています。推測して変更しません。");
        if (voiceState == AssociationTagState.None) return new(null, null);

        var targets = timeline.Items.OfType<VoiceItem>().Where(x => x.CharacterName == voice.CharacterName &&
            AssociationTag.Voice(x.Remark, out var id) == AssociationTagState.Valid && id == serial).Take(2).ToArray();
        if (targets.Length != 1 || !ReferenceEquals(targets[0], voice))
            throw new InvalidOperationException("関連付けIDとキャラクター名が一致する音声を一意に特定できません。推測して変更しません。");

        var related = new List<IItem>();
        foreach (var item in timeline.Items)
            if (AssociationTag.Source(item.Remark, out var source) == AssociationTagState.Valid && source!.Serial == serial)
                related.Add(item);
        if (related.Count == 0) return new(serial, null);

        var tagged = new List<(IItem Item, ManagedExpressionSourceDescriptor Descriptor)>();
        foreach (var item in related)
        {
            var state = ManagedExpressionSourceDescriptor.Read(item.Remark, out var descriptor);
            if (state != AssociationTagState.Valid || descriptor == null)
                throw new InvalidOperationException("対象音声のPlugin-managed Bundleに欠落または混在した関連付け情報があります。推測して変更しません。");
            tagged.Add((item, descriptor));
        }

        var ordered = tagged.OrderBy(x => x.Descriptor.Index).ToArray();
        var bundleDescriptor = ordered[0].Descriptor;
        if (ordered.Length != bundleDescriptor.Count ||
            ordered.Any(x => !bundleDescriptor.SameGroup(x.Descriptor)) ||
            !ordered.Select(x => x.Descriptor.Index).SequenceEqual(Enumerable.Range(0, bundleDescriptor.Count)))
            throw new InvalidOperationException("対象音声のPlugin-managed Bundleに欠落・重複・コピーまたは設定不一致があります。");
        if (ordered.Any(x => x.Item.Group != 0))
            throw new InvalidOperationException("対象のPlugin-managed BundleはYMM4側でグループ化されています。解除してから表情を変更してください。");
        if (ordered.Any(x => !HasPlacementMarker(x.Item.Remark)))
            throw new InvalidOperationException("対象音声の関連アイテムをTemplate Placer生成物として確認できません。推測して変更しません。");

        var sameGroup = timeline.Items.Where(item =>
            ManagedExpressionSourceDescriptor.Read(item.Remark, out var descriptor) == AssociationTagState.Valid &&
            descriptor!.Group == bundleDescriptor.Group).ToArray();
        if (sameGroup.Length != ordered.Length || sameGroup.Any(x => ordered.All(y => !ReferenceEquals(y.Item, x))))
            throw new InvalidOperationException("同じBundle IDを持つコピーまたは別アイテムがあります。推測して変更しません。");

        return new(serial, new(serial, bundleDescriptor, ordered.Select(x => x.Item).ToArray()));
    }

    private static bool HasPlacementMarker(string? remark) =>
        (remark ?? "").Split('\n').Any(line => line.TrimEnd('\r') == PlacementEngine.Marker);
}


internal static class ManagedExpressionSafety
{
    internal static bool Same(ManagedExpressionAssociation left, ManagedExpressionAssociation right)
    {
        if (left.Serial != right.Serial) return false;
        if (left.Bundle == null || right.Bundle == null)
            return left.Bundle == null && right.Bundle == null;
        if (left.Bundle.Descriptor != right.Bundle.Descriptor ||
            left.Bundle.Members.Count != right.Bundle.Members.Count)
            return false;
        return left.Bundle.Members.Select((item, i) =>
            ReferenceEquals(item, right.Bundle.Members[i])).All(x => x);
    }

    internal static void ValidatePresetState(ManagedExpressionBundle? bundle, CancellationToken token = default)
    {
        if (bundle?.Descriptor is not
            { Kind: ManagedExpressionSourceKind.TachiePreset, TachiePreset: { } descriptor }) return;
        if (bundle.Members.Count != 1 ||
            bundle.Members[0] is not TachieFaceItem face ||
            face.TachieFaceParameter == null)
            throw new InvalidOperationException(
                "現在の立ち絵プリセット表情を安全に一意確認できません。変更していません。");
        var current = TachiePresetPublicState.TryHash(face.TachieFaceParameter, token);
        if (current == null || current != descriptor.StateHash)
            throw new InvalidOperationException(
                "現在の立ち絵プリセット表情は配置後に変更されています。自動置換せず停止しました。");
    }
}
