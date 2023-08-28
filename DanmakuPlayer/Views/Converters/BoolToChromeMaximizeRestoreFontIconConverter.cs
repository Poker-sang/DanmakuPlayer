using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Views.Converters;

public class BoolToChromeMaximizeRestoreFontIconConverter : BoolToIconConverter
{
    protected override object TrueValue => new FontIcon { Glyph = "\uE923" }; // Restore

    protected override object FalseValue => new FontIcon { Glyph = "\uE922" }; // Maximize
}
