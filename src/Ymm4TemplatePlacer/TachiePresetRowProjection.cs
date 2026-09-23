using System.Diagnostics;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

internal static partial class ExpressionPreparation
{
    private static ExpressionPreparedResult PreparePresetChoices(ExpressionHostSnapshot snapshot,
        IReadOnlyList<VoiceSnapshot> voices, string fingerprint, ExpressionAssociationIndex associations,
        Stopwatch watch, int threadId, CancellationToken token)
    {
        var capabilities = snapshot.PresetCapabilities.ToDictionary(x => x.CharacterIdentity, StringComparer.Ordinal);
        var registeredByCharacter = snapshot.RegisteredPresetCandidates
            .GroupBy(x => x.Character, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
        var choicesByCharacter = new Dictionary<string, IReadOnlyList<TemplateChoice>>(StringComparer.Ordinal);
        foreach (var name in voices.Select(x => x.Character).Distinct(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var capability = capabilities.GetValueOrDefault(name)?.Capability;
            var registered = registeredByCharacter.GetValueOrDefault(name) ?? [];
            var availableRegistered = new HashSet<RegisteredPresetExpressionSource>();
            foreach (var source in registered)
                if ((capability?.Candidates ?? []).Any(source.MatchesCandidate))
                    availableRegistered.Add(source);

            var hasCandidates = availableRegistered.Count > 0 || capability?.HasCandidates == true;
            var choices = new List<TemplateChoice> { new(null, hasCandidates ? "— 選択しない —" : "— 候補なし —") };
            foreach (var source in registered)
                choices.Add(TemplateChoice.Registered(source, availableRegistered.Contains(source)));
            foreach (var candidate in capability?.Candidates ?? [])
                choices.Add(TemplateChoice.Preset(candidate));
            choicesByCharacter.Add(name, choices.AsReadOnly());
        }

        var rows = new List<ExpressionPreparedRow>(voices.Count);
        foreach (var voice in voices)
        {
            token.ThrowIfCancellationRequested();
            var choices = choicesByCharacter[voice.Character];
            var characterCapability = capabilities.GetValueOrDefault(voice.Character);
            var notice = CapabilityNotice(characterCapability);
            var association = associations.Read(voice.Voice);
            TemplateChoice selected = choices[0];

            if (association.IsError)
            {
                selected = new(null, "⚠ 関連付けを確認", null, false) { IsInvalidAssociation = true };
                notice = association.Error!;
            }
            else
            {
                if (association.Descriptor is { Kind: ManagedExpressionSourceKind.Template })
                {
                    selected = new(null, "現在：テンプレート由来の表情") { IsCurrentOtherSource = true };
                    notice = "現在の表情はテンプレートから配置されています。表示切替だけでは変更しません。" +
                        (notice.Length == 0 ? "" : " " + notice);
                }
                else if (association.Descriptor is { Kind: ManagedExpressionSourceKind.TachiePreset, TachiePreset: { } currentPreset })
                {
                    selected = choices.SingleOrDefault(x => x.TachiePreset is { } candidate &&
                        TachiePresetAssociationTag.CapabilityIdentity(candidate.Fingerprint) == currentPreset.CapabilityHash &&
                        TachiePresetAssociationTag.CandidateIdentity(candidate) == currentPreset.CandidateHash)
                        ?? new TemplateChoice(null,
                            "⚠ 現在：立ち絵プリセット（候補が消えたか立ち絵設定が変わりました）",
                            null, false);
                    notice = selected.IsAvailable
                        ? "現在の表情は立ち絵プリセットから配置されています。"
                        : "現在の立ち絵プリセットを候補から一意に再確認できません。候補が消えたか、立ち絵設定またはプラグイン構成が変わった可能性があります。［一覧を読み直す］後に候補を確認してください。";
                }

                // Preserve an uncommitted inspection choice only when there is no managed
                // Timeline truth to show. A live Template/Preset association always wins.
                if (association.Descriptor == null &&
                    snapshot.PreviousPresetChoices.TryGetValue(voice.Voice, out var previous))
                {
                    var current = choices.SingleOrDefault(x => x.TachiePreset == previous);
                    if (current != null) selected = current;
                    else
                    {
                        selected = TemplateChoice.Preset(previous) with
                        {
                            Label = "⚠ " + previous.Label + "（立ち絵設定が変わった可能性・選び直してください）",
                            IsAvailable = false
                        };
                        notice = "以前選んだ立ち絵プリセットを現在の候補から再確認できません。立ち絵設定またはプラグイン構成が変わった可能性があります。同名候補へ自動で置き換えず、候補を選び直してください。";
                    }
                }
            }

            rows.Add(new(voice, choices, selected, null, true, notice));
        }
        watch.Stop();
        return new(snapshot.Generation, snapshot.Timeline, fingerprint, snapshot.Items, rows, false,
            associations.ParsedItemCount, choicesByCharacter.Count, watch.Elapsed, threadId) { SourceMode = snapshot.SourceMode };
    }

    private static string CapabilityNotice(TachiePresetCharacterCapability? value)
    {
        if (value == null)
            return "このキャラクターの立ち絵プリセット情報を確認できません。［一覧を読み直す］で再確認してください。";
        if (value.Capability?.HasCandidates == true) return "";
        var reason = string.IsNullOrWhiteSpace(value.UnavailableReason)
            ? "利用できる候補を確認できませんでした。"
            : value.UnavailableReason!;
        return value.Capability == null
            ? "このキャラクターの立ち絵プリセット確認で問題が発生しました。この行だけ利用できません。理由: " + reason
            : "このキャラクターでは立ち絵プリセットを利用できません。理由: " + reason;
    }
}
