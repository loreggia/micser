using Micser.Engine.Configuration;

namespace Micser.Engine.Audio;

/// <summary>
/// Starts the audio host with the application and saves the configuration on shutdown.
/// </summary>
public sealed class AudioHostService : IHostedService
{
    private readonly AudioHost _host;
    private readonly EngineConfigStore _store;

    public AudioHostService(AudioHost host, EngineConfigStore store)
    {
        _host = host;
        _store = store;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _host.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _host.Stop();
        _store.Flush();
        return Task.CompletedTask;
    }
}
