namespace Micser.Audio;

/// <summary>
/// A module that produces live data for the UI, e.g. a spectrum or stream statistics. The engine polls it only while
/// a client is subscribed to the module.
/// </summary>
public interface IModuleDataSource
{
    /// <summary>
    /// Returns the current data (JSON-serializable), or null if there is none. Called from a non-audio thread.
    /// </summary>
    object? GetData();
}
