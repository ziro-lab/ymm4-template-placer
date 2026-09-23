using System.Windows;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal sealed record TachiePresetRegistrationResult(
    Guid SourceId,
    Guid PaletteId,
    bool SourceCreated,
    bool SetCreated,
    bool MembershipAdded);

public sealed partial class PlacerViewModel
{
    public ActionCommand RegisterTachiePresetSourceCommand { get; private set; } = null!;

    private void InitializeTachiePresetSourceRegistration()
    {
        RegisterTachiePresetSourceCommand = new ActionCommand(
            x => x is AssignmentRow row && CanRegisterTachiePresetSource(row),
            x => Guard(() => RegisterTachiePresetSourceFromUi((AssignmentRow)x!)));
        OnPropertyChanged(nameof(RegisterTachiePresetSourceCommand));
    }

    private bool CanRegisterTachiePresetSource(AssignmentRow row) =>
        settingsAvailable &&
        IsTachiePresetExpressionSource &&
        Rows.Contains(row) &&
        row.CanRegisterSelectedPreset &&
        row.SelectedChoice.TachiePreset != null &&
        IntentSettings?.HasChanges != true &&
        timeline != null &&
        timeline.Items.Contains(row.Target.Voice);

    private void RegisterTachiePresetSourceFromUi(AssignmentRow row)
    {
        if (!CanRegisterTachiePresetSource(row))
            throw new InvalidOperationException(
                IntentSettings?.HasChanges == true
                    ? "設定に未保存の変更があります。保存または破棄してから表情プリセットを登録してください。"
                    : "現在の表情プリセットをSetへ登録できません。一覧を読み直して選び直してください。");

        var targets = ApplicablePresetRegistrationSets(settings, row);
        Guid? targetId = null;
        if (targets.Count == 1)
        {
            targetId = targets[0].Id;
        }
        else if (targets.Count > 1)
        {
            var labels = IntentTileAppearance.Distinguish(targets.Select(x => x.Name).ToArray());
            var choices = targets.Select((x, i) =>
                new TachiePresetRegistrationTarget(x.Id, labels[i])).ToArray();
            var dialog = new TachiePresetSetPickerDialog(choices);
            if (Application.Current?.MainWindow is { IsVisible: true } owner)
                dialog.Owner = owner;
            if (dialog.ShowDialog() != true || dialog.SelectedPaletteId == null)
                return;
            targetId = dialog.SelectedPaletteId;
        }

        var result = RegisterTachiePresetSource(row, targetId);
        HasError = false;
        Status = result.MembershipAdded
            ? result.SetCreated
                ? $"「{row.SelectedChoice.DisplayName}」を登録し、{row.Character}の表情Setを作成しました。通常の配置タイルからも使えます。"
                : $"「{row.SelectedChoice.DisplayName}」をSetへ登録しました。通常の配置タイルからも使えます。"
            : $"「{row.SelectedChoice.DisplayName}」はこのSetに登録済みです。";
    }

