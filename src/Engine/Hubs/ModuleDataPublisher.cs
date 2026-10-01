using Micser.Engine.Audio;
using Microsoft.AspNetCore.SignalR;

namespace Micser.Engine.Hubs;

/// <summary>
/// Pushes the live data of subscribed modules to their subscribers, and the levels of all modules to the level subscribers.
/// </summary>
public sealed class ModuleDataPublisher : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);
    private readonly AudioHost _host;
    private readonly IHubContext<EngineHub, IEngineClient> _hub;
    private readonly ILogger<ModuleDataPublisher> _logger;
    private readonly ModuleDataSubscriptions _subscriptions;

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
}
