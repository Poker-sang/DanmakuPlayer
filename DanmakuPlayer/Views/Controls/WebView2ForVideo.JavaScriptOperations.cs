using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace DanmakuPlayer.Views.Controls;

public partial class WebView2ForVideo
{
    public class JavaScriptOperations(ILocator video)
    {
        #region CurrentTime

        public async Task<double> IncreaseCurrentTimeAsync(double second)
        {
            var currentTime = await video.EvaluateAsync($"video => video.currentTime += {second}");
            return currentTime!.Value.GetDouble();
        }

        public async Task SetCurrentTimeAsync(double second)
        {
            _ = await video.EvaluateAsync($"video => video.currentTime = {second}");
        }

        public async Task<double> CurrentTimeAsync()
        {
            var currentTime = await video.EvaluateAsync("video => video.currentTime")!;
            return currentTime!.Value.GetDouble();
        }

        #endregion

        #region Volume

        public async Task<double> IncreaseVolumeAsync(double volume)
        {
            var v = await video.EvaluateAsync($"video => video.volume += {volume / 100}");
            return v!.Value.GetDouble();
        }

        public async Task SetVolumeAsync(double volume)
        {
            _ = await video.EvaluateAsync($"video => video.volume = {volume / 100}");
        }

        public async Task<double> VolumeAsync()
        {
            var v = await video.EvaluateAsync("video => video.volume")!;
            return v!.Value.GetDouble() * 100;
        }

        public async Task<double> IncreaseVolumePercentageAsync(double volume)
        {
            var v = await video.EvaluateAsync($"video => video.volume += {volume}");
            return v!.Value.GetDouble();
        }

        public async Task SetVolumePercentageAsync(double volume)
        {
            _ = await video.EvaluateAsync($"video => video.volume = {volume}");
        }

        public async Task<double> VolumePercentageAsync()
        {
            var v = await video.EvaluateAsync("video => video.volume")!;
            return v!.Value.GetDouble();
        }

        #endregion

        #region Muted

        public async Task<bool> MutedFlipAsync()
        {
            var muted = await video.EvaluateAsync("video => video.muted = !video.muted");
            return muted!.Value.GetBoolean();
        }

        public async Task SetMutedAsync(bool muted)
        {
            _ = await video.EvaluateAsync("video => video.muted = " + (muted ? "true" : "false"));
        }

        public async Task<bool> MutedAsync()
        {
            var muted = await video.EvaluateAsync("video => video.muted");
            return muted!.Value.GetBoolean();
        }

        #endregion

        public async Task<double> DurationAsync()
        {
            try
            {
                var duration = await video.EvaluateAsync("video => video.duration");
                return duration!.Value.GetDouble();
            }
            // 可能出现.NET不支持无穷浮点数异常
            catch (ArgumentException)
            {
                return 0;
            }
        }

        #region PlayPause

        public async Task<bool> IsPlayingAsync()
        {
            var isPlaying =
                await video.EvaluateAsync(
                    "video => !!(video.currentTime > 0 && !video.paused && !video.ended && video.readyState > 2)");
            return isPlaying!.Value.GetBoolean();
        }

        public async Task<bool> PlayPauseFlipTask()
        {
            var isPlaying = await IsPlayingAsync();
            if (isPlaying)
                await PauseAsync();
            else
                await PlayAsync();
            return !isPlaying;
        }

        public async Task PlayAsync()
        {
            _ = await video.EvaluateAsync("video => video.play()");
        }

        public async Task PauseAsync()
        {
            _ = await video.EvaluateAsync("video => video.pause()");
        }

        #endregion

        #region PlaybackRate

        public async Task<double> PlaybackRateAsync()
        {
            var playbackRate = await video.EvaluateAsync("video => video.playbackRate");
            return playbackRate!.Value.GetDouble();
        }

        public async Task SetPlaybackRateAsync(double playbackRate)
        {
            _ = await video.EvaluateAsync($"video => video.playbackRate = {playbackRate}");
        }

        #endregion

        #region FullScreen

        public async Task<bool> FullScreenAsync()
        {
            var fullScreen = await video.EvaluateAsync("video => window.document.fullscreenElement");
            return fullScreen.HasValue;
        }

        public async Task<bool> FullScreenFlipAsync()
        {
            var fullScreen = (await video.EvaluateAsync("video => window.document.fullscreenElement")).HasValue;
            if (fullScreen)
                await ExitFullScreenAsync();
            else
                await RequestFullScreenAsync();
            return !fullScreen;
        }

        public async Task RequestFullScreenAsync()
        {
            _ = await video.EvaluateAsync("video => video.requestFullscreen()");
        }

        public async Task ExitFullScreenAsync()
        {
            _ = await video.EvaluateAsync("video => window.document.exitFullscreen()");
        }

        /// <summary>
        /// <seealso href="https://stackoverflow.com/a/51726329"/>
        /// </summary>
        public async Task ClearControlsAsync()
        {
            _ = await video.EvaluateAsync(
                """
                v = () => {
                    function injectStyles(rule, id) {
                        removeStyle(id);
                        const style = document.createElement('style');
                        style.id = id;
                        style.textContent = rule;
                        document.head.appendChild(style);
                    }
                    
                    function removeStyle(id) {
                        const existing = document.getElementById(id);
                        if (existing) existing.remove();
                    }
                    
                    const css = `video::-webkit-media-controls-panel {
                        display: none !important;
                        opacity: 0 !important;
                    }`;
                    
                    injectStyles(css, 'd');
                };
                """);
        }

        public async Task RestoreControlsAsync()
        {
            _ = await video.EvaluateAsync(
                """
                v = () => {
                    function removeStyle(id) {
                        const element = document.getElementById(id);
                        if (element) {
                            element.remove();
                        }
                    }
                    removeStyle('d');
                };
                """);
        }

        #endregion
    }
}
