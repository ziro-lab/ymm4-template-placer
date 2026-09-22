using System.Reflection;
using System.Windows.Threading;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Tachie;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace Ymm4TemplatePlacer;

// These objects are temporary UI-thread inputs. They never enter row preparation or cache values.
internal sealed class TachiePresetProbeTarget
{
    public Character Character { get; }
    public object Configuration { get; }
    public TachiePresetCapabilityFingerprint Fingerprint { get; }
    private readonly Func<object> createFace;
    private readonly Func<TachieFaceItem>? createItem;
    private readonly Func<bool> current;
    private readonly object seedFace;
    internal string CharacterParameterRuntimeType =>
        Configuration.GetType().AssemblyQualifiedName ?? Configuration.GetType().FullName ?? Configuration.GetType().Name;

    internal TachiePresetProbeTarget(Character character, object configuration, Type pluginType,
        Func<object> createFace, Func<bool> current, Func<TachieFaceItem>? createItem = null)
    {
        TachiePresetPublicState.RequireUiThread();
        Character = character;
        Configuration = configuration;
        this.createFace = createFace;
        this.createItem = createItem;
        this.current = current;
        seedFace = CreateRawFace();
        Fingerprint = new(
            typeof(Character).Assembly.GetName().Version + ":" + typeof(Character).Module.ModuleVersionId,
            character.Name,
            TachiePresetPublicState.Hash(configuration),
            pluginType.AssemblyQualifiedName ?? pluginType.FullName ?? pluginType.Name,
            seedFace.GetType().AssemblyQualifiedName ?? seedFace.GetType().FullName ?? seedFace.GetType().Name,
            pluginType.Module.ModuleVersionId);
    }

    public static TachiePresetProbeTarget Resolve(Character character)
    {
        TachiePresetPublicState.RequireUiThread();
        var type = character.TachieType;
        var matches = PluginLoader.TachiePlugins.Where(p => p.GetType() == type).Take(2).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("使用中の立ち絵プラグインを一意に特定できません。");
        var plugin = matches[0];
        var configuration = character.TachieCharacterParameter
            ?? throw new InvalidOperationException("使用中の立ち絵設定がありません。");
        if (configuration.GetType() != plugin.CreateCharacterParameter().GetType())
            throw new InvalidOperationException("立ち絵プラグインとキャラクター設定の種類が一致していません。");
        return new(character, configuration, plugin.GetType(), () => plugin.CreateFaceParameter(),
            () => character.TachieType == type && ReferenceEquals(character.TachieCharacterParameter, configuration) &&
                  PluginLoader.TachiePlugins.Count(p => p.GetType() == type) == 1 &&
                  PluginLoader.TachiePlugins.Any(p => ReferenceEquals(p, plugin)),
            () => new TachieFaceItem(character));
    }

    public bool IsCurrent(CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        token.ThrowIfCancellationRequested();
        return current() && Character.Name == Fingerprint.CharacterIdentity &&
               TachiePresetPublicState.Hash(Configuration, token) == Fingerprint.CharacterConfigIdentity;
    }

    private object CreateRawFace()
    {
        if (createItem != null)
        {
            var item = createItem() ?? throw new InvalidOperationException("表情アイテムを作成できません。");
            return item.TachieFaceParameter ?? throw new InvalidOperationException("表情アイテムの表情パラメータを作成できません。");
        }
        return createFace() ?? throw new InvalidOperationException("表情パラメータを作成できません。");
    }

