using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Services;

public interface IInfoBarService
{
    const bool DefaultClosable = false;

    static IInfoBarService Create(StackPanel stackPanel) => new InfoBarService(stackPanel);

    int MaxInfoBarCount { get; set; }

    void Show(string? title,
        string message,
        InfoBarSeverity severity,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null);

    void Show(string message,
        InfoBarSeverity severity,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) => Show(null, message, severity, delay, isClosable, actionButtonText, action);

    void Info(string? title,
        string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) => Show(title, message, InfoBarSeverity.Informational, delay, isClosable,
        actionButtonText, action);

    void Success(string? title,
        string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) =>
        Show(title, message, InfoBarSeverity.Success, delay, isClosable, actionButtonText, action);

    void Warning(string? title,
        string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) =>
        Show(title, message, InfoBarSeverity.Warning, delay, isClosable, actionButtonText, action);

    void Error(string? title,
        string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) =>
        Show(title, message, InfoBarSeverity.Error, delay, isClosable, actionButtonText, action);

    void Info(string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) => Info(null, message, delay, isClosable, actionButtonText, action);

    void Success(string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) => Success(null, message, delay, isClosable, actionButtonText, action);

    void Warning(string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) => Warning(null, message, delay, isClosable, actionButtonText, action);

    void Error(string message,
        int delay = 3000,
        bool isClosable = DefaultClosable,
        string? actionButtonText = null,
        Action? action = null) => Error(null, message, delay, isClosable, actionButtonText, action);
}

file class InfoBarService(StackPanel container) : IInfoBarService
{
    public int MaxInfoBarCount { get; set; } = 2;

    public void Show(string? title,
        string message,
        InfoBarSeverity severity,
        int delay = 3000,
        bool isClosable = IInfoBarService.DefaultClosable,
        string? actionButtonText = null,
        Action? action = null)
    {
        _ = container.DispatcherQueue.TryEnqueue(async () =>
        {
            if (MaxInfoBarCount > 0 && container.Children.Count >= MaxInfoBarCount)
                if (container.Children is [InfoBar oldestInfoBar, ..])
                {
                    oldestInfoBar.IsOpen = false;
                    container.Children.Remove(oldestInfoBar);
                }

            var infoBar = new InfoBar
            {
                Severity = severity,
                Title = title,
                Message = message,
                IsOpen = true,
                IsClosable = isClosable,
            };

            if (action is not null)
                infoBar.ActionButton = new Button
                {
                    Content = actionButtonText,
                    Command = new RelayCommand(action)
                };

            container.Children.Add(infoBar);

            // 小于等于0则不自动关闭
            if (delay <= 0)
                return;
            await Task.Delay(delay);
            _ = container.DispatcherQueue.TryEnqueue(() =>
            {
                if (!infoBar.IsOpen)
                    return;
                infoBar.IsOpen = false;
                container.Children.Remove(infoBar);
            });
        });
    }
}
