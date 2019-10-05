using Micser.Common;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// Base interface for engine plugins.
    /// </summary>
    public interface IEngineModule
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