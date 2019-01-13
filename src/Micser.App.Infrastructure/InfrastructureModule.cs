using Micser.App.Infrastructure.Widgets;
using Prism.Ioc;

namespace Micser.App.Infrastructure
{
    public class InfrastructureModule : IAppModule
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