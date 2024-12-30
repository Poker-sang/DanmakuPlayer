using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;

namespace DanmakuPlayer.Views.ViewModels;

public partial class BackgroundPanelViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool EditingTime { get; set; }

    /// <summary>
    /// 进度条时间
    /// </summary>
    [ObservableProperty]
    public partial double Time { get; set; }

    [ObservableProperty]
    public partial bool FullScreen { get; set; }

    [ObservableProperty]
    public partial bool Mute { get; set; }

    [ObservableProperty]
    public partial bool StartPlaying { get; set; }

    [ObservableProperty]
    public partial double TotalTime { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial bool LoadingDanmaku { get; set; }

    [ObservableProperty]
    public partial float DanmakuDelayTime { get; set; }

    public bool EnableWebView2 => AppConfig.EnableWebView2;

    public bool LockWebView2
    {
        get => AppConfig.LockWebView2;
        set
        {
            if (value == AppConfig.LockWebView2)
                return;
            AppConfig.LockWebView2 = value;
            OnPropertyChanged();
            AppContext.SaveConfiguration(AppConfig);
        }
    }

    public bool TopMost
    {
        get => AppConfig.TopMost;
        set
        {
            if (value == AppConfig.TopMost &&
                value == App.OverlappedPresenter.IsAlwaysOnTop)
                return;
            App.OverlappedPresenter.IsAlwaysOnTop = AppConfig.TopMost = value;
            OnPropertyChanged();
            AppContext.SaveConfiguration(AppConfig);
        }
    }

    public bool IsMaximized
    {
        get => App.OverlappedPresenter.State is OverlappedPresenterState.Maximized;
        set
        {
            if (value == IsMaximized)
                return;
            if (value)
                App.OverlappedPresenter.Maximize();
            else
                App.OverlappedPresenter.Restore();
            OnPropertyChanged();
        }
    }

    private double _tempPlaybackRate;

    private int _tempDuration;

    /// <summary>
    /// 设为3倍速时，临时保存原来的倍速；
    /// 设为-1倍速时，恢复原来的倍速；
    /// 3倍速时，改为别的倍速时，改变临时保存的倍速；
    /// </summary>
    public double PlaybackRate
    {
        get => AppConfig.PlaybackRate;
        set
        {
            switch (value)
            {
                case 3 when AppConfig.PlaybackRate is 3:
                    return;
                // 如果是改为3倍速，临时保存原来的倍速
                case 3:
                    _tempPlaybackRate = AppConfig.PlaybackRate;
                    _tempDuration = AppConfig.DanmakuDuration;
                    AppConfig.DanmakuDuration /= 3;
                    AppConfig.PlaybackRate = value;
                    break;
                case -1 when AppConfig.PlaybackRate is not 3:
                    return;
                // 如果是改为-1倍速，且原来是3倍速，恢复原来的倍速
                case -1:
                    AppConfig.PlaybackRate = _tempPlaybackRate;
                    AppConfig.DanmakuDuration = _tempDuration;
                    break;
                // 如果已经是3倍速，且改为别的倍速，则改变临时保存的倍速
                // ReSharper disable once PatternAlwaysMatches
                case double when AppConfig.PlaybackRate is 3:
                    _tempPlaybackRate = value;
                    return;
                // 其他情况直接改变倍速
                default:
                {
                    // 改变幅度都>=0.25
                    if (Math.Abs(value - AppConfig.PlaybackRate) < 0.2)
                        return;
                    AppConfig.PlaybackRate = value;
                    AppContext.SaveConfiguration(AppConfig);
                    // 临时调整为3倍速时不会触发重新加载弹幕
                    ResetProvider?.Invoke();
                    break;
                }
            }
            DispatcherTimerHelper.ResetTimerInterval();
            OnPropertyChanged();
        }
    }

    public event Action? ResetProvider;

    /// <summary>
    /// 现实时间
    /// </summary>
    public double ActualTime
    {
        get => Time / AppConfig.PlaybackRate;
        set => Time = value * AppConfig.PlaybackRate;
    }

    public bool IsPlaying
    {
        get => DispatcherTimerHelper.IsRunning;
        set
        {
            if (value != DispatcherTimerHelper.IsRunning)
            {
                DispatcherTimerHelper.IsRunning = value;
                OnPropertyChanged();
            }
        }
    }

#pragma warning disable CA1822
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822

    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
}
