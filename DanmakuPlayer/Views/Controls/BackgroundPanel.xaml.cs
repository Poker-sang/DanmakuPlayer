using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ProtoBuf;
using WinRT;
using WinUI3Utilities;
using RenderMode = DanmakuPlayer.Enums.RenderMode;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class BackgroundPanel : SwapChainPanel
{
    public BackgroundPanel()
    {
        AppContext.BackgroundPanel = this;
        Vm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName is nameof(Vm.Volume))
                VolumeChanged(sender, e);
        };

        InitializeComponent();
        App.Window.SetDragMove(this, new(DragMoveAndResizeMode.Both));
        AppContext.DanmakuCanvas = DanmakuCanvas;

        DispatcherTimerHelper.Tick += TimerTick;

        _filter =
        [
            DanmakuCombiner.Combine,
            DanmakuRegex.Match
        ];
    }
#pragma warning disable CA1822, IDE0079 // 将成员标记为 static
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
    private Color TransparentColor => Color.FromArgb(0xff / 2, 0, 0, 0);
#pragma warning restore CA1822, IDE0079

    public RootViewModel Vm { get; } = new();

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

        await _cancellationTokenSource.CancelAsync();
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

            var renderedCount = await DanmakuHelper.Render(DanmakuCanvas, RenderMode.RenderInit, _cancellationTokenSource.Token);
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

    public async void ReloadDanmaku(RenderMode renderType)
    {
        if (DanmakuHelper.Pool.Length is 0)
            return;

        TryPause();

        await _cancellationTokenSource.CancelAsync();
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

    public void ResetProvider() => ReloadDanmaku(RenderMode.ReloadProvider);

    public void DanmakuFontChanged() => ReloadDanmaku(RenderMode.ReloadFormats);

    #region 播放及暂停

    private DateTime _lastTime;

    private int _tickCount;

    private async void TimerTick(object? sender, object e)
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
                var lastTime = Vm.Time;
                Vm.Time = await WebView.CurrentTimeAsync();
                if (Math.Abs(Vm.Time - lastTime) > 0.5)
                    Sync();
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
        DanmakuHelper.RenderType = RenderMode.RenderAlways;
        Vm.IsPlaying = true;
        if (WebView.HasVideo)
        {
            await WebView.PlayAsync();
            Sync();
        }
    }

    private async void Pause()
    {
        DanmakuHelper.RenderType = RenderMode.RenderOnce;
        Vm.IsPlaying = false;
        if (WebView.HasVideo)
        {
            await WebView.PauseAsync();
            Sync();
        }
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

        ReloadDanmaku(RenderMode.ReloadProvider);
    }

    private void RootUnloaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    #endregion

    #region Title区按钮

    private void CloseTapped(object sender, TappedRoutedEventArgs e) => Application.Current.Exit();

    private void FrontTapped(object sender, TappedRoutedEventArgs e)
    {
        Vm.TopMost = !App.OverlappedPresenter.IsAlwaysOnTop;
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

    private void WebViewOnPageLoaded(WebView2ForVideo sender, VideoEventArgs e)
    {
        Vm.TotalTime = sender.Duration;
        TrySetPlaybackRate();
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
        var file = await App.Window.PickSingleFileAsync();
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

    private void VolumeDownTapped(object sender, IWinRTObject e)
    {
        VolumeUp(-5);
    }

    private void VolumeUpTapped(object sender, IWinRTObject e)
    {
        VolumeUp(5);
    }

    private void RewindTapped(object sender, IWinRTObject e)
    {
        FastForward(-(e is RightTappedRoutedEventArgs ? 90 : Vm.AppConfig.PlayFastForward));
    }

    private void FastForwardTapped(object sender, IWinRTObject e)
    {
        FastForward(e is RightTappedRoutedEventArgs ? 90 : Vm.AppConfig.PlayFastForward);
    }

    private async void FastForward(int fastForwardTime)
    {
        var time = Math.Clamp(Vm.Time + fastForwardTime, 0, Vm.TotalTime);
        if (WebView.HasVideo)
        {
            await WebView.SetCurrentTimeAsync(time);
            Vm.Time = time;
        }
        else
            Vm.Time = time;
    }

    private void VolumeUp(int volumeUp)
    {
        var volume = Math.Clamp(Vm.Volume + volumeUp, 0, 100);
        //see VolumeChanged()
        //if (WebView.HasVideo)
        //{
        //    await WebView.SetVolumeAsync(volume);
        //    Vm.Volume = volume;
        //}
        //else
        Vm.Volume = volume;
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

    private async void Sync()
    {
        _ = await WebView.TryLoadVideoAsync();
        if (WebView.HasVideo)
        {
            Vm.Time = await WebView.CurrentTimeAsync();
            Vm.IsPlaying = await WebView.IsPlayingAsync();
            Vm.Volume = await WebView.VolumeAsync();
            Vm.Mute = await WebView.MutedAsync();
            Vm.FullScreen = await WebView.FullScreenAsync();
            TrySetPlaybackRate();
        }
    }

    /// <summary>
    /// 倍速是面前唯一一个存在于<see cref="AppConfig"/>，且修改后需要本类和Vm类同时更改的属性，比较麻烦，所以没有写到Vm的属性里
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
        if (WebView.HasVideo)
            Vm.Mute = await WebView.MutedFlipAsync();
    }

    private async void VolumeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (WebView.HasVideo)
            await WebView.SetVolumeAsync(Vm.Volume);
    }

    private void LoadSyncOnTapped(object sender, TappedRoutedEventArgs e) => Sync();

    private void LockWebView2OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (WebView.HasVideo)
            Vm.LockWebView2 = !Vm.LockWebView2;
    }

    private async void FullScreenOnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (WebView.HasVideo)
            Vm.FullScreen = await WebView.FullScreenFlipAsync();
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
