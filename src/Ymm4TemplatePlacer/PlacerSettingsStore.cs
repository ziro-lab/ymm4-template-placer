using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ymm4TemplatePlacer;

public sealed partial class PlacerSettings
{
    public int Schema { get; set; } = 4;
    public List<LibraryEntry> Library { get; set; } = [];
    public long NextAssociationId { get; set; } = 1;
}

/// <summary>Small reference-only settings. Atomic writes; corrupt/external edits are never overwritten silently.</summary>
public sealed class PlacerSettingsStore
{
    private const int MaximumBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, MaxDepth = 32 };
    private readonly string path;
    private readonly string? legacyPath;
    private string? expectedDigest;
    private bool loaded;

    public static string PluginDirectory
    {
        get
        {
            var location = typeof(PlacerSettingsStore).Assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
                throw new InvalidOperationException("プラグインのインストール場所を取得できません。設定は移行・保存していません。");
            return Path.GetFullPath(Path.GetDirectoryName(location)
                ?? throw new InvalidOperationException("プラグインのインストールフォルダを取得できません。設定は移行・保存していません。"));
        }
    }

    public static string DefaultPath => Path.Combine(PluginDirectory, "Data", "settings-v04.json");
    public static string LegacyPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ymm4TemplatePlacer", "settings-v04.json");
    public bool MigratedLegacyOnLastLoad { get; private set; }

    public static PlacerSettingsStore CreateDefault() => new(DefaultPath, LegacyPath);

    public PlacerSettingsStore(string path) : this(path, null) { }

    internal PlacerSettingsStore(string path, string? legacyPath)
    {
        this.path = Path.GetFullPath(path);
        this.legacyPath = string.IsNullOrWhiteSpace(legacyPath) ? null : Path.GetFullPath(legacyPath);
        if (this.legacyPath != null && string.Equals(this.path, this.legacyPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Portable設定と旧設定のパスは分けてください。", nameof(legacyPath));
    }

    private static byte[]? ReadBytes(string target)
    {
        if (!File.Exists(target)) return null;
        using var stream = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumBytes) throw new InvalidDataException("設定ファイルが1 MiBを超えています。元ファイルは保持しています。");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private byte[]? ReadBytes() => ReadBytes(path);
    private static string? Digest(byte[]? bytes) => bytes == null ? null : Convert.ToHexString(SHA256.HashData(bytes));

    private static PlacerSettings DeserializeAndValidate(byte[]? bytes)
    {
        var result = bytes == null
            ? new PlacerSettings()
            : JsonSerializer.Deserialize<PlacerSettings>(bytes, Options) ?? throw new InvalidDataException("設定ファイルが空です。");
        SelectionPresetSettings.Upgrade(result);
        IntentPaletteSettings.Upgrade(result);
        ExpressionPresetSettings.Upgrade(result);
        Validate(result);
        return result;
    }

    private static byte[] CanonicalBytes(PlacerSettings settings) =>
        JsonSerializer.SerializeToUtf8Bytes(settings, Options);

    private static void WriteFileAtomically(string target, byte[] bytes, bool overwrite)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            File.Move(temporary, target, overwrite);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private void MigrateLegacy(byte[] legacyBytes)
    {
        if (legacyPath == null) throw new InvalidOperationException("旧設定の移行元がありません。");
        var legacyDigest = Digest(legacyBytes)!;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var saveLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (ReadBytes(path) != null)
            throw new InvalidOperationException("Portable設定が移行中に作成されました。既存のPortable設定を優先するため、旧設定は移行していません。ツールを開き直してください。");
        var currentLegacy = ReadBytes(legacyPath);
        if (Digest(currentLegacy) != legacyDigest)
            throw new InvalidOperationException("旧設定が移行中に変更されました。どちらも上書きしていません。ツールを開き直してください。");
        WriteFileAtomically(path, legacyBytes, overwrite: false);
        MigratedLegacyOnLastLoad = true;
    }

    public PlacerSettings Load()
    {
        MigratedLegacyOnLastLoad = false;

        // Once Portable settings exist they are the sole authority. The retained LocalAppData
        // file is only a legacy backup/migration source and never overrides or blocks Portable.
        var portableBytes = ReadBytes();
        if (portableBytes != null)
        {
            var portable = DeserializeAndValidate(portableBytes);
            expectedDigest = Digest(portableBytes);
            loaded = true;
            return portable;
        }

        if (legacyPath != null)
        {
            var legacyBytes = ReadBytes(legacyPath);
            if (legacyBytes != null)
            {
                var legacy = DeserializeAndValidate(legacyBytes);
                MigrateLegacy(legacyBytes);
                var migratedBytes = ReadBytes() ?? throw new IOException("Portable設定の移行結果を読み直せませんでした。旧設定は保持しています。");
                var migrated = DeserializeAndValidate(migratedBytes);
                if (!CanonicalBytes(migrated).SequenceEqual(CanonicalBytes(legacy)))
                    throw new IOException("Portable設定の移行結果が旧設定と一致しません。旧設定は保持しています。");
                expectedDigest = Digest(migratedBytes);
                loaded = true;
                return migrated;
            }
        }

        var result = DeserializeAndValidate(null);
        expectedDigest = null;
        loaded = true;
        return result;
    }

    public static PlacerSettings Copy(PlacerSettings settings) =>
        JsonSerializer.Deserialize<PlacerSettings>(JsonSerializer.SerializeToUtf8Bytes(settings, Options), Options)!;

    public static void Validate(PlacerSettings settings)
    {
        if (settings.Schema != 4 || settings.Library == null || settings.Library.Count > 2048 || settings.NextAssociationId < 1 ||
            !Enum.IsDefined(settings.ExpressionSourceMode))
            throw new InvalidDataException("未対応または不正なプラグイン設定です。元ファイルは保持しています。");
        if (settings.Library.Any(x => x == null || x.Id == Guid.Empty || x.Source == null || x.Source.Name == null || x.Source.PathJson == null || string.IsNullOrWhiteSpace(x.DisplayName) || x.DisplayName.Length > 256) ||
            settings.Library.Select(x => x.Id).Distinct().Count() != settings.Library.Count)
            throw new InvalidDataException("テンプレート管理のID・参照・表示名が不正です。");
        if (settings.Presentation == null) throw new InvalidDataException("配置パレットの表示設定が空です。");
        settings.Presentation.Validate();
        PaletteSettings.Validate(settings);
        ExpressionPresetSettings.Validate(settings);
        TachiePresetLearnedAdapterSettings.Validate(settings);
        TachiePresetSourceSettings.Validate(settings);
        SelectionPresetSettings.Validate(settings);
        IntentPaletteSettings.Validate(settings);
    }

    public void Save(PlacerSettings settings)
    {
        if (!loaded) throw new InvalidOperationException("設定の読み込みに成功していないため保存しません。元ファイルを確認してツールを開き直してください。");
        Validate(settings);
        SaveBytes(JsonSerializer.SerializeToUtf8Bytes(settings, Options));
    }

    internal void SaveExpressionSourceMode(ExpressionSourceMode mode)
    {
        if (!loaded) throw new InvalidOperationException("設定の読み込みに成功していないため表示モードを保存しません。");
        if (!Enum.IsDefined(mode)) throw new InvalidOperationException("表情の表示モードが不正です。");
        var bytes = ReadBytes();
        if (Digest(bytes) != expectedDigest)
            throw new InvalidOperationException("別のツールまたはYMM4で設定が変更されました。ツールを開き直してください。外部変更は上書きしていません。");
        var stored = DeserializeAndValidate(bytes);
        stored.ExpressionSourceMode = mode;
        Validate(stored);
        SaveBytes(JsonSerializer.SerializeToUtf8Bytes(stored, Options));
    }

    private void SaveBytes(byte[] bytes)
    {
        if (bytes.Length > MaximumBytes) throw new InvalidOperationException("設定が1 MiBを超えるため保存できません。");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Cooperating Tool instances cannot pass the digest check concurrently.
        using var saveLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (Digest(ReadBytes()) != expectedDigest)
            throw new InvalidOperationException("別のツールまたはYMM4で設定が変更されました。ツールを開き直してください。外部変更は上書きしていません。");
        WriteFileAtomically(path, bytes, overwrite: true);
        expectedDigest = Digest(bytes);
    }
}
