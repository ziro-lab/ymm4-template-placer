using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Text;

if (args.Length < 2) return 2;
var dir = Path.GetFullPath(args[0]);
var outPath = Path.GetFullPath(args[1]);
AssemblyLoadContext.Default.Resolving += (_, n) => {
    var p = Path.Combine(dir, n.Name + ".dll");
    if (!File.Exists(p)) return null;
    try { return AssemblyLoadContext.Default.LoadFromAssemblyPath(p); } catch { return null; }
};
var asms = new List<Assembly>();
foreach (var p in Directory.EnumerateFiles(dir, "*.dll").Concat(Directory.EnumerateFiles(dir, "*.exe")))
    try { var a = AssemblyLoadContext.Default.LoadFromAssemblyPath(p); if (!asms.Any(x => x.FullName == a.FullName)) asms.Add(a); } catch { }
IEnumerable<Type> Types(Assembly a) { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null)!; } catch { return []; } }
var types = asms.SelectMany(Types).ToArray();
Type T(string name) => types.FirstOrDefault(x => x.FullName == name);

var one = new OpCode[256]; var two = new OpCode[256];
foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)) if (f.GetValue(null) is OpCode o) { var v=(ushort)o.Value; if(v<256) one[v]=o; else if((v&0xff00)==0xfe00) two[v&255]=o; }
string Tok(MethodBase m,int token,OperandType k) { try { var ta=m.DeclaringType?.GetGenericArguments(); var ma=m.IsGenericMethod?m.GetGenericArguments():null; return k switch { OperandType.InlineMethod=>m.Module.ResolveMethod(token,ta,ma)?.ToString(), OperandType.InlineField=>m.Module.ResolveField(token,ta,ma)?.ToString(), OperandType.InlineType=>m.Module.ResolveType(token,ta,ma)?.ToString(), OperandType.InlineTok=>m.Module.ResolveMember(token,ta,ma)?.ToString(), OperandType.InlineString=>"\""+m.Module.ResolveString(token)+"\"", _=>null } ?? $"0x{token:X8}"; } catch { return $"0x{token:X8}"; } }
List<string> IL(MethodBase m) {
    var r=new List<string>(); var b=m.GetMethodBody()?.GetILAsByteArray(); if(b==null) return r; int p=0;
    while(p<b.Length) { int off=p; OpCode o; byte x=b[p++]; o=x==0xfe?two[b[p++]]:one[x]; string a="";
        try { switch(o.OperandType) {
            case OperandType.InlineNone: break; case OperandType.ShortInlineI:a=((sbyte)b[p]).ToString();p++;break; case OperandType.InlineI:a=BitConverter.ToInt32(b,p).ToString();p+=4;break; case OperandType.InlineI8:a=BitConverter.ToInt64(b,p).ToString();p+=8;break; case OperandType.ShortInlineR:a=BitConverter.ToSingle(b,p).ToString("R");p+=4;break; case OperandType.InlineR:a=BitConverter.ToDouble(b,p).ToString("R");p+=8;break; case OperandType.ShortInlineVar:a=b[p++].ToString();break; case OperandType.InlineVar:a=BitConverter.ToUInt16(b,p).ToString();p+=2;break;
            case OperandType.ShortInlineBrTarget:{var d=(sbyte)b[p++];a=$"IL_{p+d:X4}";break;} case OperandType.InlineBrTarget:{var d=BitConverter.ToInt32(b,p);p+=4;a=$"IL_{p+d:X4}";break;} case OperandType.InlineSwitch:{var n=BitConverter.ToInt32(b,p);p+=4;var q=p+n*4;var z=new string[n];for(int i=0;i<n;i++)z[i]=$"IL_{q+BitConverter.ToInt32(b,p+i*4):X4}";p=q;a=string.Join(",",z);break;}
            case OperandType.InlineString: case OperandType.InlineMethod: case OperandType.InlineField: case OperandType.InlineType: case OperandType.InlineTok: case OperandType.InlineSig:{var token=BitConverter.ToInt32(b,p);p+=4;a=Tok(m,token,o.OperandType);break;} }
        } catch(Exception e){a="<decode:"+e.GetType().Name+">";} r.Add($"IL_{off:X4}: {o.Name} {a}".TrimEnd()); }
    return r;
}
IEnumerable<MethodInfo> Methods(Type t) { try { return t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly); } catch { return []; } }

