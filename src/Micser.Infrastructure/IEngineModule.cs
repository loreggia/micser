using Unity;

namespace Micser.Infrastructure
{
    public interface IEngineModule
    {
        void RegisterTypes(IUnityContainer container);
    }
}