using Unity;

namespace Micser.Engine.Infrastructure
{
    public interface IEngineModule
    {
        void RegisterTypes(IUnityContainer container);
    }
}