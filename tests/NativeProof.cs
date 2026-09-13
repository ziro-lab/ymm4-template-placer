using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

// Compiled only with -p:Ymm4Proof=true. Never included in distributable builds.
internal static class NativeProof
{
    private static bool scheduled;
    private static string output = "";
    public static void Schedule()
    {
        var dir = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_PROOF_DIR");
        if (scheduled || string.IsNullOrWhiteSpace(dir)) return;
        scheduled = true;
        output = Path.GetFullPath(dir);
        Directory.CreateDirectory(output);
        Application.Current.Dispatcher.BeginInvoke(new Action(Start));
    }
    private static void Start()
    {
        DumpApi();
        var ticks = 0;
        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            try
            {
                ticks++;
                foreach (Window window in Application.Current.Windows)
                {
                    var root = window.DataContext;
                    if (root == null || !root.GetType().Name.Contains("Main", StringComparison.Ordinal)) continue;
                    DumpType(root.GetType());
                    var timeline = Find<Timeline>(root, 3);
                    var undo = Find<UndoRedoManager>(root, 3);
                    if (timeline == null || undo == null) continue;
                    timer.Stop();
                    Log($"host={root.GetType().FullName};timeline={timeline.GetType().FullName}");
                    Run(timeline, undo);
                    File.WriteAllText(Path.Combine(output, "proof-result.txt"), "PASS P1 P2 P3\n");
                    return;
                }
                if (ticks >= 90) throw new InvalidOperationException("Native main Timeline/UndoRedoManager was not available.");
            }
            catch (Exception ex)
            {
                timer.Stop();
                Log(ex.ToString());
                File.WriteAllText(Path.Combine(output, "proof-result.txt"), "FAIL\n" + ex);
            }
        };
        timer.Start();
    }
    private static void Run(Timeline timeline, UndoRedoManager undo)
    {
        Assert(!timeline.Items.Any(), "CI fixture requires an empty native Timeline");
        var a = Create<VoiceItem>("TestA");
        Set(a, "Character", "TestA"); a.Frame = 10; a.Length = 30; a.Layer = 1; a.Serif = "Test A voice";
        var b = Create<VoiceItem>("TestB");
        Set(b, "Character", "TestB"); b.Frame = 60; b.Length = 20; b.Layer = 1; b.Serif = "Test B voice";
        var faceA = Create<TachieFaceItem>("TestA");
        Set(faceA, "Character", "TestA"); faceA.Frame = 0; faceA.Length = 100; faceA.Layer = 3;
        var faceB = Create<TachieFaceItem>("TestB");
        Set(faceB, "Character", "TestB"); faceB.Frame = 0; faceB.Length = 100; faceB.Layer = 4;
        var templateA = MakeTemplate("TestA/Neutral", [faceA]);
        var templateB = MakeTemplate("TestB/Neutral", [faceB]);
        ItemSettings.Default.Templates.Add(templateA);
        ItemSettings.Default.Templates.Add(templateB);
        ItemSettings.Default.Templates.Add(MakeTemplate("Invalid/multiple", [faceA, faceB]));
        ItemSettings.Default.Templates.Add(MakeTemplate("Invalid/voice", [a]));
        var catalog = TemplateCatalog.Read();
        Assert(catalog.Count(x => x.Name.StartsWith("Test", StringComparison.Ordinal)) == 2, "P1 live registered Face Template catalog");
        Assert(catalog.All(x => !x.Name.StartsWith("Invalid/", StringComparison.Ordinal)), "P1 singleton Face filtering");
        Assert(TemplateCatalog.ForVoice(a, catalog).Select(x => x.Name).SequenceEqual(["TestA/Neutral"]), "P1 Character filtering");
        Assert(timeline.TryAddItems([a], a.Frame, a.Layer), "fixture Voice A insertion");
        Assert(timeline.TryAddItems([b], b.Frame, b.Layer), "fixture Voice B insertion");
        undo.Record();
        var voices = VoiceSnapshot.Capture(timeline);
        Assert(voices.Count == 2 && voices[0].Character == "TestA" && voices[0].Frame == 10 && voices[0].Length == 30 && voices[0].Serif == "Test A voice", "P2 exact live Voice snapshot");
        var clone = PlacementEngine.CloneForVoice(voices[0], catalog.Single(x => x.Name == "TestA/Neutral"));
        PlacementEngine.AddPrepared(timeline, undo, [clone]);
        Assert(timeline.Items.Contains(clone) && clone.Frame == 10 && clone.Length == 30 && clone.Layer == 3 && clone.Remark == PlacementEngine.Marker, "P3 native Timeline placement");
        Assert(!ReferenceEquals(faceA, clone) && faceA.Frame == 0 && faceA.Length == 100 && faceA.Remark != PlacementEngine.Marker, "P3 independent clone/source preserved");
        Log("P1=PASS\nP2=PASS\nP3=PASS");
    }
    private static T Create<T>(string name, IItem[]? items = null)
    {
        var errors = new List<string>();
        foreach (var ctor in typeof(T).GetConstructors().OrderBy(x => x.GetParameters().Length))
        {
            try
            {
                var args = ctor.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue :
                    p.ParameterType == typeof(string) ? name :
                    items != null && p.ParameterType.IsAssignableFrom(typeof(IItem[])) ? (object)items :
                    p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
                return (T)ctor.Invoke(args);
            }
            catch (Exception ex) { errors.Add(ex.GetBaseException().Message); }
        }
        throw new InvalidOperationException($"Fixture constructor {typeof(T).FullName}: {string.Join("; ", errors)}");
    }
    private static ItemTemplate MakeTemplate(string name, IItem[] items)
    {
        var result = Create<ItemTemplate>(name, items);
        Set(result, "Name", name);
        var property = typeof(ItemTemplate).GetProperty("Items")!;
        if (property.CanWrite)
        {
            object value = property.PropertyType.IsAssignableFrom(typeof(IItem[])) ? items : items.ToList();
            property.SetValue(result, value);
        }
        else if (property.GetValue(result) is IList list)
        {
            list.Clear(); foreach (var item in items) list.Add(item);
        }
        Assert(result.Items.Count() == items.Length, "fixture Template item count");
        return result;
    }
    private static void Set(object value, string name, object propertyValue) => value.GetType().GetProperty(name)!.SetValue(value, propertyValue);
    private static T? Find<T>(object root, int depth) where T : class
    {
        if (root is T found) return found;
        if (depth == 0) return null;
        foreach (var property in root.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
            var ns = property.PropertyType.Namespace ?? "";
            if (!typeof(T).IsAssignableFrom(property.PropertyType) && !ns.StartsWith("YukkuriMovieMaker", StringComparison.Ordinal)) continue;
            try
            {
                var value = property.GetValue(root);
                if (value != null && !ReferenceEquals(value, root) && Find<T>(value, depth - 1) is T match) return match;
            }
            catch { /* A non-ready host property must not abort the readiness poll. */ }
        }
        return null;
    }
    private static void DumpApi()
    {
        foreach (var type in new[] { typeof(ItemTemplate), typeof(VoiceItem), typeof(TachieFaceItem), typeof(Timeline), typeof(TimelineToolInfo), typeof(UndoRedoManager), typeof(ITimelineToolViewModel), typeof(IToolViewModel) }) DumpType(type);
    }
    private static readonly HashSet<Type> dumped = [];
    private static void DumpType(Type type)
    {
        if (!dumped.Add(type)) return;
        var text = new StringBuilder().AppendLine("TYPE " + type.FullName);
        foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
            text.AppendLine(member.ToString());
        File.AppendAllText(Path.Combine(output, "api-surface.txt"), text.ToString());
    }
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("ASSERT FAIL: " + message);
        Log("ASSERT PASS: " + message);
    }
    private static void Log(string text) => File.AppendAllText(Path.Combine(output, "proof-log.txt"), text + "\n");
}
