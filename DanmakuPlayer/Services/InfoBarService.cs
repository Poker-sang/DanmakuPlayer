using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace DanmakuPlayer.Services;

internal class InfoBarService(StackPanel container) : IInfoBarService
{
    public int MaxInfoBarCount { get; set; } = 3;
    public void Show(string? title,
        string message,
        InfoBarSeverity severity,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null)
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
        {
            if (MaxInfoBarCount > 0 && container.Children.Count >= MaxInfoBarCount)
            {
                if (container.Children.FirstOrDefault() is InfoBar oldestInfoBar)
                {
                    oldestInfoBar.IsOpen = false;
                    container.Children.Remove(oldestInfoBar);
                }
            }

            var infoBar = new InfoBar
            {
                Severity = severity,
                Title = title,
                Message = message,
                IsOpen = true,
                IsClosable = isClosable,
            };
            if(action is not null)
            {
                infoBar.ActionButton = new Button
                {
                    Content = actionButtonText,
                    Command = new RelayCommand(action)
                };
            }
            container.Children.Add(infoBar);

            if (delay <= 0) // 小于等于0则不自动关闭
                return;
            await Task.Delay(delay);
            if (infoBar.IsOpen is false) // 因超出容量限制被提前关闭
                return;
            infoBar.IsOpen = false;
            container.Children.Remove(infoBar);
        });
    }
}

public interface IInfoBarService
{
    int MaxInfoBarCount{ get; set; }

    void Show(string? title,
        string message,
        InfoBarSeverity severity,
        int delay = 3000, 
        bool isClosable = true,
        string? actionButtonText = null, 
        Action? action = null);

    void Show(string message,
        InfoBarSeverity severity,
        int delay = 3000, 
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Show(null, message,severity, delay, isClosable,actionButtonText,action);

    void Info(string? title,
        string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Show(title, message, InfoBarSeverity.Informational, delay, isClosable, actionButtonText, action);

    void Success(string? title, 
        string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Show(title, message, InfoBarSeverity.Success, delay, isClosable, actionButtonText, action);

    void Warning(string? title, 
        string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Show(title, message, InfoBarSeverity.Warning, delay, isClosable, actionButtonText, action);

    void Error(string? title,
        string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Show(title, message, InfoBarSeverity.Error, delay, isClosable, actionButtonText, action);

    void Info(string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Info(null, message, delay, isClosable, actionButtonText, action);

    void Success(string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Success(null, message, delay, isClosable, actionButtonText, action);

    void Warning(string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Warning(null, message, delay, isClosable, actionButtonText, action);

    void Error(string message,
        int delay = 3000,
        bool isClosable = true,
        string? actionButtonText = null,
        Action? action = null) => Error(null, message, delay, isClosable, actionButtonText, action);
}
