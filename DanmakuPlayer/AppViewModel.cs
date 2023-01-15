using CommunityToolkit.Mvvm.ComponentModel;

namespace DanmakuPlayer;

public partial class AppViewModel : ObservableObject
{
    [ObservableProperty] private bool _isPlaying;

    [ObservableProperty] private double _time;

    [ObservableProperty] private double _totalTime;

    [ObservableProperty] private bool _topMost;

    [ObservableProperty] private bool _pointerInTitleArea;

    [ObservableProperty] private bool _pointerInImportArea;

    [ObservableProperty] private bool _pointerInControlArea;

#pragma warning disable CA1822
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822
}
