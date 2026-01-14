using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AutoSettingsPage.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Settings;

public sealed partial class RegexSettingsCard : IEntryControl<RegexSettingsEntry<AppConfig>>
{
    /// <inheritdoc />
    public RegexSettingsEntry<AppConfig> Entry { get; set; } = null!;

    public RegexSettingsCard() => InitializeComponent();

    private void AddRegexPattern(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        if (string.IsNullOrEmpty(sender.Text))
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Warning;
            RegexErrorInfoBar.Message = SettingsDialogResources.RegexCannotBeEmpty;
            RegexErrorInfoBar.IsOpen = true;
            return;
        }

        if (Entry.Value.Contains(sender.Text))
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Warning;
            RegexErrorInfoBar.Message = SettingsDialogResources.DuplicatesWithExistingRegex;
            RegexErrorInfoBar.IsOpen = true;
            return;
        }

        try
        {
            _ = new Regex(sender.Text);
        }
        catch (RegexParseException ex)
        {
            RegexErrorInfoBar.Severity = InfoBarSeverity.Error;
            RegexErrorInfoBar.Message = ex.Message;
            RegexErrorInfoBar.IsOpen = true;
            return;
        }

        RegexErrorInfoBar.IsOpen = false;
        Entry.Value.Add(sender.Text);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void RegexPatternChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        try
        {
            _ = new Regex(sender.Text);
            sender.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }
        catch (RegexParseException)
        {
            sender.BorderBrush = new SolidColorBrush(Colors.Red);
        }
    }

    private void RemoveTapped(object sender, TappedRoutedEventArgs e) =>
        Entry.Value.Remove(sender.To<FrameworkElement>().GetTag<string>());

}
