using System.Diagnostics;
using System.Text.Json;

namespace Micser.Shell;

/// <param name="Url">The engine's base URL.</param>
/// <param name="Token">The access token; null if the engine doesn't require one.</param>
internal sealed record EngineInfo(Uri Url, string? Token, int ProcessId);

/// <summary>
/// Finds the running engine through its discovery file. The file stays behind when the engine crashes, so it's only
/// trusted if its process is a running engine that answers the health check.
/// </summary>
internal sealed class EngineLocator
{
    private static readonly TimeSpan HealthTimeout = TimeSpan.FromSeconds(2);

    private readonly HttpClient _http;
    private readonly Func<int, bool> _isEngineProcess;

    public EngineLocator(string discoveryPath, HttpClient http, Func<int, bool>? isEngineProcess = null)
    {
        DiscoveryPath = discoveryPath;
        _http = http;
        _isEngineProcess = isEngineProcess ?? IsEngineProcess;
    }

    public static string DefaultDiscoveryPath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micser",
            "engine.json"
        );

    public string DiscoveryPath { get; }

    /// <summary>
    /// Returns the running engine, or null if there is none.
    /// </summary>
    public async Task<EngineInfo?> FindAsync(CancellationToken cancellationToken = default)
    {
        var info = ReadDiscovery();
        if (info == null || !_isEngineProcess(info.ProcessId))
        {
            return null;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(HealthTimeout);
            using var response = await _http.GetAsync(new Uri(info.Url, "/api/health"), timeout.Token);
            return response.IsSuccessStatusCode ? info : null;
        }
        catch (Exception ex)
            when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    internal EngineInfo? ReadDiscovery()
    {
        try
        {
            using var stream = new FileStream(
                DiscoveryPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete
            );
            var file = JsonSerializer.Deserialize<DiscoveryFile>(stream, JsonSerializerOptions.Web);
            return file is { Url: not null } && Uri.TryCreate(file.Url, UriKind.Absolute, out var url)
                ? new EngineInfo(url, file.Token, file.ProcessId)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static bool IsEngineProcess(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited
                && process.ProcessName.Equals("Micser.Engine", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private sealed record DiscoveryFile(string? Url, string? Token, int ProcessId);
}
