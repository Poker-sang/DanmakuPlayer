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
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
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
                    case nameof(Vm.FullScreen):
                        FullScreenChanged();
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
            FullScreenChanged();
            AppContext.DanmakuCanvas = DanmakuCanvas;
            AppContext.SetTimerInterval();
            _webViewSyncTimer.Tick += async (_, _) => await SyncAsync();
            _webViewSyncTimer.Start();
        }
        catch (Exception e)
        {
            App.Logger.LogCritical("", e);
        }

        return;

        void FullScreenChanged()
        {
            if (Vm.FullScreen)
            {
                TopRow.Height = new(1, GridUnitType.Star);
                BottomRow.Height = new(1, GridUnitType.Star);
                SetRow(WebView, 0);
                SetRowSpan(WebView, 3);
            }
            else
            {
                TopRow.Height = GridLength.Auto;
                BottomRow.Height = GridLength.Auto;
                SetRow(WebView, 1);
                SetRowSpan(WebView, 1);
            }
        }
    }

    public IInfoBarService InfoBarService { get; private set; } = null!;
    
    public BackgroundPanelViewModel Vm { get; } = new();

    #region Grid事件

    private void RootLoaded(object sender, RoutedEventArgs e)
    {
        App.Window.SetDragMove(this);
        InfoBarService = IInfoBarService.Create(InfoBarContainer);
    }

    private void MaximizeRestoreDoubleTapped(object sender, RoutedEventArgs e) => Vm.IsMaximized = !Vm.IsMaximized;

    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DanmakuHelper.Pool.Count is 0)
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

    private void CloseClick(object sender, RoutedEventArgs e) => Application.Current.Exit();

    private void TopMostClick(object sender, RoutedEventArgs e)
    {
        Vm.TopMost = !App.OverlappedPresenter.IsAlwaysOnTop;
        InfoBarService.Info(
            Vm.TopMost ? MainPanelResources.TopMostOn : MainPanelResources.TopMostOff,
            Emoticon.Okay);
    }

    private async void SettingClick(object sender, RoutedEventArgs e) => await DialogSetting.ShowAsync();

    #endregion

    #region 导航区按钮

    private async void GoBackClick(object sender, RoutedEventArgs e)
    {
        await WebView.GoBackAsync();
    }

    private async void AddressBoxOnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
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

    public async void ImportClick(object sender, RoutedEventArgs e)
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

    public async void FileClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Vm.LoadingDanmaku = true;

            var file = await new FileOpenPicker(App.Window.AppWindow.Id).PickSingleFileAsync();
            if (file is not null)
                await LoadDanmakuAsync(async token =>
                {
                    await using var stream = File.OpenRead(file.Path);
                    return BiliHelper.ToDanmaku(await XDocument.LoadAsync(stream, LoadOptions.None, token)).ToArray();
                });
        }
        finally
        {
            Vm.LoadingDanmaku = false;
        }
    }

    public async void RemoteClick(object sender, RoutedEventArgs e) => await DialogRemote.ShowAsync(this);

    #endregion

    #region Control区事件

    private async void PauseResumeClick(object sender, RoutedEventArgs e)
    {
        await (Vm.IsPlaying ? PauseAsync() : ResumeAsync());
        StatusChanged();
    }

    private void VolumeDownClick(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) => VolumeUp(-5);

    private void VolumeUpClick(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) => VolumeUp(5);

    private void RewindClick(object sender, RoutedEventArgs e) => FastForward(e is RightTappedRoutedEventArgs, true);

    private void FastForwardClick(object sender, IWinRTObject e)
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

    private void AdvanceDanmakuClick(object sender, RoutedEventArgs e)
    {
        Vm.DanmakuDelayTime -= e is RightTappedRoutedEventArgs ? _LargeStep : _SmallStep;
        StatusChanged();
    }

    private void DelayDanmakuClick(object sender, RoutedEventArgs e)
    {
        Vm.DanmakuDelayTime += e is RightTappedRoutedEventArgs ? _LargeStep : _SmallStep;
        StatusChanged();
    }

    private void SyncDanmakuClick(object sender, RoutedEventArgs e)
    {
        Vm.DanmakuDelayTime = TimeSpan.Zero;
        StatusChanged();
    }

    [SuppressMessage("Performance", "CA1822:将成员标记为 static")]
    private void DanmakuCanvasCreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs e) => DanmakuHelper.Current = new(sender);

    private void DanmakuCanvasDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs e)
    {
        DispatcherQueue.TryEnqueue(TimerTick);

        if (Vm.TurnOffDanmaku)
            e.DrawingSession.Clear(Colors.Transparent);
        else
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
                var info = (LoginInfo) JsonSerializer.Deserialize(message.Data, typeof(LoginInfo), RemoteSerializerContext.Default)!;
                DialogRemote.ConnectedCount = info.Current.TotalConnectedClients;
                InfoBarService.Info(string.Format(MainPanelResources.RemoteUserLogin, info.UserName), Emoticon.Okay);
                break;
            }
            case MessageTypes.Exit:
            {
                var info = (LoginInfo) JsonSerializer.Deserialize(message.Data, typeof(LoginInfo), RemoteSerializerContext.Default)!;
                DialogRemote.ConnectedCount = info.Current.TotalConnectedClients;
                InfoBarService.Info(string.Format(MainPanelResources.RemoteUserExit, info.UserName), Emoticon.Depressed);
                break;
            }
            case MessageTypes.StatusUpdate:
            {
                Status = (RemoteStatus) JsonSerializer.Deserialize(message.Data, typeof(RemoteStatus), RemoteSerializerContext.Default)!;
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
        if (!Vm.EnableWebView2)
            return;
        if (WebView.HasVideo)
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
        }
        else
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

    private async void MuteOnClick(object sender, RoutedEventArgs e) =>
        await WebView.LockOperationsAsync(async operations => Vm.Mute = await operations.MutedFlipAsync());

    private async void VideoSliderOnUserValueChangedByManipulation(object sender, EventArgs e) =>
        await WebView.LockOperationsAsync(async operations => await operations.SetCurrentTimeAsync(Vm.Time.TotalSeconds));

    private void VideoSliderOnSliderManipulationCompleted(object sender, EventArgs e) => StatusChanged();

    private async void VolumeChanged() =>
        await WebView.LockOperationsAsync(async operations => await operations.SetVolumeAsync(Vm.Volume));

    private async void LoadSyncOnClick(object sender, RoutedEventArgs e)
    {
        await WebView.LoadVideoAsync();
        await SyncAsync();
    }

    private void TurnOffDanmakuOnClick(object sender, RoutedEventArgs e)
    {
        Vm.TurnOffDanmaku = !Vm.TurnOffDanmaku;
        // 在暂停时也触发清屏
        DanmakuCanvas.Invalidate();
    }

    private void LockWebView2OnClick(object sender, RoutedEventArgs e) => Vm.LockWebView2 = !Vm.LockWebView2;

    private async void FullScreenOnClick(object sender, RoutedEventArgs e)
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

    private void BackgroundPanelOnPointerPressed(object sender, PointerRoutedEventArgs e) => WebView.WebView2PointerReleased(sender, e);

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
