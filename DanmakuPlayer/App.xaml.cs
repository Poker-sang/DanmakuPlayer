using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DanmakuPlayer.Services;
using DanmakuPlayer.Services.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUI3Utilities;
using WinUIEx;

namespace DanmakuPlayer;

public partial class App : Application
{
    public static MainWindow Window { get; set; } = null!;

    public static OverlappedPresenter OverlappedPresenter => Window.OverlappedPresenter;

    public App()
    {
        SettingsValueConverter.Context = SettingsSerializeContext.Default;
        AppContext.Initialize();
        RegisterUnhandledExceptionHandler();
        HttpClientHelper.Initialize();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Logger.LogTrace("OnLaunched", null);
        try
        {
            Window = new() { SystemBackdrop = new TransparentTintBackdrop() };
            Window.Initialize(new()
            {
                Size = WindowHelper.EstimatedWindowSize(),
                ExtendTitleBar = true,
                BackdropType = BackdropType.None,
                Title = nameof(DanmakuPlayer),
                IconPath = new(AppContext.ApplicationUriToPath(new("ms-appx:///Assets/DanmakuPlayer.ico")))
            });
            Window.RegisterUnhandledExceptionHandler();
            Window.Activate();
        }
        catch (Exception e)
        {
            Logger.LogCritical("", e);
        }
    }

    public static FileLogger Logger { get; private set; } = null!;

    private void RegisterUnhandledExceptionHandler()
    {
        Logger = new(Path.Combine(AppContext.AppLocalFolder, "Logs"));
        DebugSettings.BindingFailed += (o, e) =>
        {
            Logger.LogWarning(e.Message, null);
        };
        DebugSettings.XamlResourceReferenceFailed += (o, e) =>
        {
            Logger.LogWarning(e.Message, null);
        };
        UnhandledException += (o, e) =>
        {
            Logger.LogError(e.Message, e.Exception);
            e.Handled = true;
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
        };
        TaskScheduler.UnobservedTaskException += (o, e) =>
        {
            Logger.LogError(nameof(TaskScheduler.UnobservedTaskException), e.Exception);
            e.SetObserved();
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
        };
        AppDomain.CurrentDomain.UnhandledException += (o, e) =>
        {
            if (e.IsTerminating)
                Logger.LogCritical(nameof(AppDomain.UnhandledException), e.ExceptionObject as Exception);
            else
                Logger.LogError(nameof(AppDomain.UnhandledException), e.ExceptionObject as Exception);
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
            if (e.IsTerminating && Debugger.IsAttached)
                Debugger.Break();
#endif
        };
    }
}
