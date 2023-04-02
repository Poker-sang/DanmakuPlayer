using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.ViewModels;

[SettingsViewModel<AppConfig>(nameof(AppConfig))]
public partial class SettingViewModel : ObservableObject
{
    public SettingViewModel()
    {
        PatternsCollection = JsonSerializer.Deserialize<ObservableCollection<string>>(RegexPatterns) ?? new ObservableCollection<string>();
        PatternsCollection.CollectionChanged += (_, _) => RegexPatterns = JsonSerializer.Serialize(PatternsCollection);
    }

    public ObservableCollection<string> PatternsCollection { get; }

#pragma warning disable CA1822 // 将成员标记为 static
    public AppConfig AppConfig => AppContext.AppConfig;
#pragma warning restore CA1822 // 将成员标记为 static
}
