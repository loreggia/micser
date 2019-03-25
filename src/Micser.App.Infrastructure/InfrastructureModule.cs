using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.DataAccess.Repositories;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Settings;
using Micser.App.Infrastructure.Themes;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Infrastructure.Widgets;
using Micser.Common.Api;
using Prism.Ioc;

namespace Micser.App.Infrastructure
{
    public class InfrastructureModule : IAppModule
    {
        private static bool _isRegistered;

        public static void RegisterInfrastructureTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();

            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            containerRegistry.RegisterSingleton<IToolBarRegistry, ToolBarRegistry>();
            containerRegistry.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();
            containerRegistry.RegisterSingleton<ISettingsRegistry, SettingsRegistry>();
            containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
            containerRegistry.RegisterSingleton<IRequestProcessorFactory, RequestProcessorFactory>();
            containerRegistry.RegisterSingleton<IApiClient, ApiClient>();

            containerRegistry.Register<IRequestProcessor, ApiEventRequestProcessor>();

            containerRegistry.Register<ISettingValueRepository, SettingValueRepository>();

            _isRegistered = true;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var apiClient = containerProvider.Resolve<IApiClient>();
            apiClient.ConnectAsync();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            if (_isRegistered)
            {
                return;
            }

            RegisterInfrastructureTypes(containerRegistry);
        }
    }
}