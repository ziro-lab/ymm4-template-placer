using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

internal static class CommandBindingProbe
{
    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 3) return;
            var ymm4Dir = Path.GetFullPath(args[1]);
            var mainOutput = Path.GetFullPath(args[2]);
            var output = Path.Combine(Path.GetDirectoryName(mainOutput)!, "command-binding-probe.txt");

            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var p = Path.Combine(ymm4Dir, name.Name + ".dll");
                if (!File.Exists(p)) return null;
                try { return AssemblyLoadContext.Default.LoadFromAssemblyPath(p); } catch { return null; }
            };

            var sb = new StringBuilder();
            sb.AppendLine("=== PLAYBACK COMMAND BINDING NEIGHBORS ===");
            foreach (var path in Directory.EnumerateFiles(ymm4Dir, "*.dll").Concat(Directory.EnumerateFiles(ymm4Dir, "*.exe")))
            {
                Assembly asm;
                try { asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path); } catch { continue; }
                Type? t = null;
                try { t = asm.GetType("YukkuriMovieMaker.ViewModels.MainViewModel+<>c", false, false); } catch { }
                if (t is null) continue;
                sb.AppendLine("TYPE=" + t.AssemblyQualifiedName);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => m.Name.Contains("<CreateCommandBindings>b__135_", StringComparison.Ordinal))
                    .OrderBy(m => m.Name))
                {
                    var suffixText = m.Name.Split('_').LastOrDefault();
                    if (!int.TryParse(suffixText, out var suffix) || suffix < 205 || suffix > 216) continue;
                    sb.AppendLine($"METHOD {m.Name} RETURN={m.ReturnType.FullName}");
                    sb.AppendLine("  PARAMS=" + string.Join(" | ", m.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name)));
                    var body = m.GetMethodBody();
                    var il = body?.GetILAsByteArray() ?? Array.Empty<byte>();
                    sb.AppendLine("  ILHEX=" + Convert.ToHexString(il));
                    sb.AppendLine("  LOCALS=" + string.Join(" | ", body?.LocalVariables.Select(v => v.LocalType.FullName) ?? Array.Empty<string>()));
                    try
                    {
                        foreach (var c in m.GetCustomAttributesData()) sb.AppendLine("  ATTR=" + c.AttributeType.FullName);
                    }
                    catch { }
                }
                break;
            }
            File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
            Console.WriteLine(sb.ToString());
        }
        catch { }
    }
}
