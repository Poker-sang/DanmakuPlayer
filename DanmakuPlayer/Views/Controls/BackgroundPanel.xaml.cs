using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models.Remote;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.DanmakuServices;
using DanmakuPlayer.Views.ViewModels;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public sealed partial class BackgroundPanel : Grid
{
    private readonly double[] _playbackRates = [2, 1.5, 1.25, 1, 0.75, 0.5];

    public BackgroundPanel()
    {
        try
        {
            Vm.PropertyChanged += (o, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(Vm.Volume):
                        VolumeChanged();
                        break;
                    case nameof(Vm.PlaybackRate):
                        TrySetPlaybackRate();
                        break;
                    case nameof(Vm.CId):
                        _ = OnCIdChangedAsync();
                        break;
                }
            };
            Vm.TempConfig.PropertyChanged += async (o, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(Vm.TempConfig.UsePlaybackRate3):
                        TrySetPlaybackRate();
                        break;
                    case nameof(Vm.TempConfig.IsPlaying):
                        if (o.To<TempConfig>().IsPlaying)
                        {
                            DanmakuHelper.RenderType = RenderMode.RenderAlways;
                            await WebView.LockOperationsAsync(async operations => await operations.PlayAsync());
                        }
                        else
                        {
                            DanmakuHelper.RenderType = RenderMode.RenderOnce;
                            await WebView.LockOperationsAsync(async operations => await operations.PauseAsync());
                        }

                        break;
                }
            };
            Vm.ResetProvider += ResetProvider;

            InitializeComponent();
            AppContext.DanmakuCanvas = DanmakuCanvas;
            AppContext.SetTimerInterval();
            _webViewSyncTimer.Tick += WebViewSyncTimerTick;
            _webViewSyncTimer.Start();
        }
        catch (Exception e)
        {
            App.Logger.LogCritical("", e);
        }
    }

    public IInfoBarService InfoBarService { get; private set; } = null!;
    
    public BackgroundPanelViewModel Vm { get; } = new();

    #region Grid事件

    private void RootLoaded(object sender, RoutedEventArgs e)
    {
        App.Window.SetDragMove(this, new(DragMoveAndResizeMode.Both));
        InfoBarService = IInfoBarService.Create(InfoBarContainer);
    }

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

    private void TopMostTapped(object sender, TappedRoutedEventArgs e)
    {
        Vm.TopMost = !App.OverlappedPresenter.IsAlwaysOnTop;
        InfoBarService.Info(
            Vm.TopMost ? MainPanelResources.TopMostOn : MainPanelResources.TopMostOff,
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
        var url = sender.Text;
        if (string.IsNullOrEmpty(url))
            return;
        StatusChanged(nameof(Vm.Url), url);
        await WebView.GotoAsync(url);
    }

    private void WebViewOnPageLoaded(WebView2ForVideo sender, EventArgs e)
    {
        Vm.TotalTime = TimeSpan.FromSeconds(sender.Duration);
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

            Vm.CId = cId;
            StatusChanged(nameof(Vm.CId), Vm.CId.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            InfoBarService.Error(Emoticon.Shocked + " " + MainPanelResources.UnknownException, ex.Message);
        }
        finally
        {
            Vm.LoadingDanmaku = false;
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

    private async void RemoteTapped(object sender, TappedRoutedEventArgs e) => await DialogRemote.ShowAsync(this);

    #endregion

    #region Control区事件

    private async void PauseResumeTapped(object sender, IWinRTObject e)
    {
        await (Vm.IsPlaying ? PauseAsync() : ResumeAsync());
        StatusChanged();
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

    private static readonly TimeSpan _LargeStep = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan _SmallStep = TimeSpan.FromSeconds(5);

    private void AdvanceDanmakuTapped(object sender, IWinRTObject e)
    {
        Vm.DanmakuDelayTime -= e is RightTappedRoutedEventArgs ? _LargeStep : _SmallStep;
        StatusChanged();
    }

    private void DelayDanmakuTapped(object sender, IWinRTObject e)
    {
        Vm.DanmakuDelayTime += e is RightTappedRoutedEventArgs ? _LargeStep : _SmallStep;
        StatusChanged();
    }

    private void SyncDanmakuTapped(object sender, TappedRoutedEventArgs e)
    {
        Vm.DanmakuDelayTime = TimeSpan.Zero;
        StatusChanged();
    }

    [SuppressMessage("Performance", "CA1822:将成员标记为 static")]
    private void DanmakuCanvasCreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new(sender);

    private void DanmakuCanvasDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs e)
    {
        DispatcherQueue.TryEnqueue(TimerTick);
       
        DanmakuHelper.Rendering(sender, e, Vm.Time - Vm.DanmakuDelayTime, Vm.TempConfig, Vm.AppConfig);
    }

    #endregion

    #region WebView视频控制

    public async void OnMessageReceived(object? sender, Message message)
    {
        switch (message.Type)
        {
            case MessageTypes.Login:
            {
                var info = JsonSerializer.Deserialize<LoginInfo>(message.Data);
                DialogRemote.ConnectedCount = info!.Current.TotalConnectedClients;
                InfoBarService.Info(string.Format(MainPanelResources.RemoteUserLogin, info.UserName), Emoticon.Okay);
                break;
            }
            case MessageTypes.Exit:
            {
                var info = JsonSerializer.Deserialize<LoginInfo>(message.Data);
                DialogRemote.ConnectedCount = info!.Current.TotalConnectedClients;
                InfoBarService.Info(string.Format(MainPanelResources.RemoteUserExit, info.UserName),
                    Emoticon.Depressed);
                break;
            }
            case MessageTypes.StatusUpdate:
            {
                Status = JsonSerializer.Deserialize<RemoteStatus>(message.Data);
                break;
            }
            case MessageTypes.SendCurrentStatus:
            {
                await Task.Delay(500);
                var newStatus = Status;
                newStatus.ChangedValues["Url"] = Vm.Url;
                await RemoteService.Current!.SendStatusAsync(newStatus);
                break;
            }
            default:
                break;
        }
    }

    private async Task SyncAsync()
    {
        await WebView.LockOperationsAsync(async operations =>
        {
            Vm.Time = TimeSpan.FromSeconds(await operations.CurrentTimeAsync());
            Vm.TotalTime = TimeSpan.FromSeconds(await operations.DurationAsync());
            Vm.IsPlaying = await operations.IsPlayingAsync();
            Vm.Volume = await operations.VolumeAsync();
            Vm.Mute = await operations.MutedAsync();
            Vm.FullScreen = await operations.FullScreenAsync();
        });
        TrySetPlaybackRate();

        if (Vm.EnableWebView2 && !WebView.HasVideo)
        {
            Vm.Volume = 0;
            Vm.TotalTime = Vm.Time = TimeSpan.Zero;
            Vm.FullScreen = Vm.IsPlaying = Vm.Mute = false;
        }
    }

    private void PlaybackRateOnSelectionChanged(RadioMenuFlyout sender)
    {
        var value = sender.SelectedItem.To<double>();
        if (value <= 0)
            value = 1;
        Vm.PlaybackRate = value;
        StatusChanged();
    }

    private async void CurrentVideoOnSelectionChanged(RadioMenuFlyout obj)
    {
        await SyncAsync();
        StatusChanged(nameof(Vm.Duration), Vm.Duration.ToString(CultureInfo.InvariantCulture));
    }

    private async void MuteOnTapped(object sender, TappedRoutedEventArgs e) =>
        await WebView.LockOperationsAsync(async operations => Vm.Mute = await operations.MutedFlipAsync());

    private async void VideoSliderOnUserValueChangedByManipulation(object sender, EventArgs e) =>
        await WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(Vm.Time.TotalSeconds));

    private void VideoSliderOnSliderManipulationCompleted(object sender, EventArgs e) => StatusChanged();

    private async void VolumeChanged() =>
        await WebView.LockOperationsAsync(async operations => await operations.SetVolumeAsync(Vm.Volume));

    private async void LoadSyncOnTapped(object sender, TappedRoutedEventArgs e)
    {
        await WebView.LoadVideoAsync();
        await SyncAsync();
    }

    private void LockWebView2OnTapped(object sender, TappedRoutedEventArgs e) => Vm.LockWebView2 = !Vm.LockWebView2;

    private async void FullScreenOnTapped(object sender, TappedRoutedEventArgs e)
    {
        await WebView.LockOperationsAsync(async operations =>
        {
            Vm.FullScreen = await operations.FullScreenFlipAsync();
            if (AppContext.AppConfig.ClearStyleWhenFullScreen)
                if (Vm.FullScreen)
                    await operations.ClearControlsAsync();
                else
                    await operations.RestoreControlsAsync();
        });
    }

    private void GridOnPointerReleased(object sender, PointerRoutedEventArgs e) => WebView.WebView2PointerReleased(sender, e);

    #endregion

    #region 进度条时间输入

    private void TimeTextTapped(object sender, TappedRoutedEventArgs e)
    {
        TimeText.Text = C.ToTimeString(Vm.Time);
        Vm.EditingTime = true;
    }

    private void TimeTextLostFocus(object sender, RoutedEventArgs e) => Vm.EditingTime = false;

    private void TimeTextInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (TimeSpan.TryParse(TimeText.Text/*.ReplaceLineEndings("")*/, out var result))
        {
            Vm.Time = TimeSpan.FromSeconds(Math.Clamp(0, Vm.TotalTime.TotalSeconds, TimeText.Text.Count(c => c is ':') switch
            {
                0 => result.TotalDays,
                1 => result.TotalMinutes,
                2 => result.TotalSeconds,
                _ => 1
            }));
            _ = WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(Vm.Time.TotalSeconds));
            StatusChanged();
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

    private void SecondToTimeSpan(double d) => Vm.Time = TimeSpan.FromSeconds(d);
}
