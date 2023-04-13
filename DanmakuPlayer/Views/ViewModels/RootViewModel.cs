using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;

namespace DanmakuPlayer.Views.ViewModels;

public partial class RootViewModel : ObservableObject
{
    public void RaiseForegroundChanged() => OnPropertyChanged(nameof(Foreground));

    public uint Foreground
    {
        get => AppConfig.Foreground;
        set => SetProperty(AppConfig.Foreground, value, AppConfig, (@setting, @value) => @setting.Foreground = @value);
    }

    public float DanmakuOpacity
    {
        get => AppConfig.DanmakuOpacity;
        set => SetProperty(AppConfig.DanmakuOpacity, value, AppConfig, (@setting, @value) => @setting.DanmakuOpacity = @value);
    }

    [ObservableProperty] private bool _startPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Time))]
    private double _actualTime;

    public double Time
    {
        get => ActualTime * AppConfig.PlaySpeed;
        set => ActualTime = value / AppConfig.PlaySpeed;
    }

    [ObservableProperty] private double _totalTime;

    [ObservableProperty] private double _defaultInputTime;

    [ObservableProperty] private bool _topMost;

    [ObservableProperty] private bool _pointerInTitleArea;

    [ObservableProperty] private bool _pointerInImportArea;

    [ObservableProperty] private bool _pointerInControlArea;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigateInputtingTime))]
    private bool _inputtingTime;

    public bool NavigateInputtingTime => !InputtingTime;

#pragma warning disable CA1822
    public AppConfig AppConfig => AppContext.AppConfig;

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
#pragma warning restore CA1822
}
