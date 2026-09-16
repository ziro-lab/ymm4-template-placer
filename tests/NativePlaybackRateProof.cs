using System.Collections;
using System.Reflection;
using YukkuriMovieMaker.Settings;

namespace Ymm4TemplatePlacer;

internal static partial class NativeProof
{
    private static void VerifyPlaybackRateExtension(object mainViewModel)
    {
        stage = "playback-rate-extension";
        var preview = FindPreviewViewModel(mainViewModel)
            ?? throw new InvalidOperationException("PreviewViewModel was not found in the live YMM4 host.");
        var player = preview.GetType().GetField("player", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(preview)
            ?? throw new InvalidOperationException("PreviewViewModel.player was not available.");
        var playerRate = player.GetType().GetProperty("PlaybackRate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TimelineVideoPlayer.PlaybackRate was not available.");
        var update = preview.GetType().GetMethod("UpdatePlayerPlaybackRate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PreviewViewModel.UpdatePlayerPlaybackRate was not available.");

        var settings = YMMSettings.Default;
        var originalSetting = settings.PlaybackRate;
        var rows = new List<string>
        {
            $"preview={preview.GetType().FullName}",
            $"player={player.GetType().FullName}",
            $"playerBase={player.GetType().BaseType?.FullName}",
            $"originalSetting={originalSetting}",
            $"originalPlayerRate={Convert.ToDouble(playerRate.GetValue(player)):R}"
        };

        try
        {
            foreach (var (setting, expectedPercent) in new[] { (31, 800d), (63, 1600d), (127, 3200d) })
            {
                settings.PlaybackRate = setting;
                // PropertyChanged normally updates the player synchronously. Invoke explicitly as well so
                // this proof verifies the mapping independently of event subscription timing.
                update.Invoke(preview, null);
                var actual = Convert.ToDouble(playerRate.GetValue(player));
                rows.Add($"setting={setting};expectedPercent={expectedPercent:R};actualPercent={actual:R};audioMultiplier={actual / 100d:R}");
                Assert(Math.Abs(actual - expectedPercent) < 0.000001,
                    $"live Preview player accepts {expectedPercent / 100d:R}x without an 8x clamp");
            }

            Assert(player.GetType().BaseType?.FullName == "YukkuriMovieMaker.Player.TimelineAudioPlayer",
                "TimelineVideoPlayer uses TimelineAudioPlayer as its native audio playback base");
            rows.Add("RESULT=PASS");
            File.WriteAllLines(Path.Combine(output, "playback-rate-proof.txt"), rows);
        }
        finally
        {
            settings.PlaybackRate = originalSetting;
            update.Invoke(preview, null);
        }
    }

    private static object? FindPreviewViewModel(object mainViewModel)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object? areas = mainViewModel.GetType().GetProperty("AnchorableAreaViewModels", flags)?.GetValue(mainViewModel);
        areas ??= mainViewModel.GetType().GetField("anchorableAreaViewModels", flags)?.GetValue(mainViewModel);
        if (areas is not IEnumerable enumerable) return null;
        foreach (var area in enumerable)
        {
            if (area is null) continue;
            var vm = area.GetType().GetProperty("ViewModel", flags)?.GetValue(area) ?? area;
            if (vm.GetType().Name == "PreviewViewModel") return vm;
        }
        return null;
    }
}
