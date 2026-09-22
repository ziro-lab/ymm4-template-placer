#pragma warning disable CS0618
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private sealed class P3TeardownFace : P3ExpandedFace
    {
        [P3TeardownPreset]
        public object PresetDummy { get; } = new();
    }
    private sealed class P3TeardownPresetAttribute : PropertyEditorForTachieParameterAttribute
    {
        private P3ExpandedFace? face;
        public override FrameworkElement Create() => P3EditorFixture.Create();
        public override void SetBindings(FrameworkElement control, object item, object owner, PropertyInfo property)
        { face = (P3ExpandedFace)owner; P3EditorFixture.Bind(control, owner); }
        public override void ClearBindings(FrameworkElement control)
        {
            P3EditorFixture.Clear(control);
            if (face != null) { face.Mood = "teardown"; face.Amount = 0; face = null; }
        }
    }
    private sealed class P3ChangingConfig : TachieCharacterParameterBase
    {
        public Guid Unstable => Guid.NewGuid();
    }

    private static async Task VerifyTachiePresetGuards(Timeline timeline)
    {
        stage = "Tachie Preset P3 teardown and identity guards";
        var initial = Signature(timeline);
        var checks = new List<string>();
        void Check(bool condition, string name) { Assert(condition, "TP-P3 guard " + name); checks.Add(name); }
        var config = new P3Config();
        var character = new Character { Name = "P3 teardown" };
        using (var coordinator = new TachiePresetCapabilityCoordinator(c =>
            new(c, config, typeof(P3Config), () => new P3TeardownFace(), () => true)))
        {
            var result = (await coordinator.ScanAsync([character], () => true)).Single().Capability;
            Check(result is { Level: TachiePresetCapabilityLevel.Experimental } && result.Candidates.Count == 2 &&
                result.Candidates.All(c => c.Confidence == TachiePresetCapabilityLevel.Experimental),
                "a face changed by successful ClearBindings is not promoted to Strong");
            Check(coordinator.Diagnostics.ActiveEditors == 0 && P3EditorFixture.Handlers.Count == 0,
                "teardown-state mismatch still clears all temporary bindings");
        }
        var unstable = new Character { Name = "P3 unstable config" };
        var good = new Character { Name = "P3 stable config" };
        using (var coordinator = new TachiePresetCapabilityCoordinator(c =>
            new(c, ReferenceEquals(c, unstable) ? new P3ChangingConfig() : config,
                typeof(P3Config), () => new P3DirectFace(), () => true)))
        {
            var result = await coordinator.ScanAsync([unstable, good], () => true);
            Check(result[0].Capability == null && result[1].Capability?.Level == TachiePresetCapabilityLevel.Strong,
                "unstable Character configuration is a local failure, not a whole-batch cancellation");
        }
        var detached = new Character { Name = good.Name };
        using (var coordinator = new TachiePresetCapabilityCoordinator(c =>
            new(c, config, typeof(P3Config), () => new P3DirectFace(), () => true)))
        {
            var result = await coordinator.ScanAsync([good, detached], () => true);
            Check(result.Count == 1 && result[0].Capability?.Level == TachiePresetCapabilityLevel.Strong &&
                coordinator.Diagnostics.CharacterScans == 1,
                "equal detached same-name Character configurations share one discovery");
        }
        using (var coordinator = new TachiePresetCapabilityCoordinator(c =>
            new(c, ReferenceEquals(c, detached) ? new P3Config { Revision = 2 } : config,
                typeof(P3Config), () => new P3DirectFace(), () => true)))
        {
            var result = await coordinator.ScanAsync([good, detached], () => true);
            Check(result.Count == 1 && result[0].Capability == null && coordinator.Diagnostics.CharacterScans == 0,
                "different same-name configurations fail closed before discovery");
        }
        var singleton = new P3DirectFace();
        using (var coordinator = new TachiePresetCapabilityCoordinator(c =>
            new(c, config, typeof(P3Config), () => singleton, () => true)))
        {
            var result = await coordinator.ScanAsync([good], () => true);
            Check(result[0].Capability == null && singleton.Preset == "",
                "a plugin returning a shared face is rejected without probing it");
        }
        using (var coordinator = new TachiePresetCapabilityCoordinator(c =>
            new(c, config, typeof(P3Config), () => new P3ModernFace(), () => true)))
        {
            var cancelled = false;
            P3EditorFixture.AfterBind = coordinator.Dispose;
            try { await coordinator.ScanAsync([good], () => true); }
            catch (OperationCanceledException) { cancelled = true; }
            finally { P3EditorFixture.AfterBind = null; }
            Check(cancelled && coordinator.Diagnostics.ActiveEditors == 0 && P3EditorFixture.Handlers.Count == 0,
                "disposing while bound cancels and releases the editor before the task completes");
        }
        var root = CreateP3Fixtures();
        foreach (var kind in new[] { "Animation", "Psd" })
        {
            var name = kind == "Animation"
                ? "YukkuriMovieMaker.Plugin.Tachie.AnimationTachie.AnimationTachiePlugin"
                : "YukkuriMovieMaker.Plugin.Tachie.Psd.PsdTachiePlugin";
            var plugin = PluginLoader.TachiePlugins.Single(p => p.GetType().FullName == name);
            var cp = plugin.CreateCharacterParameter();
            foreach (var property in cp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType != typeof(string) || property.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0) continue;
                if (property.Name.Contains("Directory", StringComparison.Ordinal)) property.SetValue(cp, Path.Combine(root, "animation"));
                else if (property.Name.Contains("FilePath", StringComparison.Ordinal)) property.SetValue(cp, Path.Combine(root, "psd", "1layer.psd"));
            }
            var current = new Character { Name = "P3 guard " + kind, TachieType = plugin.GetType(),
                TachieCharacterParameter = cp, TachieDefaultFaceParameter = plugin.CreateFaceParameter() };
            using var coordinator = new TachiePresetCapabilityCoordinator();
            var result = (await coordinator.ScanAsync([current], () => true)).Single();
            Log("TP-P3 guard " + kind + " reason=" + result.UnavailableReason + " candidates=" +
                string.Join(",", result.Capability?.Candidates.Select(c => c.Label + ":" + c.Confidence) ?? []));
            Check(result.Capability is { Level: TachiePresetCapabilityLevel.Strong } && result.Capability.Candidates.Count == 2 &&
                result.Capability.Candidates.All(c => c.Confidence == TachiePresetCapabilityLevel.Strong && c.Label.StartsWith("CNWL_", StringComparison.Ordinal)),
                kind + " exposes exactly two named Strong candidates, never the Custom placeholder");
        }
        Check(Signature(timeline) == initial, "all guard cases leave Timeline unchanged");
        File.WriteAllText(Path.Combine(output, "tachie-preset-guards.json"), JsonSerializer.Serialize(new
        {
            schema = "YMM4-Template-Placer-Tachie-Preset-Guards/1", result = "PASS", host = "YMM4 4.55.1.1 Lite",
            sourceHead = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_SOURCE_HEAD"),
            checkoutTree = Environment.GetEnvironmentVariable("YMM4_TEMPLATE_PLACER_CHECKOUT_TREE"),
            runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Log("TACHIE_PRESET_GUARDS_P3=PASS");
        P3EditorFixture.LastControl = null; P3EditorFixture.LastFace = null;
    }
}
