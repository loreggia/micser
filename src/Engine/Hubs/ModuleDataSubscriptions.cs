namespace Micser.Engine.Hubs;

/// <summary>
/// Which hub connections are subscribed to which modules' live data, and to the levels.
/// </summary>
public sealed class ModuleDataSubscriptions
{
    private readonly HashSet<string> _levelSubscribers = [];
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, HashSet<string>> _subscribers = [];

    public bool HasLevelSubscribers
    {
        get
        {
            lock (_lock)
            {
                return _levelSubscribers.Count > 0;
            }
        }
    }

    public void Add(string connectionId, Guid moduleId)
    {
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(moduleId, out var connections))
            {
                _subscribers[moduleId] = connections = [];
            }

            connections.Add(connectionId);
        }
    }

    public void AddLevels(string connectionId)
    {
        lock (_lock)
        {
            _levelSubscribers.Add(connectionId);
        }
    }

    /// <summary>
    /// The modules with at least one subscriber.
    /// </summary>
    public Guid[] GetSubscribedModules()
    {
        lock (_lock)
        {
            return [.. _subscribers.Keys];
        }
    }

    public void Remove(string connectionId, Guid moduleId)
    {
        lock (_lock)
        {
            if (
                _subscribers.TryGetValue(moduleId, out var connections)
                && connections.Remove(connectionId)
                && connections.Count == 0
            )
            {
                _subscribers.Remove(moduleId);
            }
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (_lock)
        {
            _levelSubscribers.Remove(connectionId);
            foreach (var (moduleId, connections) in _subscribers.ToArray())
            {
                if (connections.Remove(connectionId) && connections.Count == 0)
                {
                    _subscribers.Remove(moduleId);
                }
            }
        }
    }

    public void RemoveLevels(string connectionId)
    {
        lock (_lock)
        {
            _levelSubscribers.Remove(connectionId);
        }
    }
}
