using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI;
using DanmakuPlayer.Models;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using DanmakuPlayer.Views.Converters;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ProtoBuf;
using WinRT;
using WinUI3Utilities;
using RenderType = DanmakuPlayer.Enums.RenderType;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class BackgroundPanel : SwapChainPanel
{
#pragma warning disable CA1822, IDE0079 // 将成员标记为 static
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
    private Color TransparentColor => Color.FromArgb(0xff / 2, 0, 0, 0);
#pragma warning restore CA1822, IDE0079

    public RootViewModel Vm { get; } = new();

    public BackgroundPanel()
    {
        AppContext.BackgroundPanel = this;
        InitializeComponent();
        LockWebView2Button.Icon = new FontIcon { Glyph = Vm.LockWebView2 ? "\uE785" : "\uE72E" };
        DragMoveAndResizeHelper.RootPanel = this;
        AppContext.DanmakuCanvas = DanmakuCanvas;

        DispatcherTimerHelper.Tick += TimerTick;

        _filter = new()
        {
            DanmakuCombiner.Combine,
            DanmakuRegex.Match
        };
    }

    #region 操作

    private readonly DanmakuFilter _filter;

    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// 加载弹幕操作
    /// </summary>
    /// <param name="action"></param>
    private async Task LoadDanmaku(Func<CancellationToken, Task<List<Danmaku>>> action)
    {
        Pause();

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        if (!WebView.HasVideo)
        {
            Vm.TotalTime = 0;
            Vm.Time = 0;
        }
        DanmakuHelper.ClearPool();

        try
        {
            var tempPool = await action(_cancellationTokenSource.Token);

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.ObtainedAndFiltrating, tempPool.Count), TeachingTipSeverity.Information, Emoticon.Okay);

            DanmakuHelper.Pool = await _filter.Filtrate(tempPool, Vm.AppConfig, _cancellationTokenSource.Token);
            var filtrateRate = tempPool.Count is 0 ? 0 : DanmakuHelper.Pool.Length * 100 / tempPool.Count;

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.FiltratedAndRendering, DanmakuHelper.Pool.Length, filtrateRate), TeachingTipSeverity.Information, Emoticon.Okay);

            var renderedCount = await DanmakuHelper.Render(DanmakuCanvas, RenderType.RenderInit, _cancellationTokenSource.Token);
            var renderRate = DanmakuHelper.Pool.Length is 0 ? 0 : renderedCount * 100 / DanmakuHelper.Pool.Length;
            var totalRate = tempPool.Count is 0 ? 0 : renderedCount * 100 / tempPool.Count;
            if (!WebView.HasVideo)
                Vm.TotalTime = (DanmakuHelper.Pool.Length is 0 ? 0 : DanmakuHelper.Pool[^1].Time) + Vm.AppConfig.DanmakuActualDuration;

            RootTeachingTip.ShowAndHide(string.Format(MainPanelResources.DanmakuReady, DanmakuHelper.Pool.Length, filtrateRate, renderRate, totalRate), TeachingTipSeverity.Ok, Emoticon.Okay);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            RootTeachingTip.ShowAndHide(Emoticon.Depressed + " " + MainPanelResources.ExceptionThrown, TeachingTipSeverity.Error, e.Message);
        }

        Vm.StartPlaying = true;
    }

    public async void ReloadDanmaku(RenderType renderType)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        TryPause();

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        try
        {
            _ = await DanmakuHelper.Render(DanmakuCanvas, renderType, _cancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        TryResume();
    }

    public void ResetProvider() => ReloadDanmaku(RenderType.ReloadProvider);

    public void DanmakuFontChanged() => ReloadDanmaku(RenderType.ReloadFormats);

    #region 播放及暂停

    private DateTime _lastTime;

    private int _tickCount;

    private void TimerTick(object? sender, object e)
    {
        var now = DateTime.Now;
        if (Vm.Time < Vm.TotalTime)
        {
            if (Vm.IsPlaying)
            {
                Vm.ActualTime += (now - _lastTime).TotalSeconds;
                DanmakuCanvas.Invalidate();
            }
        }
        else
        {
            Pause();
            Vm.Time = 0;
        }

        if (WebView.HasVideo)
        {
            ++_tickCount;
            if (_tickCount is 10)
            {
                _tickCount = 0;
                UpdateTime();
            }
        }

        _lastTime = now;
    }

    private bool _needResume;

    private void TryPause()
    {
        _needResume = Vm.IsPlaying;
        Pause();
    }

    private void TryResume()
    {
        if (_needResume)
            Resume();
        _needResume = false;
    }

    private async void Resume()
    {
        _lastTime = DateTime.Now;
        DanmakuHelper.RenderType = RenderType.RenderAlways;
        Vm.IsPlaying = true;
        if (WebView.HasVideo)
        {
            await WebView.PlayAsync();
            UpdateTime();
        }
    }

    private async void Pause()
    {
        DanmakuHelper.RenderType = RenderType.RenderOnce;
        Vm.IsPlaying = false;
        if (WebView.HasVideo)
        {
            await WebView.PauseAsync();
            UpdateTime();
        }
    }

    private async void UpdateTime(double? time = null)
    {
        Vm.Time = time ?? await WebView.CurrentTimeAsync();
    }

    public async void TrySetPlaybackRate()
    {
        if (WebView.HasVideo)
            await WebView.SetPlaybackRateAsync(Vm.AppConfig.PlaybackRate);
    }

    #endregion

    #endregion

    #region 事件处理

    #region SwapChainPanel事件

    private void MaximizeRestoreTapped(object sender, RoutedEventArgs e)
    {
        Vm.IsMaximized = !Vm.IsMaximized;
    }

    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        ReloadDanmaku(RenderType.ReloadProvider);
    }

    private void RootUnloaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    #endregion

    #region Title区按钮

    private void CloseTapped(object sender, TappedRoutedEventArgs e) => CurrentContext.App.Exit();

    private void FrontTapped(object sender, TappedRoutedEventArgs e)
    {
        Vm.TopMost = !CurrentContext.OverlappedPresenter.IsAlwaysOnTop;
        RootTeachingTip.ShowAndHide(
            Vm.TopMost ? MainPanelResources.TopMostOn : MainPanelResources.TopMostOff,
            TeachingTipSeverity.Information,
            Emoticon.Okay);
    }

    private async void SettingTapped(object sender, IWinRTObject e) => await DialogSetting.ShowAsync();

    #endregion

    #region 导航区按钮

    private async void GoBackTapped(object sender, TappedRoutedEventArgs e)
    {
        await WebView.GoBackAsync();
    }

    private async void AddressBoxOnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await WebView.GotoAsync(sender.Text);
    }

    private void WebViewOnPageLoaded(WebView2ForVideo sender, EventArgs e)
    {
        if (sender.HasVideo)
        {
            Vm.TotalTime = sender.Duration;
            TrySetPlaybackRate();
        }
    }

    #endregion

    #region Import区按钮

    private async void ImportTapped(object sender, TappedRoutedEventArgs e)
    {
        if (await DialogInput.ShowAsync() is not { } cId)
            return;

        RootTeachingTip.ShowAndHide(MainPanelResources.DanmakuLoading, TeachingTipSeverity.Information, Emoticon.Okay);

        try
        {
            await LoadDanmaku(async token =>
            {
                var tempPool = new List<Danmaku>();
                for (var i = 0; ; ++i)
                {
                    await using var danmaku = await BiliApis.GetDanmaku(cId, i + 1, token);
                    if (danmaku is null)
                        break;
                    var reply = Serializer.Deserialize<DmSegMobileReply>(danmaku);
                    tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
                }

                return tempPool;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            RootTeachingTip.ShowAndHide(Emoticon.Shocked + " " + MainPanelResources.UnknownException, TeachingTipSeverity.Error, ex.Message);
        }
    }

    private async void FileTapped(object sender, TappedRoutedEventArgs e)
    {
        var file = await PickerHelper.PickSingleFileAsync();
        if (file is not null)
            await LoadDanmaku(async token =>
            {
                await using var stream = File.OpenRead(file.Path);
                return BiliHelper.ToDanmaku(await XDocument.LoadAsync(stream, LoadOptions.None, token)).ToList();
            });
    }

    #endregion

    #region Control区事件

    private void PauseResumeTapped(object sender, IWinRTObject e)
    {
        if (Vm.IsPlaying)
            Pause();
        else
            Resume();
    }

    private async void RewindTapped(object sender, IWinRTObject e)
    {
        if (WebView.HasVideo)
            UpdateTime(await WebView.IncreaseCurrentTimeAsync(-Vm.AppConfig.PlayFastForward));
        else if (Vm.Time - Vm.AppConfig.PlayFastForward < 0)
            Vm.Time = 0;
        else
            Vm.Time -= Vm.AppConfig.PlayFastForward;
    }

    private async void FastForwardTapped(object sender, IWinRTObject e)
    {
        if (WebView.HasVideo)
            UpdateTime(await WebView.IncreaseCurrentTimeAsync(Vm.AppConfig.PlayFastForward));
        else if (Vm.Time + Vm.AppConfig.PlayFastForward > Vm.TotalTime)
            Vm.Time = 0;
        else
            Vm.Time += Vm.AppConfig.PlayFastForward;
    }

    private void DanmakuCanvasCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new(sender, Vm.AppConfig);

    private void DanmakuCanvasDraw(CanvasControl sender, CanvasDrawEventArgs e) => DanmakuHelper.Rendering(sender, e, (float)Vm.Time, Vm.AppConfig);

    // TODO: Time Slider

    // private void TimePointerPressed(object sender, PointerRoutedEventArgs e) => TryPause();

    // private void TimePointerReleased(object sender, PointerRoutedEventArgs e) => TryResume();

    // private void SliderOnManipulationCompleted(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
    // {
    // Debug.WriteLine("ManipulationCompleted");
    // WebView.SetCurrentTimeAsync(Vm.ActualTime);
    // WebView.SetPlaybackRateAsync(3);
    // }

    #endregion

    #region WebView视频控制

    private void PlaybackRateOnTapped(object sender, TappedRoutedEventArgs e)
    {
        Vm.AppConfig.PlaybackRate = double.Parse(sender.To<MenuFlyoutItem>().Text);
        DispatcherTimerHelper.ResetTimerInterval();
        ResetProvider();
        TrySetPlaybackRate();
        AppContext.SaveConfiguration(Vm.AppConfig);
    }

    private async void MuteOnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!WebView.HasVideo)
            return;
        Vm.Mute = await WebView.MutedFlipAsync();
    }

    private double Volume
    {
        get => WebView.HasVideo ? WebView.VolumeAsync().GetAwaiter().GetResult() * 100 : 0;
        set
        {
            // TODO: Volume
            return;
            if (WebView.HasVideo)
                WebView.SetVolumeAsync(value / 100).GetAwaiter().GetResult();
        }
    }

    private void LockWebView2OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!WebView.HasVideo)
            return;
        Vm.LockWebView2 = !Vm.LockWebView2;
    }

    private async void FullScreenOnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!WebView.HasVideo)
            return;
        var button = sender.To<AppBarButton>();
        if (await WebView.FullScreenAsync())
        {
            await WebView.ExitFullScreenAsync();
            button.Icon.To<SymbolIcon>().Symbol = Symbol.FullScreen;
        }
        else
        {
            await WebView.RequestFullScreenAsync();
            button.Icon.To<SymbolIcon>().Symbol = Symbol.BackToWindow;
        }
    }

    #endregion

    #region 进度条时间输入

    private void TimeTextTapped(object sender, TappedRoutedEventArgs e)
    {
        TimeText.Text = DoubleToTimeTextConverter.ToTime(Vm.Time);
        Vm.EditingTime = true;
    }

    private void TimeTextLostFocus(object sender, RoutedEventArgs e) => Vm.EditingTime = false;

    private async void TimeTextInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (TimeSpan.TryParse(TimeText.Text/*.ReplaceLineEndings("")*/, out var result))
        {
            Vm.Time = Math.Max(Math.Min(TimeText.Text.Count(c => c is ':') switch
            {
                0 => result.TotalDays,
                1 => result.TotalMinutes,
                2 => result.TotalSeconds,
                _ => 1
            }, Vm.TotalTime), 0);
            if (WebView.HasVideo)
                await WebView.SetCurrentTimeAsync(Vm.Time);
        }

        Vm.EditingTime = false;
    }

    private void TimeTextIsEditing(object sender, DependencyPropertyChangedEventArgs e)
    {
        var tb = sender.To<TextBox>();
        if (tb.IsEnabled)
        {
            _ = tb.Focus(FocusState.Programmatic);
            tb.SelectAll();
        }
    }

    #endregion

    #endregion
}
