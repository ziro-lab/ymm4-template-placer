using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyPortableSettingsStorage()
    {
        stage = "Portable Settings migration and authority safety";
        var root = Path.Combine(output, "portable-settings-proof");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        static bool Rejected(Action action)
        {
            try { action(); return false; }
            catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException or JsonException)
            { return true; }
        }

        byte[] ValidBytes(string name, int columns)
        {
            var seed = Path.Combine(root, name + "-seed.json");
            if (File.Exists(seed)) File.Delete(seed);
            var store = new PlacerSettingsStore(seed);
            var settings = store.Load();
            settings.Presentation = settings.Presentation with { FixedColumns = columns };
            store.Save(settings);
            return File.ReadAllBytes(seed);
        }

        var pluginRoot = Path.GetFullPath(Path.GetDirectoryName(typeof(PluginEntry).Assembly.Location)!);
        var expectedPortable = Path.Combine(pluginRoot, "Data", "settings-v04.json");
        Assert(Path.GetFullPath(PlacerSettingsStore.DefaultPath) == Path.GetFullPath(expectedPortable) &&
            !string.Equals(PlacerSettingsStore.DefaultPath, PlacerSettingsStore.LegacyPath, StringComparison.OrdinalIgnoreCase),
            "PORTABLE settings path is plugin-root/Data and remains distinct from the legacy LocalAppData path");

        var a = ValidBytes("a", 4);
        var b = ValidBytes("b", 7);

        try
        {
            var portable = Path.Combine(root, "migration", "Data", "settings-v04.json");
            var legacy = Path.Combine(root, "migration", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
            File.WriteAllBytes(legacy, a);
            var legacyBefore = File.ReadAllBytes(legacy);
            var store = new PlacerSettingsStore(portable, legacy);
            var loaded = store.Load();
            Assert(store.MigratedLegacyOnLastLoad && File.Exists(portable) &&
                File.ReadAllBytes(portable).SequenceEqual(a) &&
                File.ReadAllBytes(legacy).SequenceEqual(legacyBefore),
                "PORTABLE legacy-only load copies exact validated bytes into Data and preserves the old file");

            loaded.Presentation = loaded.Presentation with { FixedColumns = 6 };
            store.Save(loaded);
            Assert(new PlacerSettingsStore(portable, legacy).Load().Presentation.FixedColumns == 6 &&
                File.ReadAllBytes(legacy).SequenceEqual(legacyBefore),
                "PORTABLE later edits advance only the portable file while the retained legacy backup stays untouched");

            var divergentPortable = Path.Combine(root, "divergent", "Data", "settings-v04.json");
            var divergentLegacy = Path.Combine(root, "divergent", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(divergentPortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(divergentLegacy)!);
            File.WriteAllBytes(divergentPortable, b); File.WriteAllBytes(divergentLegacy, a);
            var divergentLegacyBefore = File.ReadAllBytes(divergentLegacy);
            var divergentStore = new PlacerSettingsStore(divergentPortable, divergentLegacy);
            Assert(divergentStore.Load().Presentation.FixedColumns == 7 &&
                File.ReadAllBytes(divergentPortable).SequenceEqual(b) &&
                File.ReadAllBytes(divergentLegacy).SequenceEqual(divergentLegacyBefore),
                "PORTABLE when valid new and old settings differ, the portable settings are authoritative and legacy is left untouched");

            File.WriteAllBytes(divergentLegacy, b);
            Assert(new PlacerSettingsStore(divergentPortable, divergentLegacy).Load().Presentation.FixedColumns == 7,
                "PORTABLE later legacy changes never override or block an existing portable settings file");

            File.WriteAllText(divergentLegacy, "{not-json");
            Assert(new PlacerSettingsStore(divergentPortable, divergentLegacy).Load().Presentation.FixedColumns == 7,
                "PORTABLE corrupt legacy data is ignored when a valid portable settings file already exists");

            var corruptPortable = Path.Combine(root, "corrupt-portable", "Data", "settings-v04.json");
            var goodLegacy = Path.Combine(root, "corrupt-portable", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(corruptPortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(goodLegacy)!);
            File.WriteAllText(corruptPortable, "{not-json");
            File.WriteAllBytes(goodLegacy, a);
            var corruptPortableBytes = File.ReadAllBytes(corruptPortable);
            Assert(Rejected(() => new PlacerSettingsStore(corruptPortable, goodLegacy).Load()) &&
                File.ReadAllBytes(corruptPortable).SequenceEqual(corruptPortableBytes) &&
                File.ReadAllBytes(goodLegacy).SequenceEqual(a),
                "PORTABLE a corrupt authoritative portable file never silently falls back to a valid legacy file");

            var corruptLegacy = Path.Combine(root, "corrupt-legacy", "LocalAppData", "settings-v04.json");
            var absentPortable = Path.Combine(root, "corrupt-legacy", "Data", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(corruptLegacy)!);
            File.WriteAllText(corruptLegacy, "{not-json");
            var corruptLegacyBytes = File.ReadAllBytes(corruptLegacy);
            Assert(Rejected(() => new PlacerSettingsStore(absentPortable, corruptLegacy).Load()) &&
                !File.Exists(absentPortable) && File.ReadAllBytes(corruptLegacy).SequenceEqual(corruptLegacyBytes),
                "PORTABLE corrupt legacy data is rejected before first migration and no portable file is created");

            var lockedPortable = Path.Combine(root, "locked", "Data", "settings-v04.json");
            var lockedLegacy = Path.Combine(root, "locked", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(lockedPortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(lockedLegacy)!);
            File.WriteAllBytes(lockedLegacy, a);
            var lockedStore = new PlacerSettingsStore(lockedPortable, lockedLegacy);
            using (var saveLock = new FileStream(lockedPortable + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                Assert(Rejected(() => lockedStore.Load()) && !File.Exists(lockedPortable) &&
                    File.ReadAllBytes(lockedLegacy).SequenceEqual(a),
                    "PORTABLE first migration respects the real cross-instance lock and performs zero partial migration");

            // Exercise the actual default paths inside the real native YMM4 proof host.
            var actualPortable = PlacerSettingsStore.DefaultPath;
            var actualLegacy = PlacerSettingsStore.LegacyPath;
            var actualPortableBytes = File.Exists(actualPortable) ? File.ReadAllBytes(actualPortable) : null;
            var actualLegacyBytes = File.Exists(actualLegacy) ? File.ReadAllBytes(actualLegacy) : null;
            try
            {
                if (File.Exists(actualPortable)) File.Delete(actualPortable);
                Directory.CreateDirectory(Path.GetDirectoryName(actualLegacy)!);
                File.WriteAllBytes(actualLegacy, a);

                var actualStore = PlacerSettingsStore.CreateDefault();
                Assert(actualStore.Load().Presentation.FixedColumns == 4 &&
                    actualStore.MigratedLegacyOnLastLoad &&
                    File.Exists(actualPortable) && File.ReadAllBytes(actualPortable).SequenceEqual(a),
                    "PORTABLE real YMM4 CreateDefault migrates the real LocalAppData path into the real plugin/Data path");

                File.WriteAllBytes(actualPortable, b);
                File.WriteAllBytes(actualLegacy, a);
                Assert(PlacerSettingsStore.CreateDefault().Load().Presentation.FixedColumns == 7,
                    "PORTABLE real YMM4 CreateDefault reopens from portable settings and prefers new data when old differs");
            }
            finally
            {
                if (actualPortableBytes == null) File.Delete(actualPortable);
                else { Directory.CreateDirectory(Path.GetDirectoryName(actualPortable)!); File.WriteAllBytes(actualPortable, actualPortableBytes); }

                if (actualLegacyBytes == null) File.Delete(actualLegacy);
                else { Directory.CreateDirectory(Path.GetDirectoryName(actualLegacy)!); File.WriteAllBytes(actualLegacy, actualLegacyBytes); }
            }

            Log("PORTABLE_SETTINGS=PASS");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
