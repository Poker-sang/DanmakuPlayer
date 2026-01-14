using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;

namespace DanmakuPlayer.Resources;

public static class ConstantStrings
{
    public const string AuthorUri = "https://github.com/Poker-sang/";

    public const string AppName = nameof(DanmakuPlayer);

    public const string RepositoryUri = AuthorUri + AppName;

    public const string LicenseUri = RepositoryUri + "/blob/master/LICENSE";

    public const string StoreUri = "ms-windows-store://pdp/?ProductId=9PMCJD6FLBZS";

    public const string Mail = "poker_sang@outlook.com";

    public const string MailToUri = "mailto:" + Mail;

    public const string QqUin = "2639914082";

    public const string QqUri = $"http://wpa.qq.com/msgrd?v=3&uin={QqUin}&site=qq&menu=yes";

    public static string DefaultFont => AppInfoResources.DefaultFont;

    public static string Author => AppInfoResources.Author;

    public static string AppTitle => AppInfoResources.AppTitle;

    public static string AppVersion { get; } = Package.Current.Id.Version.Let(t => $"{t.Major}.{t.Minor}.{t.Build}.{t.Revision}");

    public static IReadOnlyList<string> FontFamilies { get; }

    static ConstantStrings()
    {
        using var collection = new InstalledFontCollection();
        FontFamilies = [.. collection.Families.Select(t => t.Name)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TOut Let<TIn, TOut>(this TIn obj, Func<TIn, TOut> block) => block(obj);
}
