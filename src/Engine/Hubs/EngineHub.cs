using Micser.Engine.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Micser.Engine.Hubs;

/// <summary>
/// The messages the engine pushes to clients.
/// </summary>
public interface IEngineClient
{
    Task ConnectionAdded(ConnectionDto connection);

    Task ConnectionRemoved(Guid connectionId);

    Task DevicesChanged();

    /// <summary>
    /// The levels of all processed modules, about 20 times per second, to clients that subscribed with <see cref="EngineHub.SubscribeLevels"/>.
    /// Modules without levels (e.g. not processed) are left out.
    /// </summary>
    Task Levels(IReadOnlyDictionary<Guid, PortLevelsDto[]> levels);

    Task ModuleChanged(ModuleDto module);

    /// <summary>
    /// Live data of a module the client subscribed to, about 20 times per second.
    /// </summary>
    Task ModuleData(Guid moduleId, object data);

    Task ModuleRemoved(Guid moduleId);

    Task PreferencesChanged(UiPreferencesDto preferences);

    Task StatusChanged(EngineStatusDto status);
}

/// <summary>
/// Pushes engine changes to all clients and live module data to subscribers. Mapped at <c>/hubs/engine</c>.
/// </summary>
public sealed class EngineHub : Hub<IEngineClient>
{
    public const string LevelsGroup = "levels";

    private readonly ModuleDataSubscriptions _subscriptions;

    public EngineHub(ModuleDataSubscriptions subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public static string GetModuleGroup(Guid moduleId)
    {
        return $"module:{moduleId}";
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _subscriptions.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Starts receiving <see cref="IEngineClient.ModuleData"/> for a module.
    /// </summary>
    public async Task Subscribe(Guid moduleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetModuleGroup(moduleId));
        _subscriptions.Add(Context.ConnectionId, moduleId);
    }

    /// <summary>
    /// Starts receiving <see cref="IEngineClient.Levels"/>.
    /// </summary>
    public async Task SubscribeLevels()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, LevelsGroup);
        _subscriptions.AddLevels(Context.ConnectionId);
    }

    public async Task Unsubscribe(Guid moduleId)
    {
        _subscriptions.Remove(Context.ConnectionId, moduleId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetModuleGroup(moduleId));
    }

    public async Task UnsubscribeLevels()
    {
        _subscriptions.RemoveLevels(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, LevelsGroup);
    }
}
