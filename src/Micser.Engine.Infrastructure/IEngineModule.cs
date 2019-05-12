using Unity;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// Base interface for engine plugins.
    /// </summary>
    public interface IEngineModule
    {
        /// <summary>
        /// Lets the module register its types in the DI container.
        /// </summary>
        void RegisterTypes(IUnityContainer container);
    }
}