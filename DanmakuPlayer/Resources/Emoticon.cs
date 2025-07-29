using System;
using System.Collections.Immutable;

namespace DanmakuPlayer.Resources;

public static class Emoticon
{
    public static readonly ImmutableArray<string> ShockedEmoticons =
    [
        "━━Σ(ﾟДﾟ川)━"
    ];

    public static readonly ImmutableArray<string> DepressedEmoticons =
    [
        "( ´･_･)ﾉ(._.`)"
    ];

    public static readonly ImmutableArray<string> OkayEmoticons =
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

    private static T RandomGet<T>(this ImmutableArray<T> arr) => arr[Random.Shared.Next(arr.Length)];
}
