using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.DataAccess.Repositories;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Settings;
using Micser.App.Infrastructure.Themes;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Infrastructure.Widgets;
using Micser.Common;
using Micser.Common.Api;
using Prism.Ioc;
using Prism.Unity;
using Unity;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// UI module performing default initialization tasks.
    /// </summary>
    public class InfrastructureModule : IAppModule
    {
        private static bool _isRegistered;

        /// <summary>
        /// Registers all types required by the infrastructure module.
        /// </summary>
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
            containerRegistry.RegisterSingleton<IApiEndPoint, ApiClient>();

            containerRegistry.RegisterInstance<IApiConfiguration>(new ApiConfiguration { Port = Globals.ApiPort });

            containerRegistry.Register<IRequestProcessor, ApiEventRequestProcessor>();

            containerRegistry.Register<ISettingValueRepository, SettingValueRepository>();

            var container = containerRegistry.GetContainer();
            container.RegisterInstance<ISettingHandlerFactory>(new SettingHandlerFactory(t => (ISettingHandler)container.Resolve(t)));

            _isRegistered = true;
        }

        /// <inheritdoc />
        public async void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc />
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