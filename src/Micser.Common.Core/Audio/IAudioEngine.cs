using System;

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
        void AddConnection(long id);

        /// <summary>
        /// Instantiates and adds a saved module to the currently loaded modules.
        /// </summary>
        /// <param name="id">The module ID.</param>
        void AddModule(long id);

        /// <summary>
        /// Removes a connection from the currently loaded modules.
        /// </summary>
        /// <param name="id">The connection ID.</param>
        void RemoveConnection(long id);

        /// <summary>
        /// Removes a module from the currently loaded modules.
        /// </summary>
        /// <param name="id">The module ID.</param>
        void RemoveModule(long id);

        /// <summary>
        /// Starts the audio engine and instantiates all currently saved modules and connections.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the engine and all loaded modules.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates the state of a currently loaded module.
        /// </summary>
        /// <param name="id">The module ID.</param>
        void UpdateModule(long id);
    }
}