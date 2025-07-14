using System;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial TimeSpan Time { get; set; }

    [ObservableProperty]
    public partial bool FullScreen { get; set; }

    [ObservableProperty]
    public partial bool Mute { get; set; }

    [ObservableProperty]
    public partial TimeSpan TotalTime { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial bool LoadingDanmaku { get; set; }

    [ObservableProperty]
    public partial TimeSpan DanmakuDelayTime { get; set; }

    [ObservableProperty]
    public partial ulong CId { get; set; }

    [ObservableProperty]
    public partial string Url { get; set; } = "";

    [ObservableProperty]
    public partial double Duration { get; set; }

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

    public double PlaybackRate
    {
        get => AppConfig.PlaybackRate;
        set
        {
            // 改变幅度都>=0.25
            if (Math.Abs(value - AppConfig.PlaybackRate) < 0.2)
                return;
            AppConfig.PlaybackRate = value;
            AppContext.SaveConfiguration(AppConfig);
            ResetProvider?.Invoke();
            OnPropertyChanged();
        }
    }

    public double ActualPlaybackRate => TempConfig.UsePlaybackRate3 ? 3 : AppConfig.PlaybackRate;

    public event Action? ResetProvider;

    /// <summary>
    /// 现实时间
    /// </summary>
    public TimeSpan ActualTime
    {
        get => Time / ActualPlaybackRate;
        set => Time = value * ActualPlaybackRate;
    }

    public bool IsPlaying
    {
        get => !AppContext.DanmakuCanvas.Paused;
        set
        {
            if (value != !AppContext.DanmakuCanvas.Paused)
            {
                AppContext.DanmakuCanvas.Paused = !value;
                OnPropertyChanged();
            }
        }
    }

    public bool ActualIsPlaying => IsPlaying && TempConfig.IsPlaying;

#pragma warning disable CA1822
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822

    public TempConfig TempConfig { get; } = new();

    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
}
