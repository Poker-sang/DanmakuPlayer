using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.Playwright;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.Controls;

[DependencyProperty<double>("Duration")]
[DependencyProperty<bool>("CanGoForward")]
[DependencyProperty<bool>("CanGoBack")]
[DependencyProperty<bool>("HasVideo")]
public sealed partial class WebView2ForVideo : UserControl
{
    private WebView2 WebView2 => Content.To<WebView2>();
    private IPlaywright Pw { get; set; } = null!;
    private IBrowser Browser { get; set; } = null!;
    private IPage Page { get; set; } = null!;
    private ILocator? Video { get; set; }

    public event TypedEventHandler<WebView2ForVideo, EventArgs>? PageLoaded;

    public WebView2ForVideo()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebView2.EnsureCoreWebView2Async();
        Pw = await Playwright.CreateAsync();
        Browser = await Pw.Chromium.ConnectOverCDPAsync("http://localhost:9222");
        Page = Browser.Contexts[0].Pages[0];
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        await Browser.DisposeAsync();
        Pw.Dispose();
    }

    public async Task GotoAsync(string url)
    {
        try
        {
            _ = await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        }
        catch (PlaywrightException) // 网址错误不跳转
        {
            return;
        }

        Video = Page.Locator("video").First;
        HasVideo = Video is not null;
        if (HasVideo)
            try
            {
                Duration = await DurationAsync(); 
            }
            catch (ArgumentException) // 可能出现.NET不支持无穷浮点数异常
            {
                try
                {
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    Duration = await DurationAsync();
                }
                catch (TimeoutException)
                {
                    Duration = await DurationAsync();
                }
                catch (ArgumentException)
                {
                    Duration = 0;
                }
            }
        PageLoaded?.Invoke(this, EventArgs.Empty);
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
        var duration = await Video!.EvaluateAsync($"video => video.currentTime += {second}");
        return duration!.Value.GetDouble();
    }

    public async Task SetCurrentTimeAsync(double second)
    {
        _ = await Video!.EvaluateAsync($"video => video.currentTime = {second}");
    }

    public async Task<double> CurrentTimeAsync()
    {
        var duration = await Video!.EvaluateAsync("video => video.currentTime")!;
        return duration!.Value.GetDouble();
    }

    #endregion

    #region Volume

    public async Task<double> IncreaseVolumeAsync(double volume)
    {
        var duration = await Video!.EvaluateAsync($"video => video.volume += {volume}");
        return duration!.Value.GetDouble();
    }

    public async Task SetVolumeAsync(double volume)
    {
        _ = await Video!.EvaluateAsync($"video => video.volume = {volume}");
    }

    public async Task<double> VolumeAsync()
    {
        var duration = await Video!.EvaluateAsync("video => video.volume")!;
        return duration!.Value.GetDouble();
    }

    #endregion

    #region Muted

    public async Task<bool> MutedFlipAsync()
    {
        var duration = await Video!.EvaluateAsync("video => video.muted = !video.muted");
        return duration!.Value.GetBoolean();
    }

    public async Task SetMutedAsync(bool muted)
    {
        _ = await Video!.EvaluateAsync("video => video.muted = " + (muted ? "true" : "false"));
    }

    public async Task<bool> MutedAsync()
    {
        var duration = await Video!.EvaluateAsync("video => video.muted");
        return duration!.Value.GetBoolean();
    }

    #endregion

    private async Task<double> DurationAsync()
    {
        var duration = await Video!.EvaluateAsync("video => video.duration");
        return duration!.Value.GetDouble();
    }

    public async Task PlayAsync()
    {
        _ = await Video!.EvaluateAsync("video => video.play()");
    }

    public async Task PauseAsync()
    {
        _ = await Video!.EvaluateAsync("video => video.pause()");
    }

    public async Task PlaybackRateAsync()
    {
        _ = await Video!.EvaluateAsync("video => video.playbackRate");
    }

    public async Task SetPlaybackRateAsync(double playbackRate)
    {
        _ = await Video!.EvaluateAsync($"video => video.playbackRate = {playbackRate}");
    }

    #region FullScreen

    public async Task<bool> FullScreenAsync()
    {
        var fullScreen = await Video!.EvaluateAsync("video => window.document.fullscreenElement");
        return fullScreen.HasValue;
    }

    public async Task RequestFullScreenAsync()
    {
        _ = await Video!.EvaluateAsync("video => video.requestFullscreen()");
    }

    public async Task ExitFullScreenAsync()
    {
        _ = await Video!.EvaluateAsync("video => window.document.exitFullscreen()");
    }

    #endregion

    #endregion
}
