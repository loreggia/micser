using Prism.Modularity;
using Unity;

namespace Micser.Core
{
    public class CoreModule : IModule
    {
        private readonly IUnityContainer _container;

        public CoreModule(IUnityContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
        }
    }
}