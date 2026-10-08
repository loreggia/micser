using Micser.Engine.Audio;
using Micser.Engine.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Micser.Engine.Hubs;

/// <summary>
/// Pushes the live data of subscribed modules to their subscribers, the levels of all modules to the level subscribers, and changed port
/// layouts to all clients.
/// </summary>
public sealed class ModuleDataPublisher : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);
    private readonly AudioHost _host;
    private readonly IHubContext<EngineHub, IEngineClient> _hub;
    private readonly ILogger<ModuleDataPublisher> _logger;
    private readonly ModuleDataSubscriptions _subscriptions;
    private Dictionary<Guid, ModulePortLayoutsDto> _portLayouts = [];

    /// <summary>
    /// Pushes the live data of subscribed modules to their subscribers.
    /// </summary>
    public ModuleDataPublisher(
        AudioHost host,
        ModuleDataSubscriptions subscriptions,
        IHubContext<EngineHub, IEngineClient> hub,
        ILogger<ModuleDataPublisher> logger)
    {
        _host = host;
        _subscriptions = subscriptions;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PublishLevelsAsync();
            await PublishPortLayoutsAsync();
            foreach (var moduleId in _subscriptions.GetSubscribedModules())
            {
                try
                {
                    if (_host.GetModuleData(moduleId) is { } data)
                    {
                        await _hub.Clients.Group(EngineHub.GetModuleGroup(moduleId)).ModuleData(moduleId, data);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Publishing data of module {Module} failed.", moduleId);
                }
            }
        }
    }

    private static bool HaveSameLayouts(IReadOnlyDictionary<string, PortLayoutDto> ports, IReadOnlyDictionary<string, PortLayoutDto> others)
    {
        return ports.Count == others.Count && ports.All(port => others.TryGetValue(port.Key, out var other)
            && other.ChannelCount == port.Value.ChannelCount
            && (other.Speakers ?? []).SequenceEqual(port.Value.Speakers ?? []));
    }

    private async Task PublishLevelsAsync()
    {
        try
        {
            // read even without subscribers, so a new subscriber doesn't get peaks from long ago
            var levels = _host.ReadLevels();
            if (_subscriptions.HasLevelSubscribers)
            {
                await _hub.Clients.Group(EngineHub.LevelsGroup).Levels(levels);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Publishing the levels failed.");
        }
    }

    private async Task PublishPortLayoutsAsync()
    {
        try
        {
            var layouts = _host.GetPortLayouts();
            ModulePortLayoutsDto[] changed = [.. layouts.Where(layout => !_portLayouts.TryGetValue(layout.ModuleId, out var previous)
                || !HaveSameLayouts(previous.Inputs, layout.Inputs)
                || !HaveSameLayouts(previous.Outputs, layout.Outputs))];
            _portLayouts = layouts.ToDictionary(layout => layout.ModuleId);
            if (changed.Length > 0)
            {
                await _hub.Clients.All.PortLayoutsChanged(changed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Publishing the port layouts failed.");
        }
    }
}
