using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace Micser.Engine.Security;

/// <param name="Url">The base URL of the engine, e.g. <c>http://127.0.0.1:53412</c>.</param>
/// <param name="Token">The access token; null if the engine doesn't require one.</param>
public sealed record DiscoveryInfo(string Url, string? Token, int ProcessId);

/// <summary>
/// Writes <see cref="DiscoveryInfo"/> to <see cref="EngineOptions.DiscoveryPath"/> once the server listens and deletes
/// it on shutdown. Other users can't read it: it's in the user's local app data or runtime folder, and on Unix it's created with mode 0600.
/// </summary>
public sealed class EngineDiscovery : IHostedService
{
    private readonly EngineAccess _access;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger _logger;
    private readonly string _path;
    private readonly IServer _server;
    private bool _written;

    public EngineDiscovery(
        IServer server,
        IHostApplicationLifetime lifetime,
        EngineAccess access,
        IOptions<EngineOptions> options,
        ILogger<EngineDiscovery> logger
    )
    {
        _server = server;
        _lifetime = lifetime;
        _access = access;
        _path = options.Value.DiscoveryPath;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(Write);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_written)
        {
            try
            {
                File.Delete(_path);
            }
            // e.g. a virus scanner still has the file open
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete the discovery file {Path}.", _path);
            }
        }

        return Task.CompletedTask;
    }

    private void Write()
    {
        var url = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
        if (url == null)
        {
            return;
        }

        var info = new DiscoveryInfo(url, _access.RequireToken ? _access.Token : null, Environment.ProcessId);
        PrivateFiles.CreateDirectory(Path.GetDirectoryName(_path)!);
        PrivateFiles.WriteAllText(_path, JsonSerializer.Serialize(info, JsonSerializerOptions.Web));
        _written = true;
        _logger.LogInformation("Engine listening on {Url}.", url);
    }
}
