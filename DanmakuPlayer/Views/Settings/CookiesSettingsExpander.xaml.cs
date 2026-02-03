using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSettingsPage.WinUI;
using DanmakuPlayer.Services;
using DanmakuPlayer.Views.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Settings;

public sealed partial class CookiesSettingsExpander : IEntryControl<CookiesSettingsEntry<AppConfig>>
{
    /// <inheritdoc />
    public CookiesSettingsEntry<AppConfig> Entry { get; set; } = null!;

    public CookiesSettingsExpander()
    {
        InitializeComponent();
    }

    private void DanmakuClearCookieAppBarButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        Entry.Value = [];
    }

    private async void DanmakuGetCookieFromClipboardAppBarButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var cookieText = await Clipboard.GetContent().GetTextAsync();
        try
        {
            if (JsonSerializer.Deserialize(cookieText, CookieSerializerContext.Default.CookieArray) is { Length: > 2 } cookie)
            {
                Entry.Value = cookie.ToDictionary(c => c.Name, c => c.Value);
                return;
            }
        }
        catch
        {
            // ignored
        }

        var cookiesDict = new Dictionary<string, string>();
        if (cookieText.Split(';') is { Length: > 2 } cookies)
            foreach (var cookie in cookies)
                if (cookie.Split('=') is [var name, var value])
                    cookiesDict[name.Trim()] = value.Trim();
        if (cookiesDict.Count > 2)
            Entry.Value = cookiesDict;
    }

    private async void DanmakuGetCookieFromWebViewAppBarButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var backgroundPanel = App.Window.Content.To<BackgroundPanel>();
        var cookie = await backgroundPanel.WebView.GetBiliCookieAsync();
        Entry.Value = cookie.ToDictionary(c => c.Name, c => c.Value);
    }

    private static IEnumerable<StringPair> GetStringPairs(Dictionary<string, string> dictionary) => dictionary.Select(p => new StringPair(p.Key, p.Value));
}

internal record StringPair(string Name, string Value);

[JsonSerializable(typeof(Cookie[]))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
public partial class CookieSerializerContext : JsonSerializerContext;
