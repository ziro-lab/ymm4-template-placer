using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

// All NativeProof partials are excluded from normal distribution builds.
internal static partial class NativeProof
{
    private static bool scheduled;
    private static string output = "", stage = "host";
    public static PlacerViewModel? ViewModel;
    public static PlacerView? View;
    public static void Schedule()
    {
        var dir = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_PROOF_DIR");
        if (scheduled || string.IsNullOrWhiteSpace(dir)) return;
        scheduled = true; output = Path.GetFullPath(dir); Directory.CreateDirectory(output);
        Application.Current.Dispatcher.BeginInvoke(new Action(Start));
    }
    private static void Start()
    {
        var ticks = 0; var projectCreated = false;
        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += async (_, _) =>
        {
            try
            {
                ticks++;
                foreach (Window window in Application.Current.Windows)
                {
                    var root = window.DataContext;
                    if (root?.GetType().FullName != "YukkuriMovieMaker.ViewModels.MainViewModel") continue;
                    var active = root.GetType().GetProperty("ActiveTimelineViewModel")?.GetValue(root);
                    if (active == null && !projectCreated)
                    {
                        projectCreated = true; root.GetType().GetMethod("CreateProject", Type.EmptyTypes)!.Invoke(root, null); break;
                    }
                    // Exact CI-only paths proven on the pinned 4.55.1.1 runtime. Production does not use reflection.
                    var model = root.GetType().GetField("model", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(root);
                    var timeline = active?.GetType().GetField("timeline", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(active) as Timeline;
                    var undo = model?.GetType().GetProperty("UndoRedoManager")?.GetValue(model) as UndoRedoManager;
                    if (timeline == null || undo == null) continue;
                    timer.Stop(); Log($"YMM4={typeof(Timeline).Assembly.GetName().Version}; host={root.GetType().FullName}");
                    DumpType(timeline.LayerSettings.GetType());
                    await Run(root, timeline, undo);
                    File.WriteAllText(Path.Combine(output, "proof-result.txt"), "PASS P1 P2 P3 P4 P5 P6 P7 P8\n"); return;
                }
                if (ticks >= 45) throw new InvalidOperationException("Pinned native host adapter could not obtain the current Timeline / Undo manager.");
            }
            catch (Exception ex)
            {
                timer.Stop(); Log(ex.ToString());
                File.WriteAllText(Path.Combine(output, "proof-result.txt"), "FAIL " + stage + "\n" + ex);
            }
        };
        timer.Start();
    }
    private static Task Idle() => Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static ItemTemplate Template(string name, IItem[] items)
    {
        var result = new ItemTemplate { Name = name }; foreach (var item in items) result.Items.Add(item); return result;
    }
    private static string Signature(Timeline timeline) => string.Join("\n", timeline.Items.Select(x => $"{x.GetType().Name}|{x.Frame}|{x.Length}|{x.Layer}|{x.Group}|{x.Remark}|{(x is TachieFaceItem f ? f.CharacterName : x is VoiceItem v ? v.CharacterName + ":" + v.Serif : "")}"));
    private static void RejectWithoutMutation(Timeline timeline, Action action, string name)
    {
        var before = timeline.Items; var signature = Signature(timeline); var rejected = false;
        try { action(); } catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException) { rejected = true; Log("expected rejection: " + ex.Message); }
        Assert(rejected && ReferenceEquals(before, timeline.Items) && Signature(timeline) == signature, name);
    }
    private static readonly HashSet<Type> dumped = [];
    private static void DumpType(Type type)
    {
        if (!dumped.Add(type)) return;
        var text = new StringBuilder().AppendLine("TYPE " + type.FullName);
        foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)) text.AppendLine(member.ToString());
        File.AppendAllText(Path.Combine(output, "api-surface.txt"), text.ToString());
    }
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("ASSERT FAIL: " + message);
        Log("ASSERT PASS: " + message);
    }
    private static void Log(string text) => File.AppendAllText(Path.Combine(output, "proof-log.txt"), text + "\n");
}
