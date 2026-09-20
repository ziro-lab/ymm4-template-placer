using System.Reflection;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

/// <summary>
/// Narrow read-only host compatibility boundary. YMM4 4.55.1.1 makes CharacterSettings internal,
/// although Default/Characters are public properties. Never reflect a Timeline/ViewModel, mutate the registry,
/// resolve arbitrary property paths, or fall back to template-object identity when this contract is unavailable.
/// </summary>
public static class IntentCharacterRegistry
{
    private static readonly Type? RegistryType = typeof(Character).Assembly.GetType("YukkuriMovieMaker.Settings.CharacterSettings");
    private static readonly PropertyInfo? DefaultProperty = RegistryType?.GetProperty("Default", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
    private static readonly PropertyInfo? CharactersProperty = RegistryType?.GetProperty("Characters", BindingFlags.Public | BindingFlags.Instance);
    public static IReadOnlyList<Character> Read()
    {
        if (DefaultProperty?.GetMethod?.IsPublic != true || CharactersProperty?.GetMethod?.IsPublic != true ||
            DefaultProperty.GetValue(null) is not { } registry || CharactersProperty.GetValue(registry) is not IEnumerable<Character> characters)
            throw new InvalidOperationException("このYMM4ではキャラクター定義の重複を確認できません。配置せず停止しました。対応するYMM4版とプラグインを確認してください。");
        return characters.ToArray();
    }
    public static void RequireUnambiguous(IEnumerable<string> names)
    {
        var needed = names.Distinct(StringComparer.Ordinal).ToArray(); if (needed.Length == 0) return;
        var registered = Read();
        foreach (var name in needed)
            if (registered.Where(x => x.Name == name).Distinct().Take(2).Count() > 1)
                throw new InvalidOperationException($"YMM4に同名キャラクター「{name}」が複数登録されています。一意にしてから配置してください。");
    }
}
