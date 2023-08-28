using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Views.Converters;

public class BoolToLockUnLockFontIconConverter : BoolToIconConverter
{
    protected override object TrueValue => new FontIcon { Glyph = "\uE785" }; // UnLock

    protected override object FalseValue => new FontIcon { Glyph = "\uE72E" }; // Lock
}
