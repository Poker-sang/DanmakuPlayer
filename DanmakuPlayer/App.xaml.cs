using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DanmakuPlayer.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUI3Utilities;
using WinUIEx;
using FluentIcons.WinUI;

namespace DanmakuPlayer;

public partial class App : Application
{
    public const ushort RemoteDebuggingPort = 9222;

    public static MainWindow Window { get; set; } = null!;

    public static OverlappedPresenter OverlappedPresenter => Window.OverlappedPresenter;

    public App()
    {
        RegisterUnhandledExceptionHandler();
        this.UseSegoeMetrics();
        SettingsValueConverter.Context = SettingsSerializeContext.Default;
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--remote-debugging-port={RemoteDebuggingPort}");
        InitializeComponent();
        AppContext.Initialize();
        HttpClientHelper.Initialize();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _logger.LogTrace("OnLaunched", null);
        try
        {
            Window = new() { SystemBackdrop = new TransparentTintBackdrop() };
            Window.Initialize(new()
            {
                Size = WindowHelper.EstimatedWindowSize(),
                ExtendTitleBar = true,
                BackdropType = BackdropType.Maintain,
                Title = nameof(DanmakuPlayer),
                IconPath = new(AppContext.ApplicationUriToPath(new("ms-appx:///Assets/DanmakuPlayer.ico")))
            });
            Window.RegisterUnhandledExceptionHandler();
            Window.Activate();
        }
        catch (Exception e)
        {
            _logger.LogCritical("", e);
        }
    }

    public static FileLogger _logger = null!;

    private void RegisterUnhandledExceptionHandler()
    {
        _logger = new FileLogger(@"C:\Users\poker\AppData\Local\Packages\PokerKo.DanmakuPlayer_0wpjzgvbyjvyr\LocalState\Logs");
        // var logger = new FileLogger(Path.Combine(AppContext.AppLocalFolder, "Logs"));
        DebugSettings.BindingFailed += (o, e) =>
        {
            _logger.LogWarning(e.Message, null);
        };
        DebugSettings.XamlResourceReferenceFailed += (o, e) =>
        {
            _logger.LogWarning(e.Message, null);
        };
        UnhandledException += (o, e) =>
        {
            _logger.LogError(e.Message, e.Exception);
            e.Handled = true;
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
        };
        TaskScheduler.UnobservedTaskException += (o, e) =>
        {
            _logger.LogError(nameof(TaskScheduler.UnobservedTaskException), e.Exception);
            e.SetObserved();
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
        };
        AppDomain.CurrentDomain.UnhandledException += (o, e) =>
        {
            if (e.IsTerminating)
                _logger.LogCritical(nameof(AppDomain.UnhandledException), e.ExceptionObject as Exception);
            else
                _logger.LogError(nameof(AppDomain.UnhandledException), e.ExceptionObject as Exception);
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
            if (e.IsTerminating && Debugger.IsAttached)
                Debugger.Break();
#endif
        };
    }
}

public class FileLogger
{
    private readonly string _basePath;

    /// <summary>
    /// Return <see langword="true"/> to cancel the logging
    /// </summary>
    public event Func<FileLogger, LogModel, bool>? Logging;

    public FileLogger(string basePath)
    {
        _basePath = basePath;
        if (!Directory.Exists(_basePath))
            _ = Directory.CreateDirectory(_basePath);
    }

    private async void LogPrivate(LogLevel logLevel, string message, Exception? exception,
        string memberName,
        string filePath,
        int lineNumber)
    {
        var log = new LogModel(
            $"at {memberName} at {filePath}: {lineNumber}",
            logLevel.ToString(),
            message,
            exception);

        if (Logging is not null && Logging(this, log))
            return;

        var logPath = Path.Combine(_basePath, DateTime.Now.ToString("yyyy-MM-dd HH") + ".log");

        await Task.Yield();
        lock (this)
            File.AppendAllText(logPath, log.ToString(), Encoding.UTF8);
    }

    public void Log(LogLevel logLevel, string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(logLevel, message, exception, memberName, filePath, lineNumber);

    public void LogTrace(string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(LogLevel.Trace, message, exception, memberName, filePath, lineNumber);

    public void LogDebug(string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(LogLevel.Debug, message, exception, memberName, filePath, lineNumber);

    public void LogInformation(string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(LogLevel.Information, message, exception, memberName, filePath, lineNumber);

    public void LogWarning(string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(LogLevel.Warning, message, exception, memberName, filePath, lineNumber);

    public void LogError(string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(LogLevel.Error, message, exception, memberName, filePath, lineNumber);

    public void LogCritical(string message, Exception? exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) =>
        LogPrivate(LogLevel.Critical, message, exception, memberName, filePath, lineNumber);
}

public class LogModel
{
    public LogModel(string position, string level, string message, Exception? exception)
    {
        Time = DateTime.Now;
        Position = position;
        Level = level;
        Message = message;
        if (exception is not null)
            Exception = new(exception);
    }

    public DateTime Time { get; }

    public string Position { get; }

    public string Level { get; }

    public string Message { get; }

    public ExceptionModel? Exception { get; }

    public override string ToString() =>
        $"""
         {Time:HH:mm:ss} {Level}
         {Message}
         {Position}
         {Exception?.ToString(1)}
         """;
}

public class ExceptionModel
{
    public ExceptionModel(Exception exception)
    {
        Exception = exception;
        Source = exception.Source;
        Message = exception.Message;
        StackTrace = exception.StackTrace;
        if (exception.InnerException is not null)
            InnerException = new ExceptionModel(exception.InnerException);
    }

    public Exception Exception { get; }

    public string? Source { get; }

    public string Message { get; }

    public string? StackTrace { get; }

    public ExceptionModel? InnerException { get; }

    public string ToString(int indent) =>
        $"""
         {Indent(indent)}Type: {Exception.GetType().FullName}
         {Indent(indent)}Source: {Source}
         {Indent(indent)}Message: {Message}
         {Indent(indent)}StackTrace: 
         {Indent(indent + 1)}{StackTrace?.ReplaceLineEndings(Environment.NewLine + Indent(indent + 1)) ?? "null"}
         {InnerException?.ToString(indent + 1)}
         
         """;

    private static string Indent(int indent) => new('\t', indent);
}

/// <summary>
/// Defines logging severity levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Logs that contain the most detailed messages. These messages may contain sensitive
    /// application data. These messages are disabled by default and should never be
    /// enabled in a production environment.
    /// </summary>
    Trace,

    /// <summary>
    /// Logs that are used for interactive investigation during development. These logs
    /// should primarily contain information useful for debugging and have no long-term
    /// value.
    /// </summary>
    Debug,

    /// <summary>
    /// Logs that track the general flow of the application. These logs should have long-term
    /// value.
    /// </summary>
    Information,

    /// <summary>
    /// Logs that highlight an abnormal or unexpected event in the application flow,
    /// but do not otherwise cause the application execution to stop.
    /// </summary>
    Warning,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure.
    /// These should indicate a failure in the current activity, not an application-wide
    /// failure.
    /// </summary>
    Error,

    /// <summary>
    /// Logs that describe an unrecoverable application or system crash, or a catastrophic
    /// failure that requires immediate attention.
    /// </summary>
    Critical,

    /// <summary>
    /// Not used for writing log messages. Specifies that a logging category should not
    /// write any messages.
    /// </summary>
    None
}
