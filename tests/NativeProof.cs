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
        var projectCreated = false;
        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            try
            {
                ticks++;
                foreach (Window window in Application.Current.Windows)
                {
                    var root = window.DataContext;
                    if (root == null || root.GetType().FullName != "YukkuriMovieMaker.ViewModels.MainViewModel") continue;
                    if (root.GetType().GetProperty("ActiveTimelineViewModel")?.GetValue(root) is object active)
                        DumpType(active.GetType());
                    else if (!projectCreated)
                    {
                        projectCreated = true;
                        root.GetType().GetMethod("CreateProject", Type.EmptyTypes)!.Invoke(root, null);
                        Log("fixture: created native project");
                        break;
                    }
                    var timeline = Find<Timeline>(root);
                    var undo = Find<UndoRedoManager>(root);
                    if (timeline == null || undo == null) continue;
                    timer.Stop();
                    Log($"host={root.GetType().FullName};timeline={timeline.GetType().FullName}");
                    Run(timeline, undo);
                    File.WriteAllText(Path.Combine(output, "proof-result.txt"), "PASS P1 P2 P3\n");
                    return;
                }
                if (ticks >= 45) throw new InvalidOperationException("Native main Timeline/UndoRedoManager was not available.");
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
        var characterA = new Character { Name = "TestA" };
        var characterB = new Character { Name = "TestB" };
        var a = new VoiceItem(characterA) { Frame = 10, Length = 30, Layer = 1, Serif = "Test A voice" };
        var b = new VoiceItem(characterB) { Frame = 60, Length = 20, Layer = 1, Serif = "Test B voice" };
        var faceA = new TachieFaceItem(characterA) { Frame = 0, Length = 100, Layer = 3 };
        var faceB = new TachieFaceItem(characterB) { Frame = 0, Length = 100, Layer = 4 };
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
        Assert(Equals(clone.Character, characterA), "P3 Character preserved");
        Assert(!ReferenceEquals(faceA, clone) && faceA.Frame == 0 && faceA.Length == 100 && faceA.Remark != PlacementEngine.Marker, "P3 independent clone/source preserved");
        Log("P1=PASS\nP2=PASS\nP3=PASS");
    }
    private static ItemTemplate MakeTemplate(string name, IItem[] items)
    {
        var result = new ItemTemplate { Name = name };
        foreach (var item in items) result.Items.Add(item);
        return result;
    }
    // CI-only adapter into the native application's actual object graph. Production uses TimelineToolInfo.
    private static T? Find<T>(object root) where T : class
    {
        var queue = new Queue<(object Value, int Depth, string Path)>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        queue.Enqueue((root, 0, "Main"));
        while (queue.Count > 0 && seen.Count < 400)
        {
            var (value, depth, path) = queue.Dequeue();
            if (!seen.Add(value)) continue;
            if (value is T match) { Log("host binding: " + path); return match; }
            if (depth >= 6) continue;
            var type = value.GetType();
            bool Allowed(Type t) => typeof(T).IsAssignableFrom(t) || (t.Namespace ?? "").StartsWith("YukkuriMovieMaker", StringComparison.Ordinal) || (t.Namespace ?? "").StartsWith("Reactive.Bindings", StringComparison.Ordinal);
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead || !Allowed(property.PropertyType)) continue;
                try { if (property.GetValue(value) is object next) queue.Enqueue((next, depth + 1, path + "." + property.Name)); }
                catch { /* Not-ready optional host property. */ }
            }
            for (var current = type; current != null && Allowed(current); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!Allowed(field.FieldType)) continue;
                    if (field.GetValue(value) is object next) queue.Enqueue((next, depth + 1, path + "." + field.Name));
                }
        }
        return null;
    }
    private static void DumpApi()
    {
        foreach (var type in new[] { typeof(Character), typeof(ItemTemplate), typeof(Timeline), typeof(TimelineToolInfo), typeof(UndoRedoManager), typeof(IToolViewModel) }) DumpType(type);
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
