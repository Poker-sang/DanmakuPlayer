using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace DanmakuPlayer.Views.Controls;

public partial class WebView2ForVideo
{
    public class JavaScriptOperations(Func<string, IAsyncOperation<string>> executeScriptAsync)
    {
        #region CurrentTime

        public async Task<double> IncreaseCurrentTimeAsync(double second) => double.Parse(await executeScriptAsync($"video.currentTime += {second}"));

        public async Task SetCurrentTimeAsync(double second) => _ = await executeScriptAsync($"video.currentTime = {second}");

        public async Task<double> CurrentTimeAsync() => double.Parse(await executeScriptAsync("video.currentTime"));

        #endregion

        #region Volume

        public async Task<double> IncreaseVolumeAsync(double volume) => double.Parse(await executeScriptAsync($"video.volume += {volume / 100}"));

        public async Task SetVolumeAsync(double volume) => await executeScriptAsync($"video.volume = {volume / 100}");

        public async Task<double> VolumeAsync() => double.Parse(await executeScriptAsync("video.volume")) * 100;

        public async Task<double> IncreaseVolumePercentageAsync(double volume) => double.Parse(await executeScriptAsync($"video.volume += {volume}"));

        public async Task SetVolumePercentageAsync(double volume) => _ = await executeScriptAsync($"video.volume = {volume}");

        public async Task<double> VolumePercentageAsync() => double.Parse(await executeScriptAsync("video.volume"));

        #endregion

        #region Muted

        public async Task<bool> MutedFlipAsync() => await executeScriptAsync("video.muted = !video.muted") is "true";

        public async Task SetMutedAsync(bool muted) => _ = await executeScriptAsync("video.muted = " + (muted ? "true" : "false"));

        public async Task<bool> MutedAsync() => await executeScriptAsync("video.muted") is "true";

        #endregion

        public async Task<double> DurationAsync() =>
            await executeScriptAsync("video.duration") is var duration && duration is "Infinity" or "-Infinity"
                ? 0
                : double.Parse(duration);

        #region PlayPause

        public async Task<bool> IsPlayingAsync() =>
            await executeScriptAsync(
                "!!(video.currentTime > 0 && !video.paused && !video.ended && video.readyState > 2)") is "true";

        public async Task<bool> PlayPauseFlipTask()
        {
            var isPlaying = await IsPlayingAsync();
            if (isPlaying)
                await PauseAsync();
            else
                await PlayAsync();
            return !isPlaying;
        }

        public async Task PlayAsync() => _ = await executeScriptAsync("video.play()");

        public async Task PauseAsync() => _ = await executeScriptAsync("video.pause()");

        #endregion

        #region PlaybackRate

        public async Task<double> PlaybackRateAsync() => double.Parse(await executeScriptAsync("video.playbackRate"));

        public async Task SetPlaybackRateAsync(double playbackRate) => await executeScriptAsync($"video.playbackRate = {playbackRate}");

        #endregion

        #region FullScreen

        public async Task<bool> FullScreenAsync() => await executeScriptAsync("currentDocument.fullscreenElement") is not "null";

        public async Task<bool> FullScreenFlipAsync()
        {
            if (await FullScreenAsync())
                await ExitFullScreenAsync();
            else
                await RequestFullScreenAsync();
            return await FullScreenAsync();
        }

        public async Task RequestFullScreenAsync() => _ = await executeScriptAsync("video.requestFullscreen()");

        public async Task ExitFullScreenAsync() => _ = await executeScriptAsync("currentDocument.exitFullscreen()");

        /// <summary>
        /// <seealso href="https://stackoverflow.com/a/51726329"/>
        /// </summary>
        public async Task ClearControlsAsync()
        {
            _ = await executeScriptAsync(
                """
                function injectStyles(rule, id) {
                    removeStyle(id);
                    const tempStyle = currentDocument.createElement('style');
                    tempStyle.id = id;
                    tempStyle.innerHTML = rule;
                    currentDocument.head.appendChild(tempStyle);
                }
                function removeStyle(id) {
                    currentDocument.getElementById(id)?.remove();
                }
                injectStyles(`video::-webkit-media-controls-panel
                {
                    display: none !important;
                    opacity: 0 !important;
                }`, 'danmakuPlayerNoControlStyle');
                """);
        }

        public async Task RestoreControlsAsync()
        {
            _ = await executeScriptAsync(
                """
                function removeStyle(id) {
                    currentDocument.getElementById(id)?.remove();
                }
                removeStyle('danmakuPlayerNoControlStyle');
                """);
        }

        #endregion
    }
}
