using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Playwright;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

[ObservableObject]
public sealed partial class WebView2ForVideo : UserControl
{
    [GeneratedDependencyProperty]
    public partial double Duration { get; set; }

    [GeneratedDependencyProperty]
    public partial bool CanGoForward { get; set; }

    [GeneratedDependencyProperty]
    public partial bool CanGoBack { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "")]
    public partial string Url { get; set; }

    public WebView2ForVideo()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ObservableCollection<VideoLocatorDisplay> Videos { get; } = [];

    public VideoLocatorDisplay? CurrentVideo
    {
        get => _currentVideo;
        set
        {
            if (Equals(value, _currentVideo))
                return;
            _currentVideo = value;
            OnPropertyChanged();
            SetOperation();
        }
    }

    private async void SetOperation()
    {
        await OperationsSemaphore.WaitAsync();
        try
        {
            Operations = _currentVideo is null ? null : new(_currentVideo.Video);
        }
        finally
        {
            _ = OperationsSemaphore.Release();
        }
    }

    private WebView2 WebView2 => Content.To<WebView2>();

    private IPlaywright Pw { get; set; } = null!;

    private IBrowser Browser { get; set; } = null!;

    private IPage Page { get; set; } = null!;

    private Uri? SourceUri
    {
        get => null;
        set
        {
            if (value is not null)
                Url = value.OriginalString;
        }
    }

    [MemberNotNullWhen(true, nameof(Operations))]
    public bool HasVideo => Operations is not null && !Page.IsClosed;

    private JavaScriptOperations? Operations { get; set; }

    public SemaphoreSlim OperationsSemaphore { get; } = new(1, 1);

    public async Task LockOperationsAsync(Func<JavaScriptOperations, Task> task)
    {
        await OperationsSemaphore.WaitAsync();
        try
        {
            if (HasVideo)
            {
                _ = await Task.WhenAny([task(Operations), Task.Delay(1000)]);
            }
        }
        finally
        {
            _ = OperationsSemaphore.Release();
        }
    }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    private VideoLocatorDisplay? _currentVideo;

    public event TypedEventHandler<WebView2ForVideo, EventArgs>? VideoLoaded;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var opt = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = "--proxy-auto-detect"
        };
        var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, opt);
        await WebView2.EnsureCoreWebView2Async(env);
        Pw = await Playwright.CreateAsync();
        Browser = await Pw.Chromium.ConnectOverCDPAsync($"http://localhost:{App.RemoteDebuggingPort}");
        Page = Browser.Contexts[0].Pages[0];
        Page.FrameNavigated += Page_FrameNavigated;
    }

    private void Page_FrameNavigated(object? sender, IFrame e)
    {
        if (e == Page.MainFrame)
            _ = DispatcherQueue.TryEnqueue(() => Url = e.Url);

        _ = DispatcherQueue.TryEnqueue(Callback);

        return;
        async void Callback() => await LoadVideoAsync();
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Browser is { } browser)
            await browser.DisposeAsync();
        Pw?.Dispose();
        WebView2.Close();
    }

    public IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> GetBiliCookieAsync() => WebView2.CoreWebView2.CookieManager.GetCookiesAsync("https://bilibili.com");

    public async Task GotoAsync(string url)
    {
        try
        {
            CurrentVideo = null;
            Videos.Clear();
            OnPropertyChanged(nameof(Videos));
            if (Page == null!)
                return;
            _ = await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }
        catch (PlaywrightException)
        {
            // 网址错误不跳转
        }
    }

    private CancellationTokenSource? _source;

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    [SuppressMessage("CodeQuality", "IDE0079:请删除不必要的忽略")]
    public async Task LoadVideoAsync()
    {
        if (_source is not null)
        {
            await _source.CancelAsync();

            for (var i = 0; i < 3 || IsLoading; ++i)
                await Task.Delay(200);

            if (IsLoading)
                return;
        }
        try
        {
            IsLoading = true;

            using (_source = new())
            using (_source = new())
            {
                CurrentVideo = null;
                Videos.Clear();
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                var tasks = Page.Frames.Select(frame => TaskAsync(frame, _source));
                _ = Task.WhenAny(Task.WhenAll(tasks), Task.Delay(3000, _source.Token)).ContinueWith(_ => _source.Cancel(), _source.Token);

                // ReSharper disable once MethodSupportsCancellation
                while (!_source.Token.IsCancellationRequested)
                    await Task.Delay(200);
            }
            _source = null;

            if (Videos.Count is not 0)
                CurrentVideo = Videos[0];

            // 触发 CollectionVisibilityConverter
            OnPropertyChanged(nameof(Videos));
        }
        catch
        {
            CurrentVideo = null;
            Videos.Clear();
            OnPropertyChanged(nameof(Videos));
        }
        finally
        {
            IsLoading = false;
        }
        if (HasVideo)
        {
            await LockOperationsAsync(async operations =>
            {
                Duration = await operations.DurationAsync();
                await operations.PauseAsync();
            });
        }
        else
        {
            Duration = 0;
            return;
        }
        VideoLoaded?.Invoke(this, EventArgs.Empty);

        return;
        [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
        async Task TaskAsync(IFrame frame, CancellationTokenSource s)
        {
            try
            {
                await frame.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                int count;
                do
                {
                    var first = frame.Locator("video");
                    count = await first.CountAsync();
                    if (count is not 0)
                        foreach (var locator in await first.AllAsync())
                            Videos.Add(await VideoLocatorDisplay.CreateAsync(locator));
                    await Task.Delay(200);
                } while (!s.Token.IsCancellationRequested && count is 0);

                if (!s.Token.IsCancellationRequested && count is not 0)
                    await s.CancelAsync();
            }
            catch
            {
                // ignored
            }
        }
    }

    public async Task GoBackAsync() => await Page.GoBackAsync();

    public async Task GoForwardAsync() => await Page.GoForwardAsync();

    public async void WebView2PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender.To<UIElement>()).Properties;
        switch (properties.PointerUpdateKind)
        {
            case PointerUpdateKind.XButton1Released:
                if (CanGoBack)
                    await GoBackAsync();
                break;
            case PointerUpdateKind.XButton2Released:
                if (CanGoForward)
                    await GoForwardAsync();
                break;
        }
    }
}
