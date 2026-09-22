using System.IO;
using System.Text.Json;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyPortableSettingsStorage()
    {
        stage = "Portable Settings migration and conflict safety";
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
                File.ReadAllBytes(legacy).SequenceEqual(legacyBefore) &&
                File.Exists(store.MigrationReceiptPathForProof),
                "PORTABLE legacy-only load copies exact validated bytes into Data, records a baseline receipt and preserves the old file");

            loaded.Presentation = loaded.Presentation with { FixedColumns = 6 };
            store.Save(loaded);
            var portableAfter = File.ReadAllBytes(portable);
            Assert(!portableAfter.SequenceEqual(a) && File.ReadAllBytes(legacy).SequenceEqual(legacyBefore) &&
                new PlacerSettingsStore(portable, legacy).Load().Presentation.FixedColumns == 6,
                "PORTABLE edits advance only the portable file; unchanged retained legacy data does not become a false conflict");

            var samePortable = Path.Combine(root, "same", "Data", "settings-v04.json");
            var sameLegacy = Path.Combine(root, "same", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(samePortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(sameLegacy)!);
            File.WriteAllBytes(samePortable, a); File.WriteAllBytes(sameLegacy, a);
            var sameStore = new PlacerSettingsStore(samePortable, sameLegacy);
            var sameLoaded = sameStore.Load();
            Assert(File.Exists(sameStore.MigrationReceiptPathForProof) && sameLoaded.Presentation.FixedColumns == 4,
                "PORTABLE identical pre-existing files establish a migration baseline without rewriting either settings file");
            sameLoaded.Presentation = sameLoaded.Presentation with { FixedColumns = 5 };
            sameStore.Save(sameLoaded);
            Assert(new PlacerSettingsStore(samePortable, sameLegacy).Load().Presentation.FixedColumns == 5,
                "PORTABLE an established identical baseline allows later portable-only edits while the retained legacy backup stays unchanged");

            var divergentPortable = Path.Combine(root, "divergent", "Data", "settings-v04.json");
            var divergentLegacy = Path.Combine(root, "divergent", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(divergentPortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(divergentLegacy)!);
            File.WriteAllBytes(divergentPortable, a); File.WriteAllBytes(divergentLegacy, b);
            var divergentPortableBefore = File.ReadAllBytes(divergentPortable);
            var divergentLegacyBefore = File.ReadAllBytes(divergentLegacy);
            var divergentStore = new PlacerSettingsStore(divergentPortable, divergentLegacy);
            Assert(Rejected(() => divergentStore.Load()) &&
                File.ReadAllBytes(divergentPortable).SequenceEqual(divergentPortableBefore) &&
                File.ReadAllBytes(divergentLegacy).SequenceEqual(divergentLegacyBefore) &&
                !File.Exists(divergentStore.MigrationReceiptPathForProof),
                "PORTABLE two different valid settings files fail closed and preserve both instead of choosing one");

            var changedPortable = Path.Combine(root, "legacy-changed", "Data", "settings-v04.json");
            var changedLegacy = Path.Combine(root, "legacy-changed", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(changedLegacy)!);
            File.WriteAllBytes(changedLegacy, a);
            var changedStore = new PlacerSettingsStore(changedPortable, changedLegacy);
            changedStore.Load();
            var portableStable = File.ReadAllBytes(changedPortable);
            File.WriteAllBytes(changedLegacy, b);
            var changedLegacyBytes = File.ReadAllBytes(changedLegacy);
            Assert(Rejected(() => new PlacerSettingsStore(changedPortable, changedLegacy).Load()) &&
                File.ReadAllBytes(changedPortable).SequenceEqual(portableStable) &&
                File.ReadAllBytes(changedLegacy).SequenceEqual(changedLegacyBytes),
                "PORTABLE a retained legacy file changed after migration is treated as a real divergence and neither side is overwritten");

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
                "PORTABLE corrupt legacy data is rejected before migration and no portable file is created");

            var lockedPortable = Path.Combine(root, "locked", "Data", "settings-v04.json");
            var lockedLegacy = Path.Combine(root, "locked", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(lockedPortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(lockedLegacy)!);
            File.WriteAllBytes(lockedLegacy, a);
            var lockedStore = new PlacerSettingsStore(lockedPortable, lockedLegacy);
            using (var saveLock = new FileStream(lockedPortable + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                Assert(Rejected(() => lockedStore.Load()) && !File.Exists(lockedPortable) &&
                    !File.Exists(lockedStore.MigrationReceiptPathForProof) && File.ReadAllBytes(lockedLegacy).SequenceEqual(a),
                    "PORTABLE migration respects the real cross-instance lock and performs zero partial migration");

            var receiptPortable = Path.Combine(root, "bad-receipt", "Data", "settings-v04.json");
            var receiptLegacy = Path.Combine(root, "bad-receipt", "LocalAppData", "settings-v04.json");
            Directory.CreateDirectory(Path.GetDirectoryName(receiptPortable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(receiptLegacy)!);
            File.WriteAllBytes(receiptPortable, a); File.WriteAllBytes(receiptLegacy, a);
            var receiptStore = new PlacerSettingsStore(receiptPortable, receiptLegacy);
            File.WriteAllText(receiptStore.MigrationReceiptPathForProof, "not-a-valid-receipt");
            Assert(Rejected(() => receiptStore.Load()) &&
                File.ReadAllBytes(receiptPortable).SequenceEqual(a) && File.ReadAllBytes(receiptLegacy).SequenceEqual(a),
                "PORTABLE corrupt migration metadata fails closed while preserving both settings files");

            Log("PORTABLE_SETTINGS=PASS");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
