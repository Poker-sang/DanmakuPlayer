using CommunityToolkit.Mvvm.ComponentModel;
using DanmakuPlayer.Services;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.ViewModels;

public partial class RootViewModel : ObservableObject
{
    public void RaiseForegroundChanged() => OnPropertyChanged(nameof(Foreground));

    public uint Foreground
    {
        get => AppConfig.Foreground;
        set => SetProperty(AppConfig.Foreground, value, AppConfig, (setting, value) => setting.Foreground = value);
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

    public bool TopMost
    {
        get => AppConfig.TopMost;
        set
        {
            if (value == AppConfig.TopMost)
                return;
            CurrentContext.OverlappedPresenter.IsAlwaysOnTop = AppConfig.TopMost = value;
            OnPropertyChanged();
            AppContext.SaveConfiguration(AppConfig);
        }
    }

    [ObservableProperty] private bool _pointerInTitleArea;

    [ObservableProperty] private bool _pointerInImportArea;

    [ObservableProperty] private bool _pointerInControlArea;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigateEditingTime))]
    private bool _editingTime;

    public bool NavigateEditingTime => !EditingTime;

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