    internal TachiePresetRegistrationResult RegisterTachiePresetSource(
        AssignmentRow row,
        Guid? targetPaletteId = null)
    {
        if (!settingsAvailable)
            throw new InvalidOperationException("設定を保存できないため表情プリセットを登録しません。");
        if (IntentSettings?.HasChanges == true)
            throw new InvalidOperationException("設定に未保存の変更があります。保存または破棄してから登録してください。");

        var current = RequireTimeline();
        var voice = row.Target.Voice;
        if (!Rows.Contains(row) ||
            !current.Items.Contains(voice) ||
            voice.Character == null ||
            voice.CharacterName != row.Target.Character ||
            voice.Frame != row.Target.Frame ||
            voice.Length != row.Target.Length ||
            voice.Layer != row.Target.Layer ||
            (voice.Serif ?? "") != row.Target.Serif)
            throw new InvalidOperationException("対象音声が一覧作成後に変更されています。一覧を読み直してください。");

        var candidate = row.SelectedChoice.TachiePreset
            ?? throw new InvalidOperationException("登録する未登録の表情プリセットを選んでください。");
        if (!row.SelectedChoice.IsAvailable)
            throw new InvalidOperationException("現在利用できない表情プリセットは登録できません。");

        CloseExpressionTrialSession();
        var target = PresetTargetResolver(voice.Character);
        if (target.Fingerprint != candidate.Fingerprint || !target.IsCurrent(CancellationToken.None))
            throw new InvalidOperationException("現在のキャラクター設定と候補が一致しません。一覧を読み直してください。");

        var prototype = TachiePresetSourceEntry.From(target, candidate, candidate.Label);
        var semanticHash = prototype.SemanticHash();
        var next = PlacerSettingsStore.Copy(settings);

        var semanticMatches = next.TachiePresetSources
            .Where(x => x.SemanticHash() == semanticHash)
            .Take(2)
            .ToArray();
        if (semanticMatches.Length > 1)
            throw new InvalidOperationException("同じ表情プリセットSourceの登録が複数あります。設定を確認してください。");

        var sourceCreated = semanticMatches.Length == 0;
        var source = semanticMatches.SingleOrDefault() ?? prototype;
        if (sourceCreated) next.TachiePresetSources.Add(source);

        var applicable = ApplicablePresetRegistrationSets(next, row);
        IntentPalette palette;
        var setCreated = false;
        if (targetPaletteId.HasValue)
        {
            palette = applicable.SingleOrDefault(x => x.Id == targetPaletteId.Value)
                ?? throw new InvalidOperationException("選択した登録先Setが現在のキャラクターに適用できません。");
        }
        else if (applicable.Count == 1)
        {
            palette = applicable[0];
        }
        else if (applicable.Count == 0)
        {
            palette = new IntentPalette(
                Guid.NewGuid(),
                row.Character,
                "表情",
                new IntentTargetContext
                {
                    ItemTypeKeys = [IntentSelectionContext.TypeKey(typeof(VoiceItem))],
                    CharacterName = row.Character
                },
                new IntentRelation
                {
                    Duration = IntentDuration.UntilRelated,
                    Neighbor = IntentNeighbor.NextSameTypeAndCharacter,
                    Fallback = IntentFallback.CurrentTargetEnd
                },
                [])
            { ExpressionCandidates = true };
            next.IntentPalettes.Add(palette);
            setCreated = true;
        }
        else
        {
            throw new InvalidOperationException("登録先の表情Setが複数あります。登録先を選んでください。");
        }

        var paletteIndex = next.IntentPalettes.FindIndex(x => x.Id == palette.Id);
        if (paletteIndex < 0)
            throw new InvalidOperationException("登録先Setを設定から再確認できません。");

        var membershipAdded = !palette.Entries.Any(x => x.SourceId == source.Id);
        if (membershipAdded)
        {
            palette = palette with
            {
                Entries = [.. palette.Entries, new IntentEntry(source.Id)
                {
                    DisplayAlias = candidate.Label
                }]
            };
            next.IntentPalettes[paletteIndex] = palette;
        }

        if (!sourceCreated && !setCreated && !membershipAdded)
            return new(source.Id, palette.Id, false, false, false);

        PlacerSettingsStore.Validate(next);
        settingsStore.Save(next);
        settings = next;
        RefreshV04();
        RefreshIntentWorkspace();
        RefreshExpressionVocabulary();
        ResetIntentSettings();
        UpdateCommands();
        RegisterTachiePresetSourceCommand?.RaiseCanExecuteChanged();
        return new(source.Id, palette.Id, sourceCreated, setCreated, membershipAdded);
    }

    private static IReadOnlyList<IntentPalette> ApplicablePresetRegistrationSets(
        PlacerSettings source,
        AssignmentRow row)
    {
        var voiceType = IntentSelectionContext.TypeKey(typeof(VoiceItem));
        return source.IntentPalettes.Where(x =>
            x.ExpressionCandidates &&
            x.Target.TypeMatch == IntentTypeMatch.UniformType &&
            x.Target.MinimumCount <= 1 &&
            x.Target.MaximumCount >= 1 &&
            x.Target.CharacterName == row.Character &&
            x.Target.ItemTypeKeys.Contains(voiceType, StringComparer.Ordinal))
            .ToArray();
    }
}
