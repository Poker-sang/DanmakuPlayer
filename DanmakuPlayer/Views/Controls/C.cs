using System;
using System.Collections;
using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;
using FluentIcons.Common;
using WinUI3Utilities;
using Symbol = FluentIcons.Common.Symbol;

namespace DanmakuPlayer.Views.Controls;

/// <summary>
/// Converters
/// </summary>
public static class C
{
    public static bool Negation(bool value) => !value;

    public static bool IsNull(object? value) => value is null;

    public static bool IsNotNull(object? value) => value is not null;

    public static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility ToVisibilityNegation(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility IsNullToVisibility(object? value) => value is null ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility IsNotNullToVisibility(object? value) => value is null ? Visibility.Collapsed : Visibility.Visible;

    public static bool IsNotZero(int value) => value is not 0;

    public static bool IsNotZeroL(long value) => value is not 0;

    public static Visibility IsNotZeroDToVisibility(double value) => value is not 0 ? Visibility.Visible : Visibility.Collapsed;

    public static string CountLimitToolTip(double value) =>
        value switch
        {
            -1 => SettingsDialogResources.Unlimited,
            > -1 => ((int)value).ToString(),
            _ => ThrowHelper.ArgumentOutOfRange<double, string>(value)
        };

    public static Visibility IsNotEmptyToVisibility(ICollection value) => value.Count is 0 ? Visibility.Visible : Visibility.Collapsed;

    public static string SecondToTime(double sec)
    {
        var time = TimeSpan.FromSeconds(sec);
        return ToTimeString(time);
    }

    public static double ToSecondDouble(TimeSpan timeSpan) => timeSpan.TotalSeconds;

    public static string ToTimeString(TimeSpan time)
    {
        return time is { Hours: 0 } ? time.ToString(@"mm\:ss") : time.ToString(@"hh\:mm\:ss");
    }

    public static unsafe Color ToAlphaColor(uint color)
    {
        var ptr = &color;
        var c = (byte*)ptr;
        return Color.FromArgb(c[3], c[2], c[1], c[0]);
    }

    public static SolidColorBrush ToSolidColorBrush(uint value) => new(ToAlphaColor(value));

    public static unsafe uint ToAlphaUInt(Color color)
    {
        uint ret;
        var ptr = &ret;
        var c = (byte*)ptr;
        c[0] = color.B;
        c[1] = color.G;
        c[2] = color.R;
        c[3] = color.A;
        return ret;
    }

    public static string StringFormatter(object value, string formatter) => string.Format(formatter, value);

    public static Symbol SymbolSelector(bool value, Symbol trueValue, Symbol falseValue) =>
        value ? trueValue : falseValue;

    public static Symbol PlaybackRateSymbolSelector(double playbackRate) => playbackRate switch
    {
        2 => Symbol.Multiplier2x,
        >= 1.75 => Symbol.Multiplier18x,
        >= 1.5 => Symbol.Multiplier15x,
        >= 1.25 => Symbol.Multiplier12x,
        >= 1 => Symbol.Multiplier1x,
        _ => Symbol.Multiplier5x
    };

    public static Symbol VolumeSymbolSelector(bool isMute, double volume) => isMute
        ? Symbol.SpeakerMute
        : volume switch
        {
            > 50 => Symbol.Speaker2,
            > 0 => Symbol.Speaker1,
            _ => Symbol.Speaker0
        };

    public static IconVariant ColorIconSelector(bool value) => value ? IconVariant.Color : IconVariant.Regular;

    public static string CultureDateTimeDateFormatter(DateTime value, CultureInfo culture) =>
        value.ToString(culture.DateTimeFormat.ShortDatePattern);

    public static string CultureDateTimeOffsetDateFormatter(DateTimeOffset value, CultureInfo culture) =>
        value.ToString(culture.DateTimeFormat.ShortDatePattern);

    public static string CultureDateTimeFormatter(DateTime value, CultureInfo culture) =>
        value.ToString(culture.DateTimeFormat.FullDateTimePattern);

    public static string CultureDateTimeOffsetFormatter(DateTimeOffset value, CultureInfo culture) =>
        value.ToString(culture.DateTimeFormat.FullDateTimePattern);

    public static FontFamily ToFontFamily(string value) => new(value);

    public static string ToPercentageString(object value, int precision)
    {
        var p = "F" + precision;
        return value switch
        {
            uint i => (i * 100).ToString(p),
            int i => (i * 100).ToString(p),
            short i => (i * 100).ToString(p),
            ushort i => (i * 100).ToString(p),
            long i => (i * 100).ToString(p),
            ulong i => (i * 100).ToString(p),
            float i => (i * 100).ToString(p),
            double i => (i * 100).ToString(p),
            decimal i => (i * 100).ToString(p),
            _ => "NaN"
        } + "%";
    }

    public static string PlusOneToString(int value) => (value + 1).ToString();

    public static CommandBarLabelPosition LabelIsNullToVisibility(string? value) =>
        value is null ? CommandBarLabelPosition.Collapsed : CommandBarLabelPosition.Default;

    public static CommandBarLabelPosition BoolToLabelVisibility(bool value) =>
        value ? CommandBarLabelPosition.Default : CommandBarLabelPosition.Collapsed;

    public static ItemsViewSelectionMode ToSelectionMode(bool value) =>
        value ? ItemsViewSelectionMode.Multiple : ItemsViewSelectionMode.None;

    public static string IntEllipsis(int value) =>
        value < 1000 ? value.ToString() : $"{value / 1000d:0.#}k";

    public static double DoubleComplementary(double value) => 1 - value;

    public static FontWeight ToFontWeight(Enum value) => value.GetHashCode() switch
    {
        0 => FontWeights.Thin,
        1 => FontWeights.ExtraLight,
        2 => FontWeights.Light,
        3 => FontWeights.SemiLight,
        4 => FontWeights.Normal,
        5 => FontWeights.Medium,
        6 => FontWeights.SemiBold,
        7 => FontWeights.Bold,
        8 => FontWeights.ExtraBold,
        9 => FontWeights.Black,
        10 => FontWeights.ExtraBlack,
        _ => ThrowHelper.ArgumentOutOfRange<Enum, FontWeight>(value)
    };

    public static Thickness ThicknessSelector(bool value,
        int left, int top, int right,int bottom,
        int left2, int top2, int right2, int bottom2)
    {
        return value 
            ? new Thickness(left, top, right, bottom) 
            : new Thickness(left2, top2, right2, bottom2);
    }
}
