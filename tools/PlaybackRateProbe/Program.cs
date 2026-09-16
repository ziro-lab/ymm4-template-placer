using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Text;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: PlaybackRateProbe <YMM4 dir> <output file>");
    return 2;
}

var ymm4Dir = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var candidate = Path.Combine(ymm4Dir, name.Name + ".dll");
    if (File.Exists(candidate))
    {
        try { return AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate); } catch { }
    }
    return null;
};

var assemblies = new List<Assembly>();
foreach (var path in Directory.EnumerateFiles(ymm4Dir, "*.dll").Concat(Directory.EnumerateFiles(ymm4Dir, "*.exe")))
{
    try
    {
        var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        if (!assemblies.Any(a => string.Equals(a.FullName, asm.FullName, StringComparison.Ordinal))) assemblies.Add(asm);
    }
    catch { }
}

static IEnumerable<Type> SafeTypes(Assembly a)
{
    try { return a.GetTypes(); }
    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    catch { return Array.Empty<Type>(); }
}

var allTypes = assemblies.SelectMany(SafeTypes).ToArray();
var ymmTypes = allTypes.Where(t => t.FullName?.StartsWith("YukkuriMovieMaker.", StringComparison.Ordinal) == true).ToArray();
var sb = new StringBuilder();
sb.AppendLine($"YMM4_DIR={ymm4Dir}");
sb.AppendLine($"ASSEMBLIES={assemblies.Count}");
sb.AppendLine($"YMM_TYPES={ymmTypes.Length}");

var single = new OpCode[0x100];
var multi = new OpCode[0x100];
foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
{
    if (f.GetValue(null) is not OpCode op) continue;
    var v = unchecked((ushort)op.Value);
    if (v < 0x100) single[v] = op;
    else if ((v & 0xff00) == 0xfe00) multi[v & 0xff] = op;
}

string ResolveToken(MethodBase method, int token, OperandType kind)
{
    try
    {
        var ta = method.DeclaringType?.GetGenericArguments();
        var ma = method.IsGenericMethod ? method.GetGenericArguments() : null;
        return kind switch
        {
            OperandType.InlineMethod => method.Module.ResolveMethod(token, ta, ma)?.ToString() ?? $"0x{token:X8}",
            OperandType.InlineField => method.Module.ResolveField(token, ta, ma)?.ToString() ?? $"0x{token:X8}",
            OperandType.InlineType => method.Module.ResolveType(token, ta, ma)?.ToString() ?? $"0x{token:X8}",
            OperandType.InlineTok => method.Module.ResolveMember(token, ta, ma)?.ToString() ?? $"0x{token:X8}",
            OperandType.InlineString => "\"" + method.Module.ResolveString(token).Replace("\r", "\\r").Replace("\n", "\\n") + "\"",
            _ => $"0x{token:X8}"
        };
    }
    catch { return $"0x{token:X8}"; }
}

IEnumerable<string> Disassemble(MethodBase method)
{
    MethodBody? body;
    try { body = method.GetMethodBody(); } catch { yield break; }
    var il = body?.GetILAsByteArray();
    if (il is null) yield break;
    var p = 0;
    while (p < il.Length)
    {
        var offset = p;
        OpCode op;
        var b = il[p++];
        if (b == 0xfe) op = multi[il[p++]]; else op = single[b];
        string operand = "";
        try
        {
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineI: operand = ((sbyte)il[p]).ToString(); p += 1; break;
                case OperandType.InlineI: operand = BitConverter.ToInt32(il, p).ToString(); p += 4; break;
                case OperandType.InlineI8: operand = BitConverter.ToInt64(il, p).ToString(); p += 8; break;
                case OperandType.ShortInlineR: operand = BitConverter.ToSingle(il, p).ToString("R"); p += 4; break;
                case OperandType.InlineR: operand = BitConverter.ToDouble(il, p).ToString("R"); p += 8; break;
                case OperandType.ShortInlineVar: operand = il[p].ToString(); p += 1; break;
                case OperandType.InlineVar: operand = BitConverter.ToUInt16(il, p).ToString(); p += 2; break;
                case OperandType.ShortInlineBrTarget:
                    { var d = (sbyte)il[p]; p += 1; operand = $"IL_{p + d:X4}"; break; }
                case OperandType.InlineBrTarget:
                    { var d = BitConverter.ToInt32(il, p); p += 4; operand = $"IL_{p + d:X4}"; break; }
                case OperandType.InlineSwitch:
                    {
                        var n = BitConverter.ToInt32(il, p); p += 4;
                        var basePos = p + n * 4;
                        var targets = new string[n];
                        for (var i = 0; i < n; i++) targets[i] = $"IL_{basePos + BitConverter.ToInt32(il, p + i * 4):X4}";
                        p = basePos; operand = string.Join(", ", targets); break;
                    }
                case OperandType.InlineString:
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                case OperandType.InlineSig:
                    { var token = BitConverter.ToInt32(il, p); p += 4; operand = ResolveToken(method, token, op.OperandType); break; }
                default: operand = "<unsupported>"; break;
            }
        }
        catch (Exception ex) { operand = "<decode-error:" + ex.GetType().Name + ">"; }
        yield return $"IL_{offset:X4}: {op.Name} {operand}".TrimEnd();
    }
}

