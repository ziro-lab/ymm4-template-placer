using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

public sealed partial class IntentPaletteDraft
{
    public IReadOnlyList<string> TargetTypeKeys => TypeChoices.Where(x => x.Selected).Select(x => x.Key).ToArray();
    public bool IsSingleOwner => TypeMatch == IntentTypeMatch.UniformType && TargetTypeKeys.Count == 1;
    public bool IsLegacyMultiType => !IsSingleOwner;
    public string? OwnerTypeKey => IsSingleOwner ? TargetTypeKeys[0] : null;
    public string OwnerTypeLabel => OwnerTypeKey is { } key ? KnownTypeLabel(key) : "複数種類";

    private string KnownTypeLabel(string key) =>
        TypeChoices.FirstOrDefault(x => x.Key == key)?.Name ?? "利用できない種類";
}

public sealed partial class IntentSettingsSession
{
    private string UniqueOwnedName(string stem, string ownerTypeKey)
    {
        var used = Palettes.Where(x => x.OwnerTypeKey == ownerTypeKey).Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        if (!used.Contains(stem)) return stem;
        for (var i = 2; ; i++)
        {
            var candidate = $"{stem} {i}";
            if (!used.Contains(candidate)) return candidate;
        }
    }

    private string UniqueLegacyName(string stem)
    {
        var used = Palettes.Where(x => x.IsLegacyMultiType).Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        if (!used.Contains(stem)) return stem;
        for (var i = 2; ; i++)
        {
            var candidate = $"{stem} {i}";
            if (!used.Contains(candidate)) return candidate;
        }
    }

    public void CreateSingleOwner(IReadOnlyList<IItem> selection)
    {
        if (selection.Count == 0)
            throw new InvalidOperationException("タイムラインで、このSetを使う対象アイテムを選択してから作成してください。");

        var types = selection.Select(x => IntentSelectionContext.TypeKey(x.GetType())).Distinct(StringComparer.Ordinal).ToArray();
        if (types.Length != 1)
            throw new InvalidOperationException("複数種類を選択中です。作成先のアイテム種類を上で選んでからSetを作成してください。");

        var owner = types[0];
        var names = selection.Select(x => ItemCharacters.Get(x)?.Name).Distinct(StringComparer.Ordinal).ToArray();
        var character = names.Length == 1 && !string.IsNullOrWhiteSpace(names[0]) ? names[0] : null;
        var context = new IntentTargetContext
        {
            ItemTypeKeys = [owner],
            TypeMatch = IntentTypeMatch.UniformType,
            MinimumCount = selection.Count,
            MaximumCount = selection.Count,
            CharacterName = character
        };
        var defaultName = character ?? "新しいセット";
        var relation = new IntentRelation();
        if (owner == IntentSelectionContext.TypeKey(typeof(VoiceItem)) && character != null)
            relation = relation with
            {
                Duration = IntentDuration.UntilRelated,
                Neighbor = IntentNeighbor.NextSameTypeAndCharacter,
                Fallback = IntentFallback.CurrentTargetEnd
            };

        SelectedPalette = AddDraft(new(
            Guid.NewGuid(),
            UniqueOwnedName(defaultName, owner),
            owner == IntentSelectionContext.TypeKey(typeof(VoiceItem)) ? "表情" : "演出",
            context,
            relation,
            []));
        MarkDirty();
    }

    public void CreateSingleOwnerForType(string ownerTypeKey)
    {
        if (!KnownTypes.ContainsKey(ownerTypeKey))
            throw new InvalidOperationException("作成先のアイテム種類を現在のYMM4で利用できません。");

        var target = new IntentTargetContext
        {
            ItemTypeKeys = [ownerTypeKey],
            TypeMatch = IntentTypeMatch.UniformType
        };
        var intent = ownerTypeKey == IntentSelectionContext.TypeKey(typeof(VoiceItem)) ? "表情" : "演出";
        SelectedPalette = AddDraft(new(
            Guid.NewGuid(),
            UniqueOwnedName("新しいセット", ownerTypeKey),
            intent,
            target,
            new(),
            []));
        MarkDirty();
    }

    public void DuplicateOwned()
    {
        if (IsGenericContext)
        {
            DuplicateGenericSet();
            return;
        }

        var sourceDraft = SelectedPalette ?? throw new InvalidOperationException("複製するSetを選んでください。");
        var source = sourceDraft.Build();
        var name = sourceDraft.OwnerTypeKey is { } owner
            ? UniqueOwnedName(source.Name, owner)
            : UniqueLegacyName(source.Name);
        SelectedPalette = AddDraft(IntentPaletteSettings.Copy(source) with
        {
            Id = Guid.NewGuid(),
            Name = name
        });
        MarkDirty();
        RefreshNavigation();
    }

    public void MoveOwned(int delta)
    {
        if (IsGenericContext)
        {
            MoveGenericSet(delta);
            return;
        }
        if (SelectedPalette == null || delta == 0) return;

        bool SameGroup(IntentPaletteDraft candidate) =>
            SelectedPalette.IsLegacyMultiType ? candidate.IsLegacyMultiType :
            candidate.OwnerTypeKey == SelectedPalette.OwnerTypeKey;

        var siblings = Palettes.Where(SameGroup).ToArray();
        var localIndex = Array.IndexOf(siblings, SelectedPalette);
        var localTarget = localIndex + Math.Sign(delta);
        if (localIndex < 0 || localTarget < 0 || localTarget >= siblings.Length) return;

        var globalFrom = Palettes.IndexOf(SelectedPalette);
        var globalTo = Palettes.IndexOf(siblings[localTarget]);
        Palettes.Move(globalFrom, globalTo);
    }

    public IntentPaletteDraft CopySelectedToOtherItem()
    {
        if (IsGenericContext)
            throw new InvalidOperationException("汎用Setは対象アイテム用Setへ直接コピーできません。");
        var destination = SelectedCopyDestination;
        if (destination?.IsRealItemType != true)
            throw new InvalidOperationException("コピー先のアイテム種類を選んでください。");

        var source = SelectedPalette?.Build() ?? throw new InvalidOperationException("コピーするSetを選んでください。");
        var owner = destination.TypeKeys.Single();
        var copy = IntentPaletteSettings.Copy(source) with
        {
            Id = Guid.NewGuid(),
            Name = UniqueOwnedName(source.Name, owner),
            Target = source.Target with
            {
                ItemTypeKeys = [owner],
                TypeMatch = IntentTypeMatch.UniformType
            }
        };

        var draft = AddDraft(copy);
        MarkDirty();

        SelectedItemContext = ItemContexts.Single(x => x.IsRealItemType && x.Key == owner);
        RefreshPaletteFilter(draft.Id);
        SelectedPalette = draft;
        RefreshCopyDestination();
        return draft;
    }
}
