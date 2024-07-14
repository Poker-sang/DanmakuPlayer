using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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

    public ObservableCollection<VideoDisplay> Videos { get; } = [];

    public VideoDisplay? CurrentVideo
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
            if (_currentVideo is null)
                Operations = null;
            else
            {
                _ = await WebView2.ExecuteScriptAsync(
                    $"""
                     var currentDocument = {_currentVideo.DocumentJsQuery};
                     var video = currentDocument{_currentVideo.VideoJsQuery};
                     """);
                Operations = new(WebView2.ExecuteScriptAsync);
            }
        }
        finally
        {
            _ = OperationsSemaphore.Release();
        }
    }

    private WebView2 WebView2 => Content.To<WebView2>();

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
    public bool HasVideo => Operations is not null;

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

    private VideoDisplay? _currentVideo;

    public event TypedEventHandler<WebView2ForVideo, EventArgs>? VideoLoaded;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var opt = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = "--disable-web-security --disable-site-isolation-trials --proxy-auto-detect"
        };
        var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, opt);
        await WebView2.EnsureCoreWebView2Async(env);
        WebView2.NavigationCompleted += Navigated;
    }

    private async void Navigated(WebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        Url = (await WebView2.ExecuteScriptAsync("document.location.href")).Trim('"');
        await LoadVideoAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        WebView2.Close();
        _loadSemaphore.Dispose();
    }

    public IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> GetBiliCookieAsync() => WebView2.CoreWebView2.CookieManager.GetCookiesAsync("https://bilibili.com");

    public async Task GotoAsync(string url)
    {
        try
        {
            CurrentVideo = null;
            Videos.Clear();
            OnPropertyChanged(nameof(Videos));
            WebView2.Source = new(url);
            while (await WebView2.ExecuteScriptAsync("document.readyState === 'interactive' || document.readyState === 'complete'") is not "true")
                await Task.Delay(200);
        }
        catch (UriFormatException)
        {
            // 网址错误不跳转
        }
        catch (ArgumentNullException)
        {
            // 网址错误不跳转
        }
    }

    private CancellationTokenSource? _source;

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    [SuppressMessage("CodeQuality", "IDE0079:请删除不必要的忽略")]
    public async Task LoadVideoAsync()
    {
        await _loadSemaphore.WaitAsync();
        try
        {
            if (_source is not null)
            {
                if (!_source.IsCancellationRequested)
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
                {
                    CurrentVideo = null;
                    Videos.Clear();
                    SetOperation();
                    while (await WebView2.ExecuteScriptAsync("document.readyState === 'interactive' || document.readyState === 'complete'") is not "true")
                        await Task.Delay(200);
                    if (!_source.IsCancellationRequested || Videos.Count is 0)
                    {
                        _ = WebView2.ExecuteScriptAsync("const frames = document.querySelectorAll('iframe')");
                        var framesCount = int.Parse(await WebView2.ExecuteScriptAsync("document.querySelectorAll('iframe').length"));
                        var tasks = new Task[framesCount + 1];
                        tasks[0] = TaskAsync("document", _source);
                        for (var i = 0; i < framesCount; i++)
                            tasks[i + 1] = TaskAsync($"document.querySelectorAll('iframe')[{i}].contentDocument", _source);
                        _ = Task.WhenAny(Task.WhenAll(tasks), Task.Delay(3000, _source.Token)).ContinueWith(_ => _source.Cancel(), _source.Token);
                    }
                    while (!_source.IsCancellationRequested)
                        await Task.Delay(200);
                }
                _source = null;

                if (Videos.Count is not 0)
                    CurrentVideo = Videos[0];

                // 触发 CollectionVisibility
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
        }
        finally
        {
            _ = _loadSemaphore.Release();
        }
        return;
        [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
        async Task TaskAsync(string document, CancellationTokenSource s)
        {
            try
            {
                while (await WebView2.ExecuteScriptAsync($"{document}.readyState === 'interactive' || {document}.readyState === 'complete'") is not "true")
                    await Task.Delay(200);
                int count;
                do
                {
                    var videos = $"{document}.querySelectorAll('video')";
                    count = int.Parse(await WebView2.ExecuteScriptAsync(videos + ".length"));
                    if (count is not 0)
                        for (var i = 0; i < count; i++)
                        {
                            var query = $".querySelectorAll('video')[{i}]";
                            var durationStr = await WebView2.ExecuteScriptAsync(videos + query + ".duration");
                            if (!int.TryParse(durationStr, out var duration))
                                duration = -1;
                            Videos.Add(new(document, query, duration));
                        }
                    await Task.Delay(200);
                } while (!s.IsCancellationRequested && count is 0 && await WebView2.ExecuteScriptAsync($"{document}.readyState === 'complete'") is not "true");

                if (!s.IsCancellationRequested && count is not 0)
                    await s.CancelAsync();
            }
            catch
            {
                // ignored
            }
        }
    }

    public async Task GoBackAsync() => await WebView2.ExecuteScriptAsync("history.back()");

    public async Task GoForwardAsync() => await WebView2.ExecuteScriptAsync("history.forward()");

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
