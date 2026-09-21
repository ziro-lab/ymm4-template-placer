using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Ymm4TemplatePlacer;

// A versioned public-state projection, not a serializer and not a complete model of
// arbitrary plugins. Incomplete traversal must never become an apparently valid hash.
internal static class TachiePresetPublicState
{
    internal const int MaxDepth = 8;
    internal const int MaxNodes = 4096;
    internal const int MaxMembers = 128;
    internal const int MaxCollection = 256;
    internal const int MaxCharacters = 262144;

    public static string Hash(object value, CancellationToken token = default)
    {
        RequireUiThread();
        var capture = new Capture(token);
        capture.Write(value, 0);
        return "public-v1:" + Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(capture.Text))).ToLowerInvariant();
    }

    public static string? TryHash(object value, CancellationToken token = default)
    {
        try { return Hash(value, token); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return null; }
    }

    internal static void RequireUiThread()
    {
        if (Application.Current?.Dispatcher is not { } dispatcher || !dispatcher.CheckAccess())
            throw new InvalidOperationException("立ち絵プリセットの確認はYMM4のUIスレッドで実行してください。");
    }

    private sealed class Capture(CancellationToken token)
    {
        private readonly StringBuilder text = new();
        private readonly HashSet<object> ancestors = new(ReferenceEqualityComparer.Instance);
        private int nodes;
        public string Text => text.ToString();

        private void Add(string value)
        {
            token.ThrowIfCancellationRequested();
            if (value.Length > MaxCharacters || text.Length + value.Length + 16 > MaxCharacters)
                throw new InvalidOperationException("立ち絵の公開状態が確認上限を超えています。");
            // Length framing also distinguishes malicious/delimiter-containing labels.
            text.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
        }

        public void Write(object? value, int depth)
        {
            token.ThrowIfCancellationRequested();
            if (++nodes > MaxNodes || depth > MaxDepth)
                throw new InvalidOperationException("立ち絵の公開状態が深さまたは要素数の上限を超えています。");
            if (value == null) { Add("null"); return; }
            var type = value.GetType();
            Add(type.FullName ?? type.Name);
            if (value is string s) { Add(s); return; }
            if (value is char ch) { Add(((int)ch).ToString(CultureInfo.InvariantCulture)); return; }
            if (value is bool b) { Add(b ? "1" : "0"); return; }
            if (value is Enum en) { Add(en.ToString("D")); return; }
            if (value is float f) { Add(f.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is double d) { Add(d.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal)
            { Add(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture)); return; }
            if (value is Guid guid) { Add(guid.ToString("D")); return; }
            if (value is TimeSpan span) { Add(span.Ticks.ToString(CultureInfo.InvariantCulture)); return; }
            if (value is DateTime date)
            { Add(date.Ticks.ToString(CultureInfo.InvariantCulture)); Add(date.Kind.ToString()); return; }
            if (value is DateTimeOffset offset)
            { Add(offset.Ticks.ToString(CultureInfo.InvariantCulture)); Add(offset.Offset.Ticks.ToString(CultureInfo.InvariantCulture)); return; }
            if (value is Type runtimeType) { Add(runtimeType.AssemblyQualifiedName ?? runtimeType.FullName ?? runtimeType.Name); return; }
            if (value is DependencyObject or Delegate or MemberInfo or IntPtr or UIntPtr)
                throw new InvalidOperationException("UI・実行オブジェクトを立ち絵の状態識別子にはできません。");

            if (!type.IsValueType && !ancestors.Add(value))
                throw new InvalidOperationException("立ち絵の公開状態に循環参照があります。");
            try
            {
                if (value is IDictionary dictionary)
                {
                    if (dictionary.Count > MaxCollection) throw TooMany();
                    var keys = new List<string>();
                    foreach (var key in dictionary.Keys)
                    {
                        token.ThrowIfCancellationRequested();
                        if (keys.Count >= MaxCollection || key is not string name) throw TooMany();
                        keys.Add(name);
                    }
                    foreach (var key in keys.Order(StringComparer.Ordinal)) { Add(key); Write(dictionary[key], depth + 1); }
                    Add("end-dictionary");
                    return;
                }
                if (value is IEnumerable sequence)
                {
                    var count = 0;
                    foreach (var entry in sequence)
                    {
                        token.ThrowIfCancellationRequested();
                        if (++count > MaxCollection) throw TooMany();
                        Write(entry, depth + 1);
                    }
                    Add("end-sequence");
                    return;
                }
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.GetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
                    .OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
                if (properties.Length + fields.Length > MaxMembers) throw TooMany();
                foreach (var property in properties)
                {
                    token.ThrowIfCancellationRequested();
                    Add("property:" + property.DeclaringType?.FullName + "." + property.Name);
                    Write(property.GetValue(value), depth + 1);
                }
                foreach (var field in fields)
                {
                    token.ThrowIfCancellationRequested();
                    Add("field:" + field.DeclaringType?.FullName + "." + field.Name);
                    Write(field.GetValue(value), depth + 1);
                }
                Add("end-object");
            }
            finally { if (!type.IsValueType) ancestors.Remove(value); }
        }

        private static InvalidOperationException TooMany() =>
            new("立ち絵の公開状態を省略せず確認できません。要素数または辞書キーの形式が未対応です。");
    }
}
