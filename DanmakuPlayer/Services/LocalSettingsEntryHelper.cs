using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using AutoSettingsPage;
using AutoSettingsPage.Models;
using AutoSettingsPage.WinUI;
using DanmakuPlayer.Views.Settings;
using Microsoft.Graphics.Canvas.Text;
using Windows.Foundation.Collections;
using WinUI3Utilities;

namespace DanmakuPlayer.Services;

public static class LocalSettingsEntryHelper
{
    public static ISettingsValueConverter Converter { get; } = new SettingsValueConverter();

    public static void Init()
    {
    }

    static LocalSettingsEntryHelper()
    {
        SettingsEntryHelper.AvailableFonts = CanvasTextFormat.GetSystemFontFamilies();
        SettingsEntryAttribute.SettingsResourceKeysProvider = new SettingsResourceKeysProviderImpl();

        SettingsEntryHelper.FactoryDictionary.AddPredefined<AppConfig>()
            .Add<IntSettingsEntry<AppConfig>, DoubleSliderSettingsCard>()
            .Add<DoubleSettingsEntry<AppConfig>, DoubleSliderSettingsCard>()
            .Add<CookiesSettingsEntry<AppConfig>, CookiesSettingsExpander>()
            .Add<RegexSettingsEntry<AppConfig>, RegexSettingsCard>();
    }

    extension(ISettingsEntry entry)
    {
        public void LocalValueSaving(IPropertySet values)
        {
            if (entry is IReadOnlySingleValueSettingsEntry i)
                if (Converter.TryConvert(i.Value, out var result))
                    values[i.Token] = result;

            if (entry is IMultiValuesSettingsEntry m)
            {
                // WinUI 项目不会嵌套 IMultiValuesSettingsEntry
                foreach (var e in m.Entries)
                    if (e is IReadOnlySingleValueSettingsEntry s)
                        if (Converter.TryConvert(s.Value, out var result))
                            values[s.Token] = result;
            }
        }

        public void LocalValueReset(AppConfig appConfig)
        {
            if (entry is ISettingsValueReset<AppConfig> i)
                i.ValueReset(appConfig);

            if (entry is IMultiValuesSettingsEntry m)
            {
                // WinUI 项目不会嵌套 IMultiValuesSettingsEntry
                foreach (var e in m.Entries)
                    if (e is ISettingsValueReset<AppConfig> s)
                        s.ValueReset(appConfig);
            }
        }
    }

    private class SettingsResourceKeysProviderImpl : ISettingsResourceKeysProvider
    {
        /// <inheritdoc />
        public string this[string resourceKey] => SettingsDialogResources.GetResource(resourceKey);
    }

    extension<TSettings>(ISettingsGroupBuilder<TSettings> builder)
    {
        public ISettingsGroupBuilder<TSettings> Cookies(
            Expression<Func<TSettings, Dictionary<string, string>>> property,
            Action<CookiesSettingsEntry<TSettings>>? config = null) =>
            builder.Add(new(builder.Settings, property), config);

        public ISettingsGroupBuilder<TSettings> Regex(
            Expression<Func<TSettings, ObservableCollection<string>>> property,
            Action<RegexSettingsEntry<TSettings>>? config = null) =>
            builder.Add(new(builder.Settings, property), config);
    }
}
