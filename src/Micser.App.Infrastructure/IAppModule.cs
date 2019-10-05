using Micser.Common;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Base interface for all UI modules.
    /// </summary>
    public interface IAppModule
    {
        /// <summary>
        /// Initializes the module (after all modules have registered their types in the DI container).
        /// </summary>
        /// <param name="container">The fully registered container.</param>
        void OnInitialized(IContainerProvider container);

        /// <summary>
        /// Lets the module register its types in the DI container.
        /// </summary>
        void RegisterTypes(IContainerProvider container);
    }
}