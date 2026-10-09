using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Micser.Engine.Audio;
using Micser.Engine.Contracts;

namespace Micser.Engine.Hubs;

/// <summary>
/// Sends engine notifications to all hub clients in the order they were raised, without waiting for delivery.
/// </summary>
public sealed class HubEngineNotifier : IEngineNotifier, IDisposable
{
    private readonly IHubContext<EngineHub, IEngineClient> _hub;
    private readonly ILogger _logger;
    private readonly Channel<Func<IEngineClient, Task>> _queue = Channel.CreateUnbounded<Func<IEngineClient, Task>>(
        new UnboundedChannelOptions { SingleReader = true }
    );
    private readonly Task _sender;

    public HubEngineNotifier(IHubContext<EngineHub, IEngineClient> hub, ILogger<HubEngineNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
        _sender = Task.Run(SendAllAsync);
    }

    public void ConnectionAdded(ConnectionDto connection)
    {
        Send(c => c.ConnectionAdded(connection));
    }

    public void ConnectionRemoved(Guid connectionId)
    {
        Send(c => c.ConnectionRemoved(connectionId));
    }

    public void DevicesChanged()
    {
        Send(c => c.DevicesChanged());
    }

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _sender.Wait(TimeSpan.FromSeconds(1));
    }

    public void ModuleChanged(ModuleDto module)
    {
        Send(c => c.ModuleChanged(module));
    }

    public void ModuleRemoved(Guid moduleId)
    {
        Send(c => c.ModuleRemoved(moduleId));
    }

    public void PluginsChanged(IReadOnlyList<PluginDto> plugins)
    {
        Send(c => c.PluginsChanged(plugins));
    }

    public void PreferencesChanged(UiPreferencesDto preferences)
    {
        Send(c => c.PreferencesChanged(preferences));
    }

    public void StatusChanged(EngineStatusDto status)
    {
        Send(c => c.StatusChanged(status));
    }

    public void SubgraphChanged(SubgraphDto subgraph)
    {
        Send(c => c.SubgraphChanged(subgraph));
    }

    public void SubgraphRemoved(Guid subgraphId)
    {
        Send(c => c.SubgraphRemoved(subgraphId));
    }

    public void TemplatesChanged(IReadOnlyList<SubgraphTemplateDto> templates)
    {
        Send(c => c.TemplatesChanged(templates));
    }

    private void Send(Func<IEngineClient, Task> send)
    {
        _queue.Writer.TryWrite(send);
    }

    private async Task SendAllAsync()
    {
        await foreach (var send in _queue.Reader.ReadAllAsync())
        {
            try
            {
                await send(_hub.Clients.All);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sending a notification failed.");
            }
        }
    }
}
