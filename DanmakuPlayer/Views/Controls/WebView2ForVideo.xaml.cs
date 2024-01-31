using System;
using System.Threading.Tasks;
using Windows.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Playwright;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.Controls;

public class VideoEventArgs(ILocator Video) : EventArgs;

[DependencyProperty<double>("Duration")]
[DependencyProperty<bool>("CanGoForward")]
[DependencyProperty<bool>("CanGoBack")]
[DependencyProperty<bool>("HasVideo")]
[DependencyProperty<string>("Url")]
[ObservableObject]
public sealed partial class WebView2ForVideo : UserControl
{
    public WebView2ForVideo()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private WebView2 WebView2 => Content.To<WebView2>();

    private IPlaywright Pw { get; set; } = null!;

    private IBrowser Browser { get; set; } = null!;

    private IPage Page { get; set; } = null!;

    private ILocator Video { get; set; } = null!;

    [ObservableProperty] private bool _isLoading;

    public event TypedEventHandler<WebView2ForVideo, VideoEventArgs>? VideoLoaded;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebView2.EnsureCoreWebView2Async();
        Pw = await Playwright.CreateAsync();
        Browser = await Pw.Chromium.ConnectOverCDPAsync($"http://localhost:{App.RemoteDebuggingPort}");
        Page = Browser.Contexts[0].Pages[0];
        Page.FrameNavigated += Page_FrameNavigated;
    }

    private void Page_FrameNavigated(object? sender, IFrame e)
    {
        _ = DispatcherQueue.TryEnqueue(Callback);

        return;
        async void Callback()
        {
            Url = e.Url;

            await LoadVideoAsync();
        }
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Browser is { } browser)
            await browser.DisposeAsync();
        Pw?.Dispose();
    }

    public async Task GotoAsync(string url)
    {
        try
        {
            _ = await Page.GotoAsync(url);
        }
        // 网址错误不跳转
        catch (PlaywrightException)
        {
        }
    }

    public async Task LoadVideoAsync()
    {
        try
        {
            IsLoading = true;
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            HasVideo = false;
            foreach (var frame in Page.Frames)
            {
                var locator = frame.Locator("video");
                var count = await locator.CountAsync();
                if (count is not 0)
                {
                    Video = locator;
                    HasVideo = true;
                    break;
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
        if (HasVideo)
            try
            {
                Duration = await DurationAsync();
                await PauseAsync();
            }
            // 可能出现.NET不支持无穷浮点数异常
            catch (ArgumentException)
            {
                Duration = 0;
                return;
            }
        else
        {
            Duration = 0;
            return;
        }
        VideoLoaded?.Invoke(this, new(Video));
    }

    public async Task GoBackAsync() => await Page.GoBackAsync();

    public async Task GoForwardAsync() => await Page.GoForwardAsync();

    private async void WebView2Tapped(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender.To<UIElement>()).Properties;
        if (properties.IsXButton1Pressed)
        {
            if (CanGoBack)
                await GoBackAsync();
        }
        else if (properties.IsXButton2Pressed)
        {
            if (CanGoForward)
                await GoForwardAsync();
        }
    }

    #region JavaScript Property

    #region CurrentTime

    public async Task<double> IncreaseCurrentTimeAsync(double second)
    {
        var currentTime = await Video.EvaluateAsync($"video => video.currentTime += {second}");
        return currentTime!.Value.GetDouble();
    }

    public async Task SetCurrentTimeAsync(double second)
    {
        _ = await Video.EvaluateAsync($"video => video.currentTime = {second}");
    }

    public async Task<double> CurrentTimeAsync()
    {
        var currentTime = await Video.EvaluateAsync("video => video.currentTime")!;
        return currentTime!.Value.GetDouble();
    }

    #endregion

    #region Volume

    public async Task<double> IncreaseVolumeAsync(double volume)
    {
        var v = await Video.EvaluateAsync($"video => video.volume += {volume / 100}");
        return v!.Value.GetDouble();
    }

    public async Task SetVolumeAsync(double volume)
    {
        _ = await Video.EvaluateAsync($"video => video.volume = {volume / 100}");
    }

    public async Task<double> VolumeAsync()
    {
        var v = await Video.EvaluateAsync("video => video.volume")!;
        return v!.Value.GetDouble() * 100;
    }

    public async Task<double> IncreaseVolumePercentageAsync(double volume)
    {
        var v = await Video.EvaluateAsync($"video => video.volume += {volume}");
        return v!.Value.GetDouble();
    }

    public async Task SetVolumePercentageAsync(double volume)
    {
        _ = await Video.EvaluateAsync($"video => video.volume = {volume}");
    }

    public async Task<double> VolumePercentageAsync()
    {
        var v = await Video.EvaluateAsync("video => video.volume")!;
        return v!.Value.GetDouble();
    }

    #endregion

    #region Muted

    public async Task<bool> MutedFlipAsync()
    {
        var muted = await Video.EvaluateAsync("video => video.muted = !video.muted");
        return muted!.Value.GetBoolean();
    }

    public async Task SetMutedAsync(bool muted)
    {
        _ = await Video.EvaluateAsync("video => video.muted = " + (muted ? "true" : "false"));
    }

    public async Task<bool> MutedAsync()
    {
        var muted = await Video.EvaluateAsync("video => video.muted");
        return muted!.Value.GetBoolean();
    }

    #endregion

    /// <summary>
    /// Use <see cref="TryLoadVideoAsync"/> to update <see cref="Duration"/>
    /// </summary>
    /// <returns></returns>
    private async Task<double> DurationAsync()
    {
        var duration = await Video.EvaluateAsync("video => video.duration");
        return duration!.Value.GetDouble();
    }

    #region PlayPause

    public async Task<bool> IsPlayingAsync()
    {
        var isPlaying = await Video.EvaluateAsync("video => !!(video.currentTime > 0 && !video.paused && !video.ended && video.readyState > 2)");
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
        _ = await Video.EvaluateAsync("video => video.play()");
    }

    public async Task PauseAsync()
    {
        _ = await Video.EvaluateAsync("video => video.pause()");
    }

    #endregion

    #region PlaybackRate

    public async Task<double> PlaybackRateAsync()
    {
        var playbackRate = await Video.EvaluateAsync("video => video.playbackRate");
        return playbackRate!.Value.GetDouble();
    }

    public async Task SetPlaybackRateAsync(double playbackRate)
    {
        _ = await Video.EvaluateAsync($"video => video.playbackRate = {playbackRate}");
    }

    #endregion

    #region FullScreen

    public async Task<bool> FullScreenAsync()
    {
        var fullScreen = await Video.EvaluateAsync("video => window.document.fullscreenElement");
        return fullScreen.HasValue;
    }

    public async Task<bool> FullScreenFlipAsync()
    {
        var fullScreen = (await Video.EvaluateAsync("video => window.document.fullscreenElement")).HasValue;
        if (fullScreen)
            await ExitFullScreenAsync();
        else
            await RequestFullScreenAsync();
        return !fullScreen;
    }

    public async Task RequestFullScreenAsync()
    {
        _ = await Video.EvaluateAsync("video => video.requestFullscreen()");
    }

    public async Task ExitFullScreenAsync()
    {
        _ = await Video.EvaluateAsync("video => window.document.exitFullscreen()");
    }

    #endregion

    #endregion
}