IEnumerable<MethodBase> Methods(Type t)
{
    try
    {
        return t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Cast<MethodBase>()
            .Concat(t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
    }
    catch { return Array.Empty<MethodBase>(); }
}

sb.AppendLine("\n=== YMMSETTINGS PLAYBACK MEMBERS / ATTRIBUTES ===");
var settingsType = ymmTypes.FirstOrDefault(t => t.FullName == "YukkuriMovieMaker.Settings.YMMSettings");
if (settingsType is not null)
{
    foreach (var member in settingsType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
             .Where(m => m.Name.Contains("Playback", StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Name))
    {
        sb.AppendLine($"{member.MemberType}: {member}");
        try
        {
            foreach (var a in member.GetCustomAttributesData())
                sb.AppendLine("  ATTR " + a);
        }
        catch { }
    }
}

sb.AppendLine("\n=== ALL YMM METHODS REFERENCING PLAYBACKRATE ===");
var playbackMethods = new List<(MethodBase Method, string[] Lines)>();
foreach (var t in ymmTypes)
{
    foreach (var m in Methods(t))
    {
        var lines = Disassemble(m).ToArray();
        if (!m.Name.Contains("PlaybackRate", StringComparison.OrdinalIgnoreCase) &&
            !lines.Any(x => x.Contains("PlaybackRate", StringComparison.OrdinalIgnoreCase) || x.Contains("playbackRate", StringComparison.OrdinalIgnoreCase)))
            continue;
        playbackMethods.Add((m, lines));
    }
}
foreach (var entry in playbackMethods.OrderBy(x => x.Method.DeclaringType?.FullName).ThenBy(x => x.Method.Name))
{
    sb.AppendLine($"\nMETHOD {entry.Method.DeclaringType?.FullName}::{entry.Method}");
    foreach (var line in entry.Lines) sb.AppendLine("  " + line);
}

sb.AppendLine("\n=== PLAYBACK METHODS WITH BOUNDARY CONSTANTS ===");
string[] boundaries = ["ldc.i4.s 15", "ldc.i4.s 16", "ldc.i4.s 31", "ldc.i4.s 32", "ldc.i4.s 63", "ldc.i4.s 64",
                       "ldc.i4 15", "ldc.i4 16", "ldc.i4 31", "ldc.i4 32", "ldc.i4 63", "ldc.i4 64",
                       "ldc.i4.8", "ldc.r8 8", "ldc.r8 16", "ldc.r8 32"];
foreach (var entry in playbackMethods)
{
    if (!entry.Lines.Any(x => boundaries.Any(b => x.Contains(b, StringComparison.Ordinal)))) continue;
    sb.AppendLine($"\nBOUNDARY {entry.Method.DeclaringType?.FullName}::{entry.Method}");
    foreach (var line in entry.Lines) sb.AppendLine("  " + line);
}

sb.AppendLine("\n=== CORE PLAYBACK TYPES FULL IL ===");
var coreNames = new HashSet<string>(StringComparer.Ordinal)
{
    "YukkuriMovieMaker.ViewModels.PreviewViewModel",
    "YukkuriMovieMaker.Player.TimelineAudioPlayer",
    "YukkuriMovieMaker.Player.TimelineVideoPlayer",
    "YukkuriMovieMaker.Player.Audio.AudioPlayer",
    "YukkuriMovieMaker.Player.Audio.EffectedItemSource",
    "YukkuriMovieMaker.Player.Audio.Effects.PlaybackRateEffect"
};
foreach (var t in ymmTypes.Where(t => coreNames.Contains(t.FullName ?? "")).OrderBy(t => t.FullName))
{
    sb.AppendLine($"\nTYPE {t.FullName}");
    foreach (var m in Methods(t).OrderBy(m => m.Name))
    {
        var lines = Disassemble(m).ToArray();
        if (lines.Length == 0) continue;
        sb.AppendLine($"\nMETHOD {m}");
        foreach (var line in lines) sb.AppendLine("  " + line);
    }
}

File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
Console.WriteLine($"Wrote {outputPath} ({sb.Length} chars), playback methods={playbackMethods.Count}");
return 0;
