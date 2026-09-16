using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

internal static class RuntimeAudioProbe
{
    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 3) return;
            var ymm4Dir = Path.GetFullPath(args[1]);
            var mainOutput = Path.GetFullPath(args[2]);
            var output = Path.Combine(Path.GetDirectoryName(mainOutput)!, "runtime-audio-probe.txt");

            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var candidate = Path.Combine(ymm4Dir, name.Name + ".dll");
                if (!File.Exists(candidate)) return null;
                try { return AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate); } catch { return null; }
            };

            var assemblies = new List<Assembly>();
            foreach (var path in Directory.EnumerateFiles(ymm4Dir, "*.dll").Concat(Directory.EnumerateFiles(ymm4Dir, "*.exe")))
            {
                try
                {
                    var a = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                    if (!assemblies.Any(x => x.FullName == a.FullName)) assemblies.Add(a);
                }
                catch { }
            }

            static Type? FindType(IEnumerable<Assembly> assemblies, string fullName)
            {
                foreach (var a in assemblies)
                {
                    try
                    {
                        var t = a.GetType(fullName, false, false);
                        if (t is not null) return t;
                    }
                    catch { }
                }
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== RUNTIME AUDIO PLAYBACK-RATE PROBE ===");
            var signalType = FindType(assemblies, "NAudio.Wave.SampleProviders.SignalGenerator");
            var audioPlayerType = FindType(assemblies, "YukkuriMovieMaker.Player.Audio.AudioPlayer");
            var modeType = FindType(assemblies, "YukkuriMovieMaker.Player.Audio.AudioPlayerPlaybackRateMode");
            sb.AppendLine($"SignalGenerator={signalType?.AssemblyQualifiedName ?? "<missing>"}");
            sb.AppendLine($"AudioPlayer={audioPlayerType?.AssemblyQualifiedName ?? "<missing>"}");
            sb.AppendLine($"ModeType={modeType?.AssemblyQualifiedName ?? "<missing>"}");
            if (signalType is null || audioPlayerType is null || modeType is null)
            {
                File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
                return;
            }

            object? signal = null;
            var zeroCtor = signalType.GetConstructor(Type.EmptyTypes);
            if (zeroCtor is not null) signal = zeroCtor.Invoke(null);
            else
            {
                var twoIntCtor = signalType.GetConstructors().FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 2 && p.All(x => x.ParameterType == typeof(int));
                });
                if (twoIntCtor is not null) signal = twoIntCtor.Invoke(new object[] { 48000, 2 });
            }
            sb.AppendLine($"SignalCreated={signal is not null}");
            if (signal is null)
            {
                File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
                return;
            }

            var playerCtor = audioPlayerType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 1 && p[0].ParameterType.IsAssignableFrom(signalType);
                });
            sb.AppendLine($"AudioPlayerCtor={playerCtor}");
            if (playerCtor is null)
            {
                File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
                return;
            }

            var player = playerCtor.Invoke(new[] { signal });
            var rateProp = audioPlayerType.GetProperty("PlaybackRate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var modeProp = audioPlayerType.GetProperty("PlaybackRateMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var reset = audioPlayerType.GetMethod("ResetStream", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var providerField = audioPlayerType.GetField("resamplerProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            sb.AppendLine($"RateProp={rateProp}; ModeProp={modeProp}; Reset={reset}; ProviderField={providerField}");

            foreach (var modeValue in new[] { 1, 2 })
            {
                var mode = Enum.ToObject(modeType, modeValue);
                foreach (var rate in new[] { 8.0, 16.0, 32.0 })
                {
                    object? provider = null;
                    string resetResult = "OK";
                    try
                    {
                        rateProp?.SetValue(player, rate);
                        modeProp?.SetValue(player, mode);
                        reset?.Invoke(player, null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        resetResult = $"THREW {ex.InnerException?.GetType().FullName}: {ex.InnerException?.Message}";
                    }
                    catch (Exception ex)
                    {
                        resetResult = $"THREW {ex.GetType().FullName}: {ex.Message}";
                    }
                    try { provider = providerField?.GetValue(player); } catch { }
                    sb.AppendLine($"MODE={mode} RATE={rate:R} RESET={resetResult}");
                    sb.AppendLine($"  Provider={provider?.GetType().AssemblyQualifiedName ?? "<null>"}");
                    if (provider is null) continue;

                    try
                    {
                        var read = provider.GetType().GetMethod("Read", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            binder: null, types: new[] { typeof(float[]), typeof(int), typeof(int) }, modifiers: null);
                        var buffer = new float[8192];
                        var readCount = read?.Invoke(provider, new object[] { buffer, 0, buffer.Length });
                        var finite = buffer.Take(Convert.ToInt32(readCount ?? 0)).All(float.IsFinite);
                        sb.AppendLine($"  Read={readCount ?? "<missing>"}; Finite={finite}");
                    }
                    catch (TargetInvocationException ex)
                    {
                        sb.AppendLine($"  READ_THREW {ex.InnerException?.GetType().FullName}: {ex.InnerException?.Message}");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"  READ_THREW {ex.GetType().FullName}: {ex.Message}");
                    }
                }
            }

            try
            {
                (player as IDisposable)?.Dispose();
            }
            catch { }

            File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
            Console.WriteLine(sb.ToString());
        }
        catch (Exception ex)
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length >= 3)
                {
                    var output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[2]))!, "runtime-audio-probe.txt");
                    File.WriteAllText(output, "FATAL " + ex, new UTF8Encoding(false));
                }
            }
            catch { }
        }
    }
}
