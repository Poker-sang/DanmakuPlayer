using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.ViewModels;

[SettingsViewModel<AppConfig>(nameof(AppConfig))]
public partial class SettingViewModel : ObservableObject
{
    public AppConfig AppConfig { get; set; } = AppContext.AppConfig with { };

    public ObservableCollection<string> RegexPatterns { get; set; } = [.. AppContext.AppConfig.RegexPatterns];

    /// <summary>
    /// 因为该项只能在下次渲染弹幕时才能生效，所以没必要即时保存
    /// </summary>
    public void SaveCollections() => AppConfig.RegexPatterns = [.. RegexPatterns];

    public void ResetDefault()
    {
        AppConfig = new AppConfig();
        RegexPatterns = [.. AppContext.AppConfig.RegexPatterns];
    }
}
