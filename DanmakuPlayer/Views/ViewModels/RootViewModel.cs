using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;

namespace DanmakuPlayer.Views.ViewModels;

public partial class RootViewModel : ObservableObject
{
    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

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

    [ObservableProperty] private bool _mute;

    [ObservableProperty] private double _volume;

    [ObservableProperty] private bool _fullScreen;

    [ObservableProperty] private bool _startPlaying;

    /// <summary>
    /// 现实时间
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Time))]
    private double _actualTime;

    /// <summary>
    /// 进度条时间
    /// </summary>
    public double Time
    {
        get => ActualTime * AppConfig.PlaybackRate;
        set => ActualTime = value / AppConfig.PlaybackRate;
    }

    [ObservableProperty] private double _totalTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigateEditingTime))]
    private bool _editingTime;

    public bool NavigateEditingTime => !EditingTime;

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
}
