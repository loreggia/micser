using Micser.Engine.Contracts;

namespace Micser.Engine.Audio;

/// <summary>
/// Tells connected clients about changes. Calls return immediately; delivery happens in the background.
/// </summary>
public interface IEngineNotifier
{
    void ConnectionAdded(ConnectionDto connection);

    void ConnectionRemoved(Guid connectionId);

    void DevicesChanged();

    void ModuleChanged(ModuleDto module);

    void ModuleRemoved(Guid moduleId);

    void PluginsChanged(IReadOnlyList<PluginDto> plugins);

    void PreferencesChanged(UiPreferencesDto preferences);

    void StatusChanged(EngineStatusDto status);
}
