using Micser.App.Infrastructure.Widgets;
using Prism.Ioc;
using Prism.Modularity;

namespace Micser.App.Infrastructure
{
    public class InfrastructureModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();
        }
    }
}