#pragma warning disable CS0618
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private sealed class P3Config : TachieCharacterParameterBase
    {
        public string PresetDefinitions { get; set; } = "//Neutral\nmood=neutral\n//Smile\nmood=smile\n";
        public int Revision { get; set; }
    }
    private sealed class P3DirectFace : TachieFaceParameterBase
    {
        public string Preset { get; set; } = "";
        protected override IEnumerable<IAnimatable> GetAnimatables() => [];
    }
    private sealed class P3NoiseFace : TachieFaceParameterBase
    {
        public string CompressionPreset { get; set; } = "fast";
        protected override IEnumerable<IAnimatable> GetAnimatables() => [];
    }
    private class P3ExpandedFace : TachieFaceParameterBase
    {
        public string Mood { get; set; } = "Neutral";
        public int Amount { get; set; } = 1;
        protected override IEnumerable<IAnimatable> GetAnimatables() => [];
    }
    private class P3LegacyFace : P3ExpandedFace
    {
        [P3LegacyPreset]
        public object PresetDummy { get; } = new();
    }
    private sealed class P3ModernFace : P3ExpandedFace
    {
        [P3ModernPreset]
        public object PresetDummy { get; } = new();
    }
    private sealed class P3UnstableFace : P3LegacyFace
    {
        public Guid Unstable => Guid.NewGuid();
    }
    private sealed class P3CycleFace : P3LegacyFace
    {
        public object Cycle => this;
    }
    private sealed class P3ThrowingState
    {
        public int Value => throw new InvalidOperationException("P3 intentional getter failure");
    }
    private sealed class P3CountedState
    {
        public int Reads;
        public int Value { get { Reads++; return 1; } }
    }
    private static class P3EditorFixture
    {
        internal static readonly Dictionary<ComboBox, SelectionChangedEventHandler> Handlers = new();
        internal static Action? AfterBind;
        internal static bool FailCreate, FailBind, FailClear, Duplicate;
        internal static ComboBox? LastControl;
        internal static P3ExpandedFace? LastFace;
        internal static FrameworkElement Create()
        {
            if (FailCreate) throw new InvalidOperationException("P3 intentional Create failure");
            return new ComboBox { ItemsSource = Duplicate ? new[] { "Neutral", "Smile", "Smile" } : new[] { "Neutral", "Smile" } };
        }
        internal static void Bind(FrameworkElement element, object owner)
        {
            var control = (ComboBox)element;
            var face = (P3ExpandedFace)owner;
            SelectionChangedEventHandler handler = (_, _) =>
            {
                face.Mood = control.SelectedItem as string ?? "Neutral";
                face.Amount = face.Mood == "Smile" ? 7 : 1;
            };
            Handlers.Add(control, handler);
            control.SelectionChanged += handler;
            LastControl = control; LastFace = face;
            AfterBind?.Invoke();
            if (FailBind) throw new InvalidOperationException("P3 intentional partial Bind failure");
        }
        internal static void Clear(FrameworkElement element)
        {
            var control = (ComboBox)element;
            if (Handlers.Remove(control, out var handler)) control.SelectionChanged -= handler;
            if (FailClear) throw new InvalidOperationException("P3 intentional Clear failure");
        }
    }
    private sealed class P3LegacyPresetAttribute : PropertyEditorForTachieParameterAttribute
    {
        public override FrameworkElement Create() => P3EditorFixture.Create();
        public override void SetBindings(FrameworkElement control, object item, object propertyOwner, PropertyInfo propertyInfo) => P3EditorFixture.Bind(control, propertyOwner);
        public override void ClearBindings(FrameworkElement control) => P3EditorFixture.Clear(control);
    }
    private sealed class P3ModernPresetAttribute : PropertyEditorAttribute2, IPropertyEditorForTachieParameterAttribute
    {
        public object? CharacterParameter { get; set; }
        public override FrameworkElement Create() => P3EditorFixture.Create();
        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties) => P3EditorFixture.Bind(control, itemProperties.Single().PropertyOwner);
        public override void ClearBindings(FrameworkElement control) => P3EditorFixture.Clear(control);
    }

    private static async Task VerifyTachiePresetCapability(Timeline timeline, UndoRedoManager undo)
    {
        stage = "Tachie Preset P3 capability";
        var checks = new List<string>();
        void Check(bool condition, string name) { Assert(condition, "TP-P3 " + name); checks.Add(name); }
        var original = timeline.Items;
        var signature = Signature(timeline);
        var config = new P3Config();
        var characters = new Dictionary<string, Character>();
        foreach (var name in new[] { "Direct", "Legacy", "Modern", "Noise", "Unstable", "Cycle" })
            characters.Add(name, new Character { Name = "P3 " + name });
        var factories = new Dictionary<string, Func<object>>
        {
            ["P3 Direct"] = () => new P3DirectFace(), ["P3 Legacy"] = () => new P3LegacyFace(),
            ["P3 Modern"] = () => new P3ModernFace(), ["P3 Noise"] = () => new P3NoiseFace(),
            ["P3 Unstable"] = () => new P3UnstableFace(), ["P3 Cycle"] = () => new P3CycleFace()
        };
        var resolves = 0;
        TachiePresetProbeTarget Resolve(Character c)
        {
            resolves++;
            return new(c, config, typeof(P3Config), factories[c.Name], () => true);
        }
        using var coordinator = new TachiePresetCapabilityCoordinator(Resolve);
        try
        {
            var inactive = await coordinator.ScanAsync(characters.Values.ToArray(), () => false);
            Check(inactive.Count == 0 && resolves == 0 && coordinator.Diagnostics.CharacterScans == 0,
                "inactive source does zero resolution/discovery work");
            var repeated = Enumerable.Repeat(characters["Direct"], 1000).ToArray();
            var direct = (await coordinator.ScanAsync(repeated, () => true)).Single().Capability;
            Check(direct is { Level: TachiePresetCapabilityLevel.Strong } && direct.Candidates.Count == 2 &&
                coordinator.Diagnostics.CharacterScans == 1 && resolves == 1,
                "1000 Voices sharing one Character require one direct discovery");
            var cached = (await coordinator.ScanAsync(repeated, () => true)).Single().Capability;
            Check(ReferenceEquals(direct, cached) && coordinator.Diagnostics.CharacterScans == 1 && coordinator.Diagnostics.CacheHits == 1,
                "unchanged configuration reuses immutable session cache");
            config.Revision++;
            var changed = (await coordinator.ScanAsync(repeated, () => true)).Single().Capability;
            Check(changed?.Fingerprint != direct?.Fingerprint && coordinator.Diagnostics.CharacterScans == 2,
                "public Character configuration changes invalidate cache identity");
            var legacy = (await coordinator.ScanAsync([characters["Legacy"]], () => true)).Single().Capability;
            var modern = (await coordinator.ScanAsync([characters["Modern"]], () => true)).Single().Capability;
            Check(legacy is { Level: TachiePresetCapabilityLevel.Strong } && legacy.Candidates.Count == 2 &&
                legacy.Candidates.All(c => c.Route.Kind == TachiePresetRouteKind.PropertyEditorLegacy),
                "legacy public editor expands actual fresh face state");
            Check(modern is { Level: TachiePresetCapabilityLevel.Strong } && modern.Candidates.Count == 2 &&
                modern.Candidates.All(c => c.Route.Kind == TachiePresetRouteKind.PropertyEditorModern),
                "modern public ItemProperty[] editor expands actual fresh face state");
            Check(P3EditorFixture.Handlers.Count == 0 && coordinator.Diagnostics.ActiveEditors == 0 &&
                coordinator.Diagnostics.EditorsOpened == coordinator.Diagnostics.EditorsCleared,
                "successful scans release all temporary editor bindings");
            var lastFace = P3EditorFixture.LastFace!;
            var before = lastFace.Mood;
            P3EditorFixture.LastControl!.SelectedItem = before == "Smile" ? "Neutral" : "Smile";
            Check(lastFace.Mood == before, "detached control cannot mutate its old face after cleanup");
            var noise = (await coordinator.ScanAsync([characters["Noise"]], () => true)).Single().Capability;
            Check(noise is { Level: TachiePresetCapabilityLevel.None }, "unrelated CompressionPreset is rejected");
            var unstable = (await coordinator.ScanAsync([characters["Unstable"]], () => true)).Single().Capability;
            var cycle = (await coordinator.ScanAsync([characters["Cycle"]], () => true)).Single().Capability;
            Check(unstable is { Level: TachiePresetCapabilityLevel.Experimental }, "unstable public state is never Strong");
            Check(cycle is { Level: TachiePresetCapabilityLevel.Experimental }, "cyclic face state is never silently hashed as Strong");
            var loop = new ArrayList(); loop.Add(loop);
            Check(TachiePresetPublicState.TryHash(loop) == null && TachiePresetPublicState.TryHash(new P3ThrowingState()) == null &&
                TachiePresetPublicState.TryHash(Enumerable.Range(0, 257).ToArray()) == null,
                "cycles, throwing getters and collection overflow fail closed");
            Check(TachiePresetPublicState.Hash(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }) ==
                TachiePresetPublicState.Hash(new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 }),
                "dictionary fingerprint is independent of insertion order");
            Check(TachiePresetPublicState.Hash(new[] { "a;b", "c" }) != TachiePresetPublicState.Hash(new[] { "a", "b;c" }),
                "length-framed fingerprint cannot confuse delimiter-containing strings");
            var counted = new P3CountedState();
            var rejectedWorker = await Task.Run(() =>
            {
                try { TachiePresetPublicState.Hash(counted); return false; }
                catch (InvalidOperationException) { return true; }
            });
            Check(rejectedWorker && counted.Reads == 0, "off-thread access fails before reading any property");

            foreach (var failure in new[] { "Create", "Bind", "Clear", "Duplicate" })
            {
                coordinator.Invalidate();
                P3EditorFixture.FailCreate = failure == "Create";
                P3EditorFixture.FailBind = failure == "Bind";
                P3EditorFixture.FailClear = failure == "Clear";
                P3EditorFixture.Duplicate = failure == "Duplicate";
                var results = await coordinator.ScanAsync([characters["Modern"], characters["Direct"]], () => true);
                Check(results[0].Capability == null && results[1].Capability?.Level == TachiePresetCapabilityLevel.Strong &&
                    P3EditorFixture.Handlers.Count == 0 && coordinator.Diagnostics.ActiveEditors == 0,
                    failure + " failure is local and cleans partial editor state");
                P3EditorFixture.FailCreate = P3EditorFixture.FailBind = P3EditorFixture.FailClear = P3EditorFixture.Duplicate = false;
            }
            coordinator.Invalidate();
            using (var stop = new CancellationTokenSource())
            {
                P3EditorFixture.AfterBind = stop.Cancel;
                var cancelled = false;
                try { await coordinator.ScanAsync([characters["Modern"]], () => true, stop.Token); }
                catch (OperationCanceledException) { cancelled = true; }
                finally { P3EditorFixture.AfterBind = null; }
                Check(cancelled && P3EditorFixture.Handlers.Count == 0 && coordinator.Diagnostics.ActiveEditors == 0,
                    "cancellation after real binding cleans the editor before returning");
            }
            coordinator.Invalidate();
            Task<IReadOnlyList<TachiePresetCharacterCapability>>? newest = null;
            P3EditorFixture.AfterBind = () =>
            {
                P3EditorFixture.AfterBind = null;
                config.Revision++;
                newest = coordinator.ScanAsync([characters["Modern"]], () => true);
            };
            var oldCancelled = false;
            try { await coordinator.ScanAsync([characters["Modern"]], () => true); }
            catch (OperationCanceledException) { oldCancelled = true; }
            Check(newest != null && oldCancelled, "a newer request cancels the old generation while bound");
            var newestResults = await newest!;
            Check(newestResults.Single().Capability?.Level == TachiePresetCapabilityLevel.Strong &&
                P3EditorFixture.Handlers.Count == 0 && coordinator.Diagnostics.ActiveEditors == 0 && coordinator.Diagnostics.PeakEditors == 1,
                "latest-wins requests serialize editor lifetime and only return the newest result");
            coordinator.Invalidate();
            var current = true;
            P3EditorFixture.AfterBind = () => { current = false; P3EditorFixture.AfterBind = null; };
            var staleCancelled = false;
            try { await coordinator.ScanAsync([characters["Modern"]], () => current); }
            catch (OperationCanceledException) { staleCancelled = true; }
            Check(staleCancelled && coordinator.Diagnostics.ActiveEditors == 0 && coordinator.Diagnostics.StaleDiscards > 0,
                "source/Timeline scope loss discards results and releases the staging editor");
            P3EditorFixture.AfterBind = null;
            config.PresetDefinitions = "//Same\nx=1\n//Same\nx=2\n";
            var duplicate = (await coordinator.ScanAsync([characters["Direct"]], () => true)).Single();
            Check(duplicate.Capability == null, "duplicate direct definitions are not silently deduplicated");

            var fixture = CreateP3Fixtures();
            foreach (var kind in new[] { "Animation", "Psd" })
            {
                var typeName = kind == "Animation"
                    ? "YukkuriMovieMaker.Plugin.Tachie.AnimationTachie.AnimationTachiePlugin"
                    : "YukkuriMovieMaker.Plugin.Tachie.Psd.PsdTachiePlugin";
                var plugin = PluginLoader.TachiePlugins.Single(p => p.GetType().FullName == typeName);
                var cp = plugin.CreateCharacterParameter();
                foreach (var p in cp.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    if (p.PropertyType == typeof(string) && p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
                    {
                        if (p.Name.Contains("Directory", StringComparison.Ordinal)) p.SetValue(cp, Path.Combine(fixture, "animation"));
                        else if (p.Name.Contains("FilePath", StringComparison.Ordinal)) p.SetValue(cp, Path.Combine(fixture, "psd", "1layer.psd"));
                    }
                var character = new Character { Name = "P3 built-in " + kind, TachieType = plugin.GetType(),
                    TachieCharacterParameter = cp, TachieDefaultFaceParameter = plugin.CreateFaceParameter() };
                var configBefore = TachiePresetPublicState.Hash(cp);
                var defaultBefore = TachiePresetPublicState.TryHash(character.TachieDefaultFaceParameter!);
                using var real = new TachiePresetCapabilityCoordinator();
                var observed = (await real.ScanAsync([character], () => true)).Single();
                var names = observed.Capability?.Candidates.Select(c => c.Label + ":" + c.Confidence).ToArray() ?? [];
                Log("TP-P3 built-in " + kind + " reason=" + observed.UnavailableReason + " candidates=" + string.Join(",", names));
                var expected = kind == "Animation" ? "CNWL_ANIM_SMILE" : "CNWL_PSD_ON";
                Check(observed.Capability?.Candidates.Any(c => c.Label == expected && c.Confidence == TachiePresetCapabilityLevel.Strong) == true,
                    "built-in " + kind + " uses the product discovery path with a Strong fixture candidate");
                Check(ReferenceEquals(character.TachieCharacterParameter, cp) && TachiePresetPublicState.Hash(cp) == configBefore &&
                    TachiePresetPublicState.TryHash(character.TachieDefaultFaceParameter!) == defaultBefore && real.Diagnostics.ActiveEditors == 0,
                    "built-in " + kind + " leaves current Character/config/default face untouched");
            }
            Check(ReferenceEquals(timeline.Items, original) && Signature(timeline) == signature,
                "all capability tests perform zero Timeline writes");
            File.WriteAllText(Path.Combine(output, "tachie-preset-capability.json"), JsonSerializer.Serialize(new
            {
                schema = "YMM4-Template-Placer-Tachie-Preset-Capability/1", host = "YMM4 4.55.1.1 Lite", result = "PASS",
                sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
                checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
                runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), checks
            }, new JsonSerializerOptions { WriteIndented = true }));
            Log("TACHIE_PRESET_CAPABILITY_P3=PASS");
        }
        finally
        {
            P3EditorFixture.AfterBind = null;
            P3EditorFixture.FailCreate = P3EditorFixture.FailBind = P3EditorFixture.FailClear = P3EditorFixture.Duplicate = false;
            P3EditorFixture.LastControl = null; P3EditorFixture.LastFace = null;
        }
    }

    private static string CreateP3Fixtures()
    {
        var root = Path.Combine(output, "tachie-preset-fixture");
        var animation = Path.Combine(root, "animation");
        Directory.CreateDirectory(animation);
        File.WriteAllText(Path.Combine(animation, "preset.ini"),
            "[CNWL_ANIM_NEUTRAL]\n眉=neutral.png\n目=neutral.png\n口=neutral.png\n[CNWL_ANIM_SMILE]\n眉=smile.png\n目=smile.png\n口=smile.png\n");
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        foreach (var part in new[] { "眉", "目", "口" })
        {
            var dir = Path.Combine(animation, part); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "neutral.png"), png); File.WriteAllBytes(Path.Combine(dir, "smile.png"), png);
        }
        var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? Path.GetFullPath(Path.Combine(output, ".."));
        using var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace, "fixtures", "tachie-preset", "fixture.json")));
        var compressed = Convert.FromBase64String(metadata.RootElement.GetProperty("zlibBase64").GetString()!);
        using var input = new MemoryStream(compressed);
        using var inflate = new ZLibStream(input, CompressionMode.Decompress);
        using var restored = new MemoryStream(); inflate.CopyTo(restored);
        var bytes = restored.ToArray();
        Assert(bytes.Length == 6476 && Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() ==
            "c2b457581d549f4bea2e5c34a04b68b2917f640bce0e958b6bca9c65be75174b", "TP-P3 pinned P0 PSD fixture hash");
        var psd = Path.Combine(root, "psd"); Directory.CreateDirectory(psd);
        File.WriteAllBytes(Path.Combine(psd, "1layer.psd"), bytes);
        File.WriteAllText(Path.Combine(psd, "1layer-ymm.json"),
            "{\"Presets\":[{\"Name\":\"CNWL_PSD_ON\",\"Layers\":[\"n1\"]},{\"Name\":\"CNWL_PSD_OFF\",\"Layers\":[]}],\"MouthAnimations\":[],\"MouthVowelAnimations\":[],\"EyeAnimations\":[]}");
        return root;
    }
}
