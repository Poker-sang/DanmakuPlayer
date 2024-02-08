using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Converters;

public class StringToFontFamilyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => new FontFamily(value.To<string>());

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value.To<FontFamily>().Source;
}
