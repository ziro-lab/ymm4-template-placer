// The fixture edits the actual host collection to simulate duplicate definitions entered through YMM4 settings.
// This adapter is compiled only into the isolated proof assembly, never the distribution DLL.
global using CharacterSettings = Ymm4TemplatePlacer.NativeCharacterRegistryAccess;
using System.Collections;
using System.Reflection;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;
internal sealed class NativeCharacterRegistryAccess
{
    internal static NativeCharacterRegistryAccess Default { get; } = new();
    internal IList Characters
    {
        get
        {
            var type = typeof(Character).Assembly.GetType("YukkuriMovieMaker.Settings.CharacterSettings", true)!;
            var registry = type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!.GetValue(null);
            return (IList)type.GetProperty("Characters", BindingFlags.Public | BindingFlags.Instance)!.GetValue(registry)!;
        }
    }
}
