namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Provides global application state information.
    /// </summary>
    public interface IApplicationStateService
    {
        /// <summary>
        /// Gets a value that indicates whether the program finished loading all available modules.
        /// </summary>
        bool ModulesLoaded { get; }

        /// <summary>
        /// Initialized the service and registers it's event handlers.
        /// </summary>
        void Initialize();
    }
}