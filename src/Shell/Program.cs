using Avalonia;
using Avalonia.Controls;
using Avalonia.Themes.Fluent;
using Serilog;
using Velopack;

namespace Micser.Shell;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // first, so Velopack's install, update and uninstall runs exit here; it has hooks on Windows only
        var velopack = VelopackApp.Build();
        if (OperatingSystem.IsWindows())
        {
            velopack = velopack
                .OnAfterInstallFastCallback(InstallHooks.AfterInstall)
                .OnBeforeUpdateFastCallback(InstallHooks.BeforeUpdate)
                .OnBeforeUninstallFastCallback(InstallHooks.BeforeUninstall);
        }

        velopack.Run();

        using var instance = ShellInstance.TryAcquire();
        if (instance == null)
        {
            return 0;
        }

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Micser",
                    "logs",
                    "shell-.log"
                ),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
            )
            .CreateLogger();

        try
        {
            ShellOptions options;
            try
            {
                options = ShellOptions.Parse(args);
            }
            catch (Exception ex) when (ex is ArgumentException or UriFormatException)
            {
                var usage = ex.Message + "\n\nUsage: Micser.Shell [--engine <path>] [--ui <url>] [--minimized]";
                Console.Error.WriteLine(usage);
                ShowError(usage);
                return 1;
            }

            return AppBuilder
                .Configure(() => new ShellApp(options, instance))
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            // e.g. no display; errors while the shell runs are shown by ShellApp
            Log.Fatal(ex, "The shell failed.");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Shows an error before the shell started, in an app of its own.
    /// </summary>
    private static void ShowError(string message)
    {
        try
        {
            AppBuilder
                .Configure<ErrorApp>()
                .UsePlatformDetect()
                .AfterSetup(_ => ErrorApp.Message = message)
                .StartWithClassicDesktopLifetime([], ShutdownMode.OnLastWindowClose);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not show an error.");
        }
    }

    private sealed class ErrorApp : Application
    {
        public static string Message { get; set; } = "";

        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            _ = Dialogs.ShowAsync(null, Message);
            base.OnFrameworkInitializationCompleted();
        }
    }
}
