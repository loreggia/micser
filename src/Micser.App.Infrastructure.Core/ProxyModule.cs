using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Unity;

namespace Micser.App.Infrastructure
{
    public class ProxyModule<TAppModule> : IModule
        where TAppModule : IAppModule, new()
    {
        public ProxyModule()
        {
            AppModule = new TAppModule();
        }

        public TAppModule AppModule { get; }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            AppModule.OnInitialized(containerProvider.Resolve<Common.IContainerProvider>());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var container = containerRegistry.GetContainer();
            AppModule.RegisterTypes(container.Resolve<Common.IContainerProvider>());
        }
    }
}