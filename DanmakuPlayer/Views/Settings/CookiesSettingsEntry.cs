using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AutoSettingsPage;
using AutoSettingsPage.Models;
using FluentIcons.Common;

namespace DanmakuPlayer.Views.Settings;

public partial class CookiesSettingsEntry<TSettings> : SingleValueSettingsEntry<TSettings, Dictionary<string, string>>
{
    /// <inheritdoc />
    public CookiesSettingsEntry(
        TSettings settings,
        string token,
        string header,
        string description,
        Symbol icon,
        string? placeholder,
        Func<TSettings, Dictionary<string, string>> getter,
        Action<TSettings, Dictionary<string, string>> setter)
        : base(settings, token, header, description, icon, placeholder, getter, setter)
    {
    }

    /// <inheritdoc />
    public CookiesSettingsEntry(
        TSettings settings,
        string token,
        SettingsEntryAttribute attribute,
        Func<TSettings, Dictionary<string, string>> getter,
        Action<TSettings, Dictionary<string, string>> setter)
        : base(settings, token, attribute, getter, setter)
    {
    }

    /// <inheritdoc />
    public CookiesSettingsEntry(TSettings settings, Expression<Func<TSettings, Dictionary<string, string>>> property)
        : base(settings, property)
    {
    }
}
