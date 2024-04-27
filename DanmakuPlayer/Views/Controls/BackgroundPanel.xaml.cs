using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using DanmakuPlayer.Enums;
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
using System.Xml.Linq;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class BackgroundPanel : Grid
{
    private readonly double[] _playbackRates = [2, 1.5, 1.25, 1, 0.75, 0.5];

    public BackgroundPanel()
    {
        AppContext.BackgroundPanel = this;
        Vm.PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Vm.Volume):
                    VolumeChanged(sender, e);
                    break;
                case nameof(Vm.PlaybackRate):
                    TrySetPlaybackRate();
                    break;
            }
        };
        Vm.ResetProvider += ResetProvider;

        InitializeComponent();
        AppContext.DanmakuCanvas = DanmakuCanvas;
        DispatcherTimerHelper.Tick += TimerTick;
    }

    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local", Justification = "For {x:Bind}")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "For {x:Bind}")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "For ReSharper")]
    private Color TransparentColor => Color.FromArgb(0xff / 2, 0, 0, 0);

    public BackgroundPanelViewModel Vm { get; } = new();

    #region Grid事件

    private void RootLoaded(object sender, RoutedEventArgs e) => App.Window.SetDragMove(this, new(DragMoveAndResizeMode.Both));

    private void MaximizeRestoreTapped(object sender, RoutedEventArgs e) => Vm.IsMaximized = !Vm.IsMaximized;

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

    /// <summary>
    /// 阻止按钮把双击事件传递给背景
    /// </summary>
    private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => e.Handled = true;

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
        if (string.IsNullOrEmpty(sender.Text))
            return;
        await WebView.GotoAsync(sender.Text);
    }

    private void WebViewOnPageLoaded(WebView2ForVideo sender, EventArgs e)
    {
        Vm.TotalTime = sender.Duration;
        TrySetPlaybackRate();
    }

    #endregion

    #region Import区按钮

    private async void ImportTapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            Vm.LoadingDanmaku = true;

            if (await DialogInput.ShowAsync() is not { } cId)
                return;

            RootTeachingTip.ShowAndHide(MainPanelResources.DanmakuLoading, TeachingTipSeverity.Information, Emoticon.Okay);

            await LoadDanmakuAsync(async token =>
            {
                var tempPool = new List<Danmaku>();
                var danmakuCount = 0;
                var testCount = 0;
                for (var i = 0; ; ++i)
                {
                    try
                    {
                        if (await GetDanmakuAsync(tempPool, cId, i, token, BiliApis.GetWebDanmakuAsync))
                            break;
                    }
                    catch
                    {
                        if (await GetDanmakuAsync(tempPool, cId, i, token, BiliApis.GetMobileDanmaku))
                            break;
                    }

                    // 连续5次获取不到新弹幕（30min）也结束
                    if (tempPool.Count == danmakuCount)
                        testCount += 1;
                    if (testCount >= 5)
                        break;
                    danmakuCount = tempPool.Count;
                }

                return tempPool;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            RootTeachingTip.ShowAndHide(Emoticon.Shocked + " " + MainPanelResources.UnknownException, TeachingTipSeverity.Error, ex.Message);
        }
        finally
        {
            Vm.LoadingDanmaku = false;
        }

        return;

        static async Task<bool> GetDanmakuAsync(List<Danmaku> tempPool, int cId, int i, CancellationToken token, Func<int, int, CancellationToken, Task<Stream?>> getDanmakuAsync)
        {
            await using var danmaku = await getDanmakuAsync(cId, i + 1, token);
            if (danmaku is null)
                return true;
            var reply = Serializer.Deserialize<DmSegMobileReply>(danmaku);
            tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
            return false;
        }
    }

    private async void FileTapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            Vm.LoadingDanmaku = true;

            var file = await App.Window.PickSingleFileAsync();
            if (file is not null)
                await LoadDanmakuAsync(async token =>
                {
                    await using var stream = File.OpenRead(file.Path);
                    return BiliHelper.ToDanmaku(await XDocument.LoadAsync(stream, LoadOptions.None, token)).ToList();
                });
        }
        finally
        {
            Vm.LoadingDanmaku = false;
        }
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

    private void VolumeDownTapped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) => VolumeUp(-5);

    private void VolumeUpTapped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) => VolumeUp(5);

    private void RewindTapped(object sender, IWinRTObject e) => FastForward(e is RightTappedRoutedEventArgs, true);

    private void FastForwardTapped(object sender, IWinRTObject e)
    {
        switch (e)
        {
            case RightTappedRoutedEventArgs:
                FastForward(true, false);
                break;
            case KeyboardAcceleratorInvokedEventArgs when !Vm.IsPlaying:
            case TappedRoutedEventArgs:
                FastForward(false, false);
                break;
        }
    }

    private void AdvanceDanmakuTapped(object sender, IWinRTObject e) => Vm.DanmakuDelayTime -= e is RightTappedRoutedEventArgs ? 90 : 5;

    private void DelayDanmakuTapped(object sender, IWinRTObject e) => Vm.DanmakuDelayTime += e is RightTappedRoutedEventArgs ? 90 : 5;

    private void SyncDanmakuTapped(object sender, TappedRoutedEventArgs e) => Vm.DanmakuDelayTime = 0;

    [SuppressMessage("Performance", "CA1822:将成员标记为 static")]
    private void DanmakuCanvasCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new(sender);

    private void DanmakuCanvasDraw(CanvasControl sender, CanvasDrawEventArgs e) => DanmakuHelper.Rendering(sender, e, (float)Vm.Time + Vm.DanmakuDelayTime, Vm.AppConfig);

    #endregion

    #region WebView视频控制

    private async void Sync()
    {
        await WebView.LockOperationsAsync(async operations =>
        {
            Vm.Time = await operations.CurrentTimeAsync();
            Vm.TotalTime = await operations.DurationAsync();
            Vm.IsPlaying = await operations.IsPlayingAsync();
            Vm.Volume = await operations.VolumeAsync();
            Vm.Mute = await operations.MutedAsync();
            Vm.FullScreen = await operations.FullScreenAsync();
        });
        TrySetPlaybackRate();

        if (Vm.EnableWebView2 && !WebView.HasVideo)
        {
            Vm.TotalTime = Vm.Time = Vm.Volume = 0;
            Vm.FullScreen = Vm.IsPlaying = Vm.Mute = false;
        }
    }

    private void PlaybackRateOnSelectionChanged(RadioMenuFlyout sender) => Vm.PlaybackRate = sender.SelectedItem.To<double>();

    private void CurrentVideoOnSelectionChanged(RadioMenuFlyout obj) => Sync();

    private async void MuteOnTapped(object sender, TappedRoutedEventArgs e) =>
        await WebView.LockOperationsAsync(async operations => Vm.Mute = await operations.MutedFlipAsync());

    private async void VideoSliderOnUserValueChangedByManipulation(object sender, EventArgs e) =>
        await WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(Vm.Time));

    private async void VolumeChanged(object? sender, PropertyChangedEventArgs e) =>
        await WebView.LockOperationsAsync(async operations => await operations.SetVolumeAsync(Vm.Volume));

    private async void LoadSyncOnTapped(object sender, TappedRoutedEventArgs e)
    {
        await WebView.LoadVideoAsync();
        Sync();
    }

    private void LockWebView2OnTapped(object sender, TappedRoutedEventArgs e) => Vm.LockWebView2 = !Vm.LockWebView2;

    private async void FullScreenOnTapped(object sender, TappedRoutedEventArgs e)
    {
        await WebView.LockOperationsAsync(async operations =>
        {
            Vm.FullScreen = await operations.FullScreenFlipAsync();
            if (Vm.FullScreen && AppContext.AppConfig.ClearClassWhenFullScreen)
                await operations.ClearClassAsync();
        });
    }

    private void GridOnPointerReleased(object sender, PointerRoutedEventArgs e) => WebView.WebView2PointerReleased(sender, e);

    #endregion

    #region 进度条时间输入

    private void TimeTextTapped(object sender, TappedRoutedEventArgs e)
    {
        TimeText.Text = DoubleToTimeTextConverter.ToTime(Vm.Time);
        Vm.EditingTime = true;
    }

    private void TimeTextLostFocus(object sender, RoutedEventArgs e) => Vm.EditingTime = false;

    private void TimeTextInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
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
            _ = WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(Vm.Time));
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
}