var sb=new StringBuilder();
void DumpType(string name) {
    var t=T(name); sb.AppendLine($"\n=== TYPE {name} ==="); if(t==null){sb.AppendLine("NOT FOUND");return;}
    for(var c=t;c!=null;c=c.BaseType) sb.AppendLine("BASE " + c.FullName);
    foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(p=>p.Name.Contains("PlaybackRate"))) {
        sb.AppendLine($"PROP {p.DeclaringType?.FullName}::{p.PropertyType} {p.Name}"); sb.AppendLine($"  GET {p.GetMethod?.DeclaringType?.FullName}::{p.GetMethod}"); sb.AppendLine($"  SET {p.SetMethod?.DeclaringType?.FullName}::{p.SetMethod}");
        foreach(var ca in p.CustomAttributes) sb.AppendLine("  ATTR "+ca.AttributeType.FullName+"("+string.Join(",",ca.ConstructorArguments.Select(a=>a.Value))+ ")");
    }
    foreach(var f in t.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(f=>f.Name.Contains("playback",StringComparison.OrdinalIgnoreCase))) sb.AppendLine($"FIELD {f.FieldType} {f.Name}");
}
DumpType("YukkuriMovieMaker.Settings.YMMSettings");
DumpType("YukkuriMovieMaker.ViewModels.PreviewViewModel");
DumpType("YukkuriMovieMaker.Player.TimelineVideoPlayer");
DumpType("YukkuriMovieMaker.Player.TimelineAudioPlayer");
DumpType("YukkuriMovieMaker.Player.Audio.AudioPlayer");

sb.AppendLine("\n=== PLAYBACK RATE ACCESSOR IL ACROSS TARGET CHAINS ===");
foreach(var rootName in new[]{"YukkuriMovieMaker.Settings.YMMSettings","YukkuriMovieMaker.ViewModels.PreviewViewModel","YukkuriMovieMaker.Player.TimelineVideoPlayer","YukkuriMovieMaker.Player.TimelineAudioPlayer","YukkuriMovieMaker.Player.Audio.AudioPlayer"}) {
    for(var t=T(rootName);t!=null;t=t.BaseType) foreach(var m in Methods(t).Where(m=>m.Name.Contains("PlaybackRate",StringComparison.OrdinalIgnoreCase))) { sb.AppendLine($"\nMETHOD {m.DeclaringType?.FullName}::{m}"); foreach(var l in IL(m)) sb.AppendLine("  "+l); }
}

sb.AppendLine("\n=== ALL YMM METHODS WHOSE IL REFERENCES PLAYBACKRATE ===");
foreach(var t in types.Where(t=>t.FullName?.StartsWith("YukkuriMovieMaker.")==true)) foreach(var m in Methods(t)) {
    List<string> lines; try { lines=IL(m); } catch { continue; }
    if(!lines.Any(l=>l.Contains("PlaybackRate",StringComparison.OrdinalIgnoreCase))) continue;
    sb.AppendLine($"\nREFMETHOD {m.DeclaringType?.FullName}::{m}"); foreach(var l in lines) sb.AppendLine("  "+l);
}

sb.AppendLine("\n=== METHODS WITH LIKELY 8X INDEX CONSTANTS (31/32) AND PLAYBACK CONTEXT ===");
foreach(var t in types.Where(t=>t.FullName?.StartsWith("YukkuriMovieMaker.")==true)) foreach(var m in Methods(t)) {
    List<string> lines; try { lines=IL(m); } catch { continue; }
    bool rate=lines.Any(l=>l.Contains("PlaybackRate",StringComparison.OrdinalIgnoreCase)) || m.Name.Contains("PlaybackRate",StringComparison.OrdinalIgnoreCase);
    bool cap=lines.Any(l=>l.Contains("ldc.i4.s 31")||l.Contains("ldc.i4 31")||l.Contains("ldc.i4.s 32")||l.Contains("ldc.i4 32"));
    if(rate&&cap){ sb.AppendLine($"\nCAPCANDIDATE {m.DeclaringType?.FullName}::{m}"); foreach(var l in lines) sb.AppendLine("  "+l); }
}

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!); File.WriteAllText(outPath,sb.ToString(),new UTF8Encoding(false)); Console.Write(sb.ToString()); return 0;
