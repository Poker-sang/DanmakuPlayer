using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Views.Converters;

public class BoolToMuteVolumeSymbolConverter : BoolToIconConverter
{
    protected override object TrueValue => Symbol.Volume;

    protected override object FalseValue => Symbol.Mute;
}
