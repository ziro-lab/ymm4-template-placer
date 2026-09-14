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
    private string? expectedDigest;
    private bool loaded;
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ymm4TemplatePlacer", "settings-v04.json");
    public PlacerSettingsStore(string path) { this.path = Path.GetFullPath(path); }
    private byte[]? ReadBytes()
    {
        if (!File.Exists(path)) return null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumBytes) throw new InvalidDataException("設定ファイルが1 MiBを超えています。元ファイルは保持しています。");
        var bytes = new byte[(int)stream.Length]; stream.ReadExactly(bytes); return bytes;
    }
    private static string? Digest(byte[]? bytes) => bytes == null ? null : Convert.ToHexString(SHA256.HashData(bytes));
    public PlacerSettings Load()
    {
        var bytes = ReadBytes();
        var result = bytes == null ? new PlacerSettings() : JsonSerializer.Deserialize<PlacerSettings>(bytes, Options) ?? throw new InvalidDataException("設定ファイルが空です。");
        Validate(result); expectedDigest = Digest(bytes); loaded = true; return result;
    }
    public static PlacerSettings Copy(PlacerSettings settings) =>
        JsonSerializer.Deserialize<PlacerSettings>(JsonSerializer.SerializeToUtf8Bytes(settings, Options), Options)!;
    public static void Validate(PlacerSettings settings)
    {
        if (settings.Schema != 4 || settings.Library == null || settings.Library.Count > 2048 || settings.NextAssociationId < 1)
            throw new InvalidDataException("未対応または不正なPlugin設定です。元ファイルは保持しています。");
        if (settings.Library.Any(x => x == null || x.Id == Guid.Empty || x.Source == null || x.Source.Name == null || x.Source.PathJson == null || string.IsNullOrWhiteSpace(x.DisplayName) || x.DisplayName.Length > 256) ||
            settings.Library.Select(x => x.Id).Distinct().Count() != settings.Library.Count)
            throw new InvalidDataException("Library ID・参照・表示名が不正です。");
        PaletteSettings.Validate(settings);
        ExpressionPresetSettings.Validate(settings);
    }
    public void Save(PlacerSettings settings)
    {
        if (!loaded) throw new InvalidOperationException("設定の読み込みに成功していないため保存しません。元ファイルを確認してToolを開き直してください。");
        Validate(settings);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(settings, Options);
        if (bytes.Length > MaximumBytes) throw new InvalidOperationException("設定が1 MiBを超えるため保存できません。");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Cooperating Tool instances cannot pass the digest check concurrently.
        using var saveLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (Digest(ReadBytes()) != expectedDigest) throw new InvalidOperationException("別のToolまたはYMM4で設定が変更されました。Toolを開き直してください。外部変更は上書きしていません。");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes); stream.Flush(true); }
            File.Move(temporary, path, true); expectedDigest = Digest(bytes);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
