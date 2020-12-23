using System;
using System.Threading.Tasks;

namespace Micser.Common.Audio
{
    /// <summary>
    /// Describes the main audio engine component.
    /// </summary>
    public interface IAudioEngine : IDisposable
    {
        /// <summary>
        /// Gets a value that indicates whether the audio engine is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Instantiates and adds a saved connection to the currently loaded modules.
        /// </summary>
        /// <param name="id">The connection ID.</param>
        Task AddConnectionAsync(long id);

        /// <summary>
        /// Instantiates and adds a saved module to the currently loaded modules.
        /// </summary>
        /// <param name="id">The module ID.</param>
        Task AddModuleAsync(long id);

        /// <summary>
        /// Removes a connection from the currently loaded modules.
        /// </summary>
        /// <param name="id">The connection ID.</param>
        Task RemoveConnectionAsync(long id);

        /// <summary>
        /// Removes a module from the currently loaded modules.
        /// </summary>
        /// <param name="id">The module ID.</param>
        Task RemoveModuleAsync(long id);

        /// <summary>
        /// Starts the audio engine and instantiates all currently saved modules and connections.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the engine and all loaded modules.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Updates the state of a currently loaded module.
        /// </summary>
        /// <param name="id">The module ID.</param>
        Task UpdateModuleAsync(long id);
    }
}