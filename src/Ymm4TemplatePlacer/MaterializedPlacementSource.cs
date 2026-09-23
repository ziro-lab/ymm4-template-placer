using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

/// <summary>
/// One-operation fresh normalized content produced by a concrete Placement Source.
/// The Items belong only to the pending placement and may be moved by geometry planning.
/// </summary>
internal sealed class MaterializedPlacementSource
{
    private readonly Action validateCurrent;

    public Guid SourceId { get; }
    public PlacementSourceKind Kind { get; }
    public IReadOnlyList<IItem> Items { get; }
    public int Span { get; }
    public string? CharacterName { get; }
    public string SemanticHash { get; }

    internal MaterializedPlacementSource(
        Guid sourceId,
        PlacementSourceKind kind,
        IReadOnlyList<IItem> items,
        int span,
        string? characterName,
        string semanticHash,
        Action validateCurrent)
    {
        if (sourceId == Guid.Empty) throw new InvalidOperationException("配置SourceのIDがありません。");
        if (!Enum.IsDefined(kind)) throw new InvalidOperationException("配置Sourceの種類が不正です。");
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(validateCurrent);
        var snapshot = items.ToArray();
        if (snapshot.Length is < 1 or > 2048 || snapshot.Any(x => x == null) ||
            snapshot.Distinct(ReferenceEqualityComparer.Instance).Count() != snapshot.Length)
            throw new InvalidOperationException("配置Sourceの生成アイテムが空・重複または上限超過です。");
        if (snapshot.Min(x => x.Frame) != 0 || snapshot.Min(x => x.Layer) != 0)
            throw new InvalidOperationException("配置Sourceの生成アイテムは開始Frame/Layerを0基準へ正規化してください。");
        foreach (var item in snapshot)
        {
            PlacementMath.ValidateSpan(item.Frame, item.Length);
            if (item.Layer < 0) throw new InvalidOperationException("配置Sourceのレイヤーは0以上にしてください。");
        }
        var actualSpan = checked((int)snapshot.Max(x => (long)x.Frame + x.Length));
        if (span != actualSpan)
            throw new InvalidOperationException("配置Sourceの長さと生成アイテムの範囲が一致しません。");
        var names = snapshot.Select(ItemCharacters.Name).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
        if (characterName != null && names.Any(x => x != characterName))
            throw new InvalidOperationException("配置Sourceのキャラクター名と生成アイテムが一致しません。");
        if (semanticHash.Length != 64 || semanticHash.Any(x => !char.IsAsciiHexDigitLower(x)))
            throw new InvalidOperationException("配置Sourceのsemantic hashがcanonical SHA-256ではありません。");

        SourceId = sourceId;
        Kind = kind;
        Items = Array.AsReadOnly(snapshot);
        Span = span;
        CharacterName = characterName;
        SemanticHash = semanticHash;
        this.validateCurrent = validateCurrent;
    }

    public void ValidateCurrent() => validateCurrent();

    public static MaterializedPlacementSource FromTemplate(TemplateBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        bundle.ValidateCurrent();
        var items = bundle.CloneNormalized();
        return new(
            bundle.Entry.Id,
            PlacementSourceKind.Template,
            items,
            bundle.Span,
            bundle.CharacterName,
            IntentAssociationTag.Hash(bundle),
            bundle.ValidateCurrent);
    }
}