    private object PrepareFreshFace(object face, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (face.GetType() != seedFace.GetType() || ReferenceEquals(face, seedFace) ||
            ReferenceEquals(face, Configuration) || ReferenceEquals(face, Character.TachieDefaultFaceParameter))
            throw new InvalidOperationException("立ち絵プラグインが独立した表情パラメータを返しませんでした。");
        var properties = Configuration.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (properties.Length > TachiePresetPublicState.MaxMembers)
            throw new InvalidOperationException("立ち絵設定のプロパティ数が確認上限を超えています。");
        foreach (var source in properties)
        {
            token.ThrowIfCancellationRequested();
            if (source.PropertyType != typeof(string) || source.GetMethod?.IsPublic != true ||
                source.GetIndexParameters().Length != 0 ||
                !(source.Name.EndsWith("FilePath", StringComparison.Ordinal) ||
                  source.Name.EndsWith("Directory", StringComparison.Ordinal) ||
                  source.Name.EndsWith("DirectoryPath", StringComparison.Ordinal))) continue;
            var destination = face.GetType().GetProperty(source.Name, BindingFlags.Instance | BindingFlags.Public);
            if (destination?.PropertyType == typeof(string) && destination.SetMethod?.IsPublic == true &&
                destination.GetIndexParameters().Length == 0)
                destination.SetValue(face, source.GetValue(Configuration));
        }
        return face;
    }

    public object CreateFreshFace(CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        return PrepareFreshFace(CreateRawFace(), token);
    }

    public TachieFaceItem CreateFreshItem(CancellationToken token)
    {
        TachiePresetPublicState.RequireUiThread();
        token.ThrowIfCancellationRequested();
        if (createItem != null)
        {
            var item = createItem() ?? throw new InvalidOperationException("表情アイテムを作成できません。");
            var face = item.TachieFaceParameter ?? throw new InvalidOperationException("表情アイテムの表情パラメータを作成できません。");
            PrepareFreshFace(face, token);
            return item;
        }
        var created = CreateFreshFace(token);
        if (created is not ITachieFaceParameter parameter)
            throw new InvalidOperationException("表情パラメータがYMM4の立ち絵表情契約と一致しません。");
        return new TachieFaceItem(Character) { TachieFaceParameter = parameter };
    }
}

internal sealed record TachiePresetCharacterCapability(
    string CharacterIdentity, TachiePresetCapabilityResult? Capability, string? UnavailableReason);

internal sealed class TachiePresetCapabilityDiagnostics
{
    public int CharacterScans, CacheHits, CancelledScans, StaleDiscards;
    public int EditorsOpened, EditorsCleared, CleanupFailures, ActiveEditors, PeakEditors;
    public TachiePresetCapabilityDiagnostics Snapshot() => (TachiePresetCapabilityDiagnostics)MemberwiseClone();
}

// The root supplies the active source/Timeline/generation predicate and owns publication.
// P3 returns immutable results; P4 connects those results to the existing row/load coordinator.
internal sealed class TachiePresetCapabilityCoordinator : IDisposable
{
    internal const int MaxCharacters = 256;
    internal const int MaxCandidates = 128;
    private readonly Func<Character, TachiePresetProbeTarget> resolve;
    private readonly Func<TachiePresetProbeTarget, TachiePresetRouteDescriptor?> learnedRoute;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<TachiePresetCapabilityFingerprint, TachiePresetCapabilityResult> cache = new();
    private readonly TachiePresetCapabilityDiagnostics diagnostics = new();
    private CancellationTokenSource? cancellation;
    private long generation;
    private int operations;
    private bool disposed;
    public TachiePresetCapabilityDiagnostics Diagnostics => diagnostics.Snapshot();

    public TachiePresetCapabilityCoordinator() : this(TachiePresetProbeTarget.Resolve, null) { }
    internal TachiePresetCapabilityCoordinator(
        Func<Character, TachiePresetProbeTarget> resolve,
        Func<TachiePresetProbeTarget, TachiePresetRouteDescriptor?>? learnedRoute = null)
    {
        this.resolve = resolve;
        this.learnedRoute = learnedRoute ?? (_ => null);
    }

    public void Cancel()
    {
        TachiePresetPublicState.RequireUiThread();
        generation++;
        cancellation?.Cancel(); // The owning operation disposes its CTS after cleanup, never here.
    }

    public void Invalidate()
    {
        Cancel();
        cache.Clear();
    }

