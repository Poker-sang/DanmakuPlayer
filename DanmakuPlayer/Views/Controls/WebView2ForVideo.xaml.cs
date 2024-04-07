using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Playwright;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;
using System.Xml;
using Microsoft.Web.WebView2.Core;

namespace DanmakuPlayer.Views.Controls;

[DependencyProperty<double>("Duration")]
[DependencyProperty<bool>("CanGoForward")]
[DependencyProperty<bool>("CanGoBack")]
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

    public JavaScriptOperations? Operations { get; private set; }

    [ObservableProperty] private bool _isLoading;

    public event TypedEventHandler<WebView2ForVideo, EventArgs>? VideoLoaded;

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
        WebView2.Close();
    }

    public IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> GetBiliCookieAsync() => WebView2.CoreWebView2.CookieManager.GetCookiesAsync("https://bilibili.com");

    public async Task GotoAsync(string url)
    {
        try
        {
            if (Page == null!)
                return;
            _ = await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
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
            var source = new CancellationTokenSource();

            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var tasks = Page.Frames.Select(frame => TaskAsync(frame, source));
            _ = Task.WhenAny(Task.WhenAll(tasks), Task.Delay(3000, source.Token)).ContinueWith(_ =>
            {
                source.Cancel();
                source.Dispose();
            }, source.Token);
            // ReSharper disable once MethodSupportsCancellation
            while (!source.Token.IsCancellationRequested)
                await Task.Delay(200);
        }
        catch
        {
            Operations = null;
        }
        finally
        {
            IsLoading = false;
        }
        if (HasVideo)
            try
            {
                Duration = await Operations.DurationAsync();
                await Operations.PauseAsync();
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
        VideoLoaded?.Invoke(this, EventArgs.Empty);

        return;
        async Task TaskAsync(IFrame frame, CancellationTokenSource s)
        {
            try
            {
                await frame.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                ILocator locator;
                int count;
                do
                {
                    locator = frame.Locator("video");
                    count = await locator.CountAsync();
                    await Task.Delay(200, s.Token);
                } while (!s.Token.IsCancellationRequested && count is 0);

                if (!s.Token.IsCancellationRequested && count is not 0)
                {
                    Operations = new(locator);
                    await s.CancelAsync();
                }
            }
            catch
            {
                // ignored
            }
        }
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
}
