using System.Text.Json;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

// Metadata is a strict locator, not an assertion of host-provided identity.
public sealed record TemplateLocator(string Name, string PathJson, Guid SceneId)
{
    public static TemplateLocator Capture(ItemTemplate template) =>
        new(template.Name, JsonSerializer.Serialize(template.Path), template.SceneId);
    public bool Matches(ItemTemplate template) => this == Capture(template);
}
public sealed record LibraryEntry(Guid Id, TemplateLocator Source, string DisplayName, string? CharacterName);
public enum TemplateReferenceState { Resolved, Missing, Ambiguous, Unsupported, CharacterMismatch }
public sealed record TemplateResolution(TemplateReferenceState State, ItemTemplate? Template, IItem? Item)
{
    public string Message => State switch
    {
        TemplateReferenceState.Resolved => "利用可能",
        TemplateReferenceState.Missing => "⚠ 元テンプレートが見つかりません。再リンクしてください。",
        TemplateReferenceState.Ambiguous => "⚠ テンプレートを一意に特定できません。YMM4側の名前・配置を区別して再リンクしてください。",
        TemplateReferenceState.CharacterMismatch => "⚠ 登録キャラクターと元テンプレートのキャラクターが違います。再リンクまたはキャラクター設定を確認してください。",
        _ => "⚠ アイテムを1つだけ含むテンプレートに対応しています。"
    };
}
public static partial class TemplateResolver
{
    public static TemplateResolution Resolve(LibraryEntry entry)
    {
        var matches = ItemSettings.Default.Templates.Where(entry.Source.Matches).Take(2).ToArray();
        if (matches.Length == 0) return new(TemplateReferenceState.Missing, null, null);
        if (matches.Length != 1) return new(TemplateReferenceState.Ambiguous, null, null);
        var items = matches[0].Items.ToArray();
        if (items.Length != 1) return new(TemplateReferenceState.Unsupported, matches[0], null);
        var character = ItemCharacters.Get(items[0]);
        if (entry.CharacterName != null && character != null && !string.Equals(character.Name, entry.CharacterName, StringComparison.Ordinal))
            return new(TemplateReferenceState.CharacterMismatch, matches[0], null);
        return new(TemplateReferenceState.Resolved, matches[0], items[0]);
    }
    public static LibraryEntry Reference(ItemTemplate template, string displayName, string? characterName, Guid? id = null)
    {
        if (!ItemSettings.Default.Templates.Contains(template)) throw new InvalidOperationException("元テンプレートが削除されています。一覧を更新してください。");
        var name = displayName.Trim();
        if (name.Length == 0 || name.Length > 256) throw new InvalidOperationException("表示名は1〜256文字で入力してください。");
        var entry = new LibraryEntry(id ?? Guid.NewGuid(), TemplateLocator.Capture(template), name, string.IsNullOrEmpty(characterName) ? null : characterName);
        var resolution = ResolveBundle(entry);
        if (resolution.State != TemplateReferenceState.Resolved) throw new InvalidOperationException(resolution.Message);
        return entry;
    }
    public static IItem Clone(LibraryEntry entry)
    {
        var resolved = Resolve(entry);
        if (resolved.State != TemplateReferenceState.Resolved || resolved.Item == null) throw new InvalidOperationException(resolved.Message);
        var source = resolved.Item;
        var clone = source.GetClone();
        if (clone == null || ReferenceEquals(clone, source) || clone.GetType() != source.GetType() || !Equals(ItemCharacters.Get(source), ItemCharacters.Get(clone)))
            throw new InvalidOperationException("テンプレートを独立したアイテムとして複製できませんでした。");
        clone.Group = 0;
        return clone;
    }
}
public static class ItemCharacters
{
    public static Character? Get(IItem item) => item switch
    {
        VoiceItem voice => voice.Character,
        TachieFaceItem face => face.Character,
        TachieItem tachie => tachie.Character,
        _ => null
    };
    public static Character[] Read(Timeline? timeline) =>
        (timeline?.Items.Select(Get) ?? Enumerable.Empty<Character?>())
        .Concat(ItemSettings.Default.Templates.SelectMany(x => x.Items).Select(Get))
        .OfType<Character>().Distinct().ToArray();
    public static Character? ResolveUnique(Timeline? timeline, string name)
    {
        var matches = Read(timeline).Where(x => string.Equals(x.Name, name, StringComparison.Ordinal)).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
public sealed record LibraryEntryView(LibraryEntry Entry)
{
    public Guid Id => Entry.Id;
    public string DisplayName => Entry.DisplayName;
    public string SourceName => Entry.Source.Name;
    public string CharacterName => Entry.CharacterName ?? "指定なし";
    public string Status
    {
        get { var result = TemplateResolver.ResolveBundle(Entry); return result.State == TemplateReferenceState.Resolved ? "" : result.Message; }
    }
}
public sealed record CharacterOption(string? Name, string Label);
