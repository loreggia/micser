using System.Diagnostics;
using System.Net.Http.Headers;

namespace Micser.Shell;

internal static class EngineControl
{
    /// <summary>
    /// Asks the engine to shut down and waits until its process exits.
    /// </summary>
    /// <exception cref="HttpRequestException">The engine didn't accept the request.</exception>
    /// <exception cref="ArgumentException">The engine process isn't running.</exception>
    /// <exception cref="OperationCanceledException">The engine didn't exit in time.</exception>
    public static async Task ShutdownAsync(HttpClient http, EngineInfo engine, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(engine.Url, "/api/engine/shutdown"));
        if (engine.Token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", engine.Token);
        }

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var process = Process.GetProcessById(engine.ProcessId);
        await process.WaitForExitAsync(cancellationToken);
    }
}
