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
        var choicesByCharacter = new Dictionary<string, IReadOnlyList<TemplateChoice>>(StringComparer.Ordinal);
        foreach (var name in voices.Select(x => x.Character).Distinct(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var capability = capabilities.GetValueOrDefault(name)?.Capability;
            var choices = new List<TemplateChoice> { new(null, capability?.HasCandidates == true ? "— 選択しない —" : "— 候補なし —") };
            foreach (var candidate in capability?.Candidates ?? []) choices.Add(TemplateChoice.Preset(candidate));
            choicesByCharacter.Add(name, choices.AsReadOnly());
        }
        var rows = new List<ExpressionPreparedRow>(voices.Count);
        foreach (var voice in voices)
        {
            token.ThrowIfCancellationRequested();
            var choices = choicesByCharacter[voice.Character];
            var capability = capabilities.GetValueOrDefault(voice.Character);
            var notice = capability?.UnavailableReason ?? (capability?.Capability?.HasCandidates == true ? "" : "このキャラクターの立ち絵プリセットを利用できません。");
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
                    notice = "現在の表情はテンプレートから配置されています。表示切替では変更しません。" + (notice.Length == 0 ? "" : " " + notice);
                }
                else if (association.Descriptor is { Kind: ManagedExpressionSourceKind.TachiePreset, TachiePreset: { } currentPreset })
                {
                    selected = choices.SingleOrDefault(x => x.TachiePreset is { } candidate &&
                        TachiePresetAssociationTag.CapabilityIdentity(candidate.Fingerprint) == currentPreset.CapabilityHash &&
                        TachiePresetAssociationTag.CandidateIdentity(candidate) == currentPreset.CandidateHash)
                        ?? new TemplateChoice(null, "⚠ 現在：立ち絵プリセット（候補・立ち絵設定を確認）", null, false);
                    notice = selected.IsAvailable
                        ? "現在の表情は立ち絵プリセットから配置されています。"
                        : "現在の立ち絵プリセットを候補から一意に再確認できません。表示切替では変更しません。";
                }
                if (snapshot.PreviousPresetChoices.TryGetValue(voice.Voice, out var previous))
                    selected = choices.SingleOrDefault(x => x.TachiePreset == previous) ??
                        (TemplateChoice.Preset(previous) with { Label = "⚠ " + previous.Label + "（候補・立ち絵設定を確認）", IsAvailable = false });
            }
            rows.Add(new(voice, choices, selected, null, true, notice));
        }
        watch.Stop();
        return new(snapshot.Generation, snapshot.Timeline, fingerprint, snapshot.Items, rows, false,
            associations.ParsedItemCount, choicesByCharacter.Count, watch.Elapsed, threadId) { SourceMode = snapshot.SourceMode };
    }
}
