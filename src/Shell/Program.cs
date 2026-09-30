namespace Micser.Shell;

internal static class Program
{
    /// <summary>
    /// Development default until engine discovery exists: the Vite dev server.
    /// </summary>
    private const string DefaultUrl = "http://localhost:5173";

    [STAThread]
    private static void Main(string[] args)
    {
        var url = args.Length > 0 ? new Uri(args[0]) : new Uri(DefaultUrl);

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext(url));
    }
}
