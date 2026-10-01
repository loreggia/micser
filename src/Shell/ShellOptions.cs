namespace Micser.Shell;

/// <param name="EnginePath">The engine executable; default: Micser.Engine.exe next to the shell. Without one, the shell only connects to a running engine.</param>
/// <param name="UiUrl">Shows the UI from this URL instead of the engine's, e.g. the Vite dev server. The token still comes from the engine.</param>
/// <param name="StartMinimized">Starts in the tray without opening the window (used by the autostart entry).</param>
internal sealed record ShellOptions(string? EnginePath, Uri? UiUrl, bool StartMinimized)
{
    public const string MinimizedArgument = "--minimized";

    /// <summary>
    /// Parses <c>[--engine &lt;path&gt;] [--ui &lt;url&gt;] [--minimized]</c>.
    /// </summary>
    /// <exception cref="ArgumentException">An argument is unknown or misses its value.</exception>
    public static ShellOptions Parse(IReadOnlyList<string> args)
    {
        string? enginePath = null;
        Uri? uiUrl = null;
        var minimized = false;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--engine":
                    enginePath = Value(args, ++i, "--engine");
                    break;
                case "--ui":
                    uiUrl = new Uri(Value(args, ++i, "--ui"), UriKind.Absolute);
                    break;
                case MinimizedArgument:
                    minimized = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        return new ShellOptions(enginePath, uiUrl, minimized);
    }

    private static string Value(IReadOnlyList<string> args, int index, string name)
    {
        return index < args.Count ? args[index] : throw new ArgumentException($"{name} needs a value.");
    }
}
