using CommunityToolkit.Mvvm.ComponentModel;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.ViewModels;

[SettingsViewModel<AppConfig>(nameof(AppConfig))]
public partial class SettingViewModel : ObservableObject
{
    public AppConfig AppConfig => AppContext.AppConfig;
}
