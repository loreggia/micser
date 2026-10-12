using System.Net.Sockets;
using System.Runtime.Versioning;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The one shell of the user's session. A second start asks the first to show its window (<see cref="ActivationRequested"/>) and exits.
/// Windows uses a named event (<c>Local\</c> scopes it to the session); elsewhere a lock file and a Unix socket in the runtime folder,
/// which only the user can access.
/// </summary>
internal sealed class ShellInstance : IDisposable
{
    private const string EventName = @"Local\Micser.Shell";

    private readonly CancellationTokenSource _cancellation = new();
    private readonly EventWaitHandle? _event;
    private readonly FileStream? _lock;
    private readonly RegisteredWaitHandle? _registration;
    private readonly Socket? _socket;
    private readonly string? _socketPath;

    [SupportedOSPlatform("windows")]
    private ShellInstance(EventWaitHandle activation)
    {
        _event = activation;
        _registration = ThreadPool.RegisterWaitForSingleObject(
            activation,
            (_, _) => ActivationRequested?.Invoke(this, EventArgs.Empty),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false
        );
    }

    private ShellInstance(FileStream lockFile, Socket socket, string socketPath)
    {
        _lock = lockFile;
        _socket = socket;
        _socketPath = socketPath;
        _ = AcceptAsync(socket, _cancellation.Token);
    }

    /// <summary>
    /// Raised on a thread pool thread when another start asked to show the window.
    /// </summary>
    public event EventHandler? ActivationRequested;

    /// <summary>
    /// The folder of the files that only live while the shell and the engine run (lock files, sockets, the engine's discovery file):
    /// <c>$XDG_RUNTIME_DIR/micser</c> where it's set (Linux), otherwise <c>%LocalAppData%\Micser</c>, as the engine's.
    /// </summary>
    public static string RuntimeDirectory =>
        !OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime
            ? Path.Combine(runtime, "micser")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser");

    /// <summary>
    /// Becomes the session's shell, or asks the running one to show its window and returns null.
    /// </summary>
    /// <param name="directory">The folder for the lock file and the socket (not on Windows); default: <see cref="RuntimeDirectory"/>.</param>
    public static ShellInstance? TryAcquire(string? directory = null)
    {
        if (OperatingSystem.IsWindows())
        {
            var activation = new EventWaitHandle(false, EventResetMode.AutoReset, EventName, out var isFirst);
            if (isFirst)
            {
                return new ShellInstance(activation);
            }

            activation.Set();
            activation.Dispose();
            return null;
        }

        directory ??= RuntimeDirectory;
        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var socketPath = Path.Combine(directory, "shell.sock");
        FileStream lockFile;
        try
        {
            lockFile = new FileStream(
                Path.Combine(directory, "shell.lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            );
        }
        catch (IOException)
        {
            Activate(socketPath);
            return null;
        }

        // a socket left behind by a shell that crashed
        File.Delete(socketPath);
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));
        socket.Listen();
        return new ShellInstance(lockFile, socket, socketPath);
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _registration?.Unregister(null);
        _event?.Dispose();
        _socket?.Dispose();
        if (_socketPath != null)
        {
            File.Delete(_socketPath);
        }

        _lock?.Dispose();
        _cancellation.Dispose();
    }

    private static void Activate(string socketPath)
    {
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(socketPath));
        }
        catch (SocketException ex)
        {
            // the other shell is still starting or is exiting
            Log.Warning(ex, "Could not reach the running shell.");
        }
    }

    private async Task AcceptAsync(Socket socket, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = await socket.AcceptAsync(cancellationToken);
                ActivationRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                Log.Warning(ex, "Accepting an activation failed.");
            }
        }
    }
}
