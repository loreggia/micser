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

    Task ModuleChanged(ModuleDto module);

    /// <summary>
    /// Live data of a module the client subscribed to, about 20 times per second.
    /// </summary>
    Task ModuleData(Guid moduleId, object data);

    Task ModuleRemoved(Guid moduleId);

    Task StatusChanged(EngineStatusDto status);
}

/// <summary>
/// Pushes engine changes to all clients and live module data to subscribers. Mapped at <c>/hubs/engine</c>.
/// </summary>
public sealed class EngineHub : Hub<IEngineClient>
{
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

    public async Task Unsubscribe(Guid moduleId)
    {
        _subscriptions.Remove(Context.ConnectionId, moduleId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetModuleGroup(moduleId));
    }
}
