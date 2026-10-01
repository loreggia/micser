using System.Diagnostics;
using System.Net.Http.Headers;
using Serilog;

namespace Micser.Shell;

internal enum EngineState
{
    /// <summary>
    /// Looking for the engine or waiting for a started engine to come up.
    /// </summary>
    Starting,

    Running,

    /// <summary>
    /// No engine is running and the shell can't start one (no executable, or it keeps stopping).
    /// </summary>
    Unavailable,
}

/// <summary>
/// Keeps track of the engine: starts it if it isn't running, restarts it after a crash and reports when its
/// address or token changes. Runs on the UI thread; events are raised there.
/// </summary>
internal sealed class EngineSupervisor : IDisposable
{
    private const int MaxRestarts = 3;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RestartWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(20);

    private readonly CancellationTokenSource _cancellation = new();
    private readonly string? _enginePath;
    private readonly HttpClient _http;
    private readonly Queue<DateTime> _launches = new();
    private readonly EngineLocator _locator;
    private DateTime? _launchedAt;
    private bool _isShuttingDown;

    public EngineSupervisor(EngineLocator locator, HttpClient http, string? enginePath)
    {
        _locator = locator;
        _http = http;
        _enginePath = enginePath != null && File.Exists(enginePath) ? Path.GetFullPath(enginePath) : null;
    }

    /// <summary>
    /// Raised when <see cref="Engine"/> or <see cref="State"/> changes.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// The running engine, or null.
    /// </summary>
    public EngineInfo? Engine { get; private set; }

    public EngineState State { get; private set; } = EngineState.Starting;

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    /// <summary>
    /// Stops the engine gracefully and waits until its process exits. Supervision ends.
    /// </summary>
    public async Task ShutdownEngineAsync()
    {
        _isShuttingDown = true;
        await _cancellation.CancelAsync();

        var engine = Engine ?? await _locator.FindAsync();
        if (engine == null)
        {
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(engine.Url, "/api/engine/shutdown"));
            if (engine.Token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", engine.Token);
            }

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var process = Process.GetProcessById(engine.ProcessId);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or ArgumentException or OperationCanceledException)
        {
            Log.Warning(ex, "Stopping the engine failed.");
        }
    }

    /// <summary>
    /// Starts supervising. Call on the UI thread.
    /// </summary>
    public void Start()
    {
        _ = SuperviseAsync(_cancellation.Token);
    }

    private bool CanLaunch()
    {
        while (_launches.Count > 0 && DateTime.UtcNow - _launches.Peek() > RestartWindow)
        {
            _launches.Dequeue();
        }

        return _enginePath != null && !_isShuttingDown && _launches.Count < MaxRestarts;
    }

    private void Launch()
    {
        Log.Information("Starting the engine: {Path}", _enginePath);
        _launches.Enqueue(DateTime.UtcNow);
        _launchedAt = DateTime.UtcNow;

        try
        {
            // not a child of the shell: the engine keeps running when the shell exits
            using var process = Process.Start(new ProcessStartInfo(_enginePath!)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_enginePath),
            });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Error(ex, "Starting the engine failed.");
        }
    }

    private void Set(EngineInfo? engine, EngineState state)
    {
        if (engine == Engine && state == State)
        {
            return;
        }

        if (engine != Engine)
        {
            Log.Information("Engine {State}: {Url}", state, engine?.Url);
        }

        Engine = engine;
        State = state;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task SuperviseAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var engine = await _locator.FindAsync(cancellationToken);
                if (engine != null)
                {
                    _launchedAt = null;
                    Set(engine, EngineState.Running);
                }
                else if (_launchedAt != null && DateTime.UtcNow - _launchedAt < StartTimeout)
                {
                    Set(null, EngineState.Starting);
                }
                else if (CanLaunch())
                {
                    Set(null, EngineState.Starting);
                    Launch();
                }
                else
                {
                    if (_launchedAt != null)
                    {
                        Log.Error("The engine didn't start or keeps stopping; see its log in %LOCALAPPDATA%\\Micser\\logs.");
                        _launchedAt = null;
                    }

                    Set(null, _enginePath == null ? EngineState.Starting : EngineState.Unavailable);
                }

                await Task.Delay(engine == null ? PollInterval / 4 : PollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
