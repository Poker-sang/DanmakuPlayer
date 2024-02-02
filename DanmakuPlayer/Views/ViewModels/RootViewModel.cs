using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;

namespace DanmakuPlayer.Views.ViewModels;

public partial class RootViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NegativeEditingTime))]
    private bool _editingTime;

    /// <summary>
    /// 进度条时间
    /// </summary>
    [ObservableProperty] private double _time;

    [ObservableProperty] private bool _fullScreen;

    [ObservableProperty] private bool _mute;

    [ObservableProperty] private bool _startPlaying;

    [ObservableProperty] private double _totalTime;

    [ObservableProperty] private double _volume;

    [ObservableProperty] private bool _loadingDanmaku;

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
                // 如果是改为3倍速，临时保存原来的倍速
                case 3 when AppConfig.PlaybackRate is 3:
                    return;
                case 3:
                    _tempPlaybackRate = AppConfig.PlaybackRate;
                    AppConfig.PlaybackRate = value;
                    break;
                // 如果是改为-1倍速，且原来是3倍速，恢复原来的倍速
                case -1 when AppConfig.PlaybackRate is not 3:
                    return;
                case -1:
                    AppConfig.PlaybackRate = _tempPlaybackRate;
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
                    OnPropertyChanged();
                    break;
                }
            }
            DispatcherTimerHelper.ResetTimerInterval();
            OnPropertyChanged(nameof(PlaybackRateString));
        }
    }

    public string PlaybackRateString => AppConfig.PlaybackRate.ToString("F2");

    /// <summary>
    /// 现实时间
    /// </summary>
    public double ActualTime
    {
        get => Time / AppConfig.PlaybackRate;
        set => Time = value * AppConfig.PlaybackRate;
    }

    public bool NegativeEditingTime => !EditingTime;

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
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822

    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
}
