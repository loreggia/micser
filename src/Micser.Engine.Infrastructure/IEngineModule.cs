using Unity;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// Base interface for engine plugins.
    /// </summary>
    public interface IEngineModule
    {
        void RegisterTypes(IUnityContainer container);
    }
}