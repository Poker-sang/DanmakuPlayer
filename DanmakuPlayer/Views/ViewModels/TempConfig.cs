using CommunityToolkit.Mvvm.ComponentModel;

namespace DanmakuPlayer.Views.ViewModels;

public partial class TempConfig : ObservableObject
{
    [ObservableProperty]
    public partial bool UsePlaybackRate3 { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = true;
}
