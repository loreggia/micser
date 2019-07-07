using Unity;

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
        void OnInitialized(IUnityContainer container);

        /// <summary>
        /// Lets the module register its types in the DI container.
        /// </summary>
        void RegisterTypes(IUnityContainer container);
    }
}