using Serilog;

namespace Micser.Shell;

internal static class Program
{
    // "Local\" scopes the name to the session; the event shows the window of the running shell
    private const string ActivationEventName = @"Local\Micser.Shell";

    [STAThread]
    private static int Main(string[] args)
    {
        using var activation = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            activation.Set();
            return 0;
        }

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "logs", "shell-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
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
                MessageBox.Show(ex.Message + "\n\nUsage: Micser.Shell [--engine <path>] [--ui <url>] [--minimized]", "Micser", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            ApplicationConfiguration.Initialize();

            // async code started before Application.Run (the engine supervisor) must continue on the UI thread
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

            using var http = new HttpClient();
            var enginePath = options.EnginePath ?? Path.Combine(AppContext.BaseDirectory, "Micser.Engine.exe");
            using var supervisor = new EngineSupervisor(new EngineLocator(EngineLocator.DefaultDiscoveryPath, http), http, enginePath);

            Application.Run(new TrayApplicationContext(options, supervisor, activation));
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "The shell failed.");
            MessageBox.Show(
                $"Micser stopped because of an error: {ex.Message}\n\nThe log is in %LOCALAPPDATA%\\Micser\\logs.",
                "Micser",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
