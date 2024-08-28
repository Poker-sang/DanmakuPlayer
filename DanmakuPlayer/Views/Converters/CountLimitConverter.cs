using System;
using Microsoft.UI.Xaml.Data;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public partial class CountLimitConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var v = value.To<double>();
        return v switch
        {
            -1 => SettingsDialogResources.Unlimited,
            > -1 => (int)v,
            _ => ThrowHelper.ArgumentOutOfRange<double, string>(v)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => ThrowHelper.NotSupported<object>();
}
