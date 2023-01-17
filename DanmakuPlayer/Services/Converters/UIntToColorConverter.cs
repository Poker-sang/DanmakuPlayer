using System;
using Microsoft.UI.Xaml.Data;
using Windows.UI;
using WinUI3Utilities;

namespace DanmakuPlayer.Services.Converters;

public class UIntToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value.To<uint>().GetAlphaColor();

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value.To<Color>().GetAlphaUInt();
}
