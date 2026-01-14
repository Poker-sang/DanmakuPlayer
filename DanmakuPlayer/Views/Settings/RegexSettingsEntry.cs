using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using AutoSettingsPage;
using AutoSettingsPage.Models;
using FluentIcons.Common;

namespace DanmakuPlayer.Views.Settings;

public partial class RegexSettingsEntry<TSettings> : CollectionSettingsEntry<TSettings, string>
{
    /// <inheritdoc />
    public RegexSettingsEntry(
        TSettings settings,
        string token,
        string header,
        string description,
        Symbol icon,
        string? placeholder,
        Func<TSettings, ObservableCollection<string>> getter,
        Action<TSettings, ObservableCollection<string>> setter)
        : base(settings, token, header, description, icon, placeholder, getter, setter)
    {
    }

    /// <inheritdoc />
    public RegexSettingsEntry(
        TSettings settings,
        string token,
        SettingsEntryAttribute attribute,
        Func<TSettings, ObservableCollection<string>> getter,
        Action<TSettings, ObservableCollection<string>> setter)
        : base(settings, token, attribute, getter, setter)
    {
    }

    /// <inheritdoc />
    public RegexSettingsEntry(TSettings settings, Expression<Func<TSettings, ObservableCollection<string>>> property)
        : base(settings, property)
    {
    }
}
