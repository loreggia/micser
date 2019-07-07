using Micser.Common;
using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.DataAccess.Repositories;
using Micser.Common.Settings;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using Micser.Engine.Infrastructure.Services;
using Micser.Engine.Infrastructure.Updates;
using NLog;
using System.Data.Entity;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// The main infrastructure module.
    /// </summary>
    public class InfrastructureModule : IEngineModule
    {
        /// <inheritdoc />
        public void OnInitialized(IUnityContainer container)
        {
            var settingsRegistry = container.Resolve<ISettingsRegistry>();

            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.UpdateCheck,
                DefaultValue = true
            });
        }

        /// <inheritdoc />
        public void RegisterTypes(IUnityContainer container)
        {
            container.RegisterType<ILogger>(new InjectionFactory(c => LogManager.GetCurrentClassLogger()));

            container.RegisterType<DbContext, EngineDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => (IRepository)container.Resolve(t, new ParameterOverride("context", c))));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, UnitOfWork>();

            container.RegisterType<IModuleRepository, ModuleRepository>();
            container.RegisterType<IModuleConnectionRepository, ModuleConnectionRepository>();
            container.RegisterType<ISettingValueRepository, SettingValueRepository>();

            container.RegisterType<IModuleService, ModuleService>();
            container.RegisterType<IModuleConnectionService, ModuleConnectionService>();

            container.RegisterSingleton<ISettingsRegistry, SettingsRegistry>();
            container.RegisterSingleton<ISettingsService, SettingsService>();
            container.RegisterInstance<ISettingHandlerFactory>(new SettingHandlerFactory(t => (ISettingHandler)container.Resolve(t)));

            container.RegisterSingleton<IApiServer, ApiServer>();
            container.RegisterInstance<IApiConfiguration>(new ApiConfiguration { Port = Globals.ApiPort });

            container.RegisterSingleton<IRequestProcessorFactory, RequestProcessorFactory>();

            container.RegisterInstance(new HttpUpdateSettings
            {
                ManifestUrl = "https://micser.lloreggia.ch/update/manifest.json"
            });
            container.RegisterSingleton<IUpdateService, HttpUpdateService>();

            var server = container.Resolve<IApiServer>();
            container.RegisterInstance<IApiEndPoint>(server);
        }
    }
}