    public async Task<IReadOnlyList<TachiePresetCharacterCapability>> ScanAsync(
        IReadOnlyList<Character> characters, Func<bool> scopeIsCurrent, CancellationToken token = default)
    {
        TachiePresetPublicState.RequireUiThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(characters);
        ArgumentNullException.ThrowIfNull(scopeIsCurrent);
        Cancel();
        token.ThrowIfCancellationRequested();
        if (!scopeIsCurrent()) return Array.Empty<TachiePresetCharacterCapability>();
        if (characters.Count > 50000) throw new InvalidOperationException("確認対象のキャラクター数が上限を超えています。");
        using var own = CancellationTokenSource.CreateLinkedTokenSource(token);
        cancellation = own;
        var request = generation;
        var acquired = false;
        operations++;
        void EnsureCurrent()
        {
            TachiePresetPublicState.RequireUiThread();
            own.Token.ThrowIfCancellationRequested();
            if (disposed || request != generation || !scopeIsCurrent())
            {
                diagnostics.StaleDiscards++;
                throw new OperationCanceledException("立ち絵プリセットの確認対象が変わりました。", own.Token);
            }
        }
        try
        {
            await gate.WaitAsync(own.Token);
            acquired = true;
            EnsureCurrent();
            var groups = characters.Distinct((IEqualityComparer<Character>)ReferenceEqualityComparer.Instance)
                .GroupBy(c => c.Name, StringComparer.Ordinal).ToArray();
            if (groups.Length > MaxCharacters)
                throw new InvalidOperationException("同時に確認できるキャラクターは256種類までです。");
            var results = new List<TachiePresetCharacterCapability>();
            var pending = new List<(TachiePresetCapabilityFingerprint Key, TachiePresetCapabilityResult Value)>();
            var targetsToValidate = new List<TachiePresetProbeTarget>();
            foreach (var group in groups)
            {
                EnsureCurrent();
                await Dispatcher.Yield(DispatcherPriority.Background);
                EnsureCurrent();
                try
                {
                    var targets = group.Select(resolve).ToArray();
                    var target = targets[0];
                    if (targets.Any(t => t.Fingerprint != target.Fingerprint))
                        throw new InvalidOperationException("同名キャラクターの立ち絵設定が一致していません。");
                    // Detached same-name objects are acceptable only with equal exact configuration.
                    if (targets.Any(t => !t.IsCurrent(own.Token)))
                        throw new OperationCanceledException("キャラクターの立ち絵設定が変わりました。", own.Token);
                    targetsToValidate.AddRange(targets);
                    TachiePresetCapabilityResult result;
                    if (cache.TryGetValue(target.Fingerprint, out var cached))
                    {
                        result = cached;
                        diagnostics.CacheHits++;
                    }
                    else
                    {
                        diagnostics.CharacterScans++;
                        result = await TachiePresetDiscovery.DiscoverAsync(
                            target, diagnostics, EnsureCurrent, own.Token, learnedRoute(target));
                        pending.Add((target.Fingerprint, result));
                    }
                    EnsureCurrent();
                    results.Add(new(group.Key, result, result.UnavailableReason));
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    EnsureCurrent();
                    results.Add(new(group.Key, null, LocalReason(ex)));
                }
            }
            EnsureCurrent();
            if (targetsToValidate.Any(t => !t.IsCurrent(own.Token)))
                throw new OperationCanceledException("確認中に立ち絵設定が変わりました。", own.Token);
            EnsureCurrent();
            foreach (var (key, value) in pending)
            {
                if (cache.Count >= MaxCharacters && !cache.ContainsKey(key)) cache.Clear();
                cache[key] = value;
            }
            return results.AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            diagnostics.CancelledScans++;
            throw;
        }
        finally
        {
            if (acquired) gate.Release();
            if (ReferenceEquals(cancellation, own)) cancellation = null;
            operations--;
            if (disposed && operations == 0) gate.Dispose();
        }
    }

    internal static string LocalReason(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        return message.Length <= 360 ? message : message[..360] + "…";
    }

    public void Dispose()
    {
        TachiePresetPublicState.RequireUiThread();
        if (disposed) return;
        disposed = true;
        Cancel();
        cache.Clear();
        if (operations == 0) gate.Dispose();
    }
}
