using System;
using Microsoft.UI.Xaml;

namespace DanmakuPlayer.Services;

public static class DispatcherTimerHelper
{
    private static DispatcherTimer Timer { get; } = new()
    {
        Interval = TimeSpan.FromSeconds(1d / AppContext.AppConfig.PlayFramePerSecond)
    };

    public static bool IsRunning
    {
        get => Timer.IsEnabled;
        set
        {
            if (value)
                Timer.Start();
            else
                Timer.Stop();
        }
    }

    public static void ResetTimerInterval()
    {
        Timer.Interval = TimeSpan.FromSeconds(1d / AppContext.AppConfig.PlayFramePerSecond);
    }

    public static event EventHandler<object> Tick
    {
        add => Timer.Tick += value;
        remove => Timer.Tick -= value;
    }
}
