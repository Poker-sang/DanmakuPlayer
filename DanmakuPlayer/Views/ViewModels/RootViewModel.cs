using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.ViewModels;

public partial class RootViewModel : ObservableObject
{
    /// <summary>
    /// 现实时间
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Time))]
    private double _actualTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigateEditingTime))]
    private bool _editingTime;

    [ObservableProperty] private bool _fullScreen;

    [ObservableProperty] private bool _mute;

    [ObservableProperty] private bool _startPlaying;

    [ObservableProperty] private double _totalTime;

    [ObservableProperty] private double _volume;

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
                value == CurrentContext.OverlappedPresenter.IsAlwaysOnTop)
                return;
            CurrentContext.OverlappedPresenter.IsAlwaysOnTop = AppConfig.TopMost = value;
            OnPropertyChanged();
            AppContext.SaveConfiguration(AppConfig);
        }
    }

    public bool IsMaximized
    {
        get => CurrentContext.OverlappedPresenter.State is OverlappedPresenterState.Maximized;
        set
        {
            if (value == IsMaximized)
                return;
            if (value)
                CurrentContext.OverlappedPresenter.Maximize();
            else
                CurrentContext.OverlappedPresenter.Restore();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 进度条时间
    /// </summary>
    public double Time
    {
        get => ActualTime * AppConfig.PlaybackRate;
        set => ActualTime = value / AppConfig.PlaybackRate;
    }

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
    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
}
