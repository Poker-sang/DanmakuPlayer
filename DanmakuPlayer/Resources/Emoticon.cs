using System;
using System.Collections.Generic;

namespace DanmakuPlayer.Resources;

public static class Emoticon
{
    public static readonly string[] ShockedEmoticons =
    [
        "━━Σ(ﾟДﾟ川)━"
    ];

    public static readonly string[] DepressedEmoticons =
    [
        "( ´･_･)ﾉ(._.`)"
    ];

    public static readonly string[] OkayEmoticons =
    [
        "(｀・ω・´)",
        "✧(≖ ◡ ≖✿)",
        "('ヮ')",
        "(/・ω・)/",
        "(°∀°)ﾉ",
        "(・ω< )★"
    ];

    public static string Shocked => ShockedEmoticons.RandomGet();

    public static string Depressed => DepressedEmoticons.RandomGet();

    public static string Okay => OkayEmoticons.RandomGet();
    private static T RandomGet<T>(this IReadOnlyList<T> arr) => arr[new Random().Next(arr.Count)];
}
