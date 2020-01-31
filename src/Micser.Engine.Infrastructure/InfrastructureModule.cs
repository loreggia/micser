using Microsoft.EntityFrameworkCore;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.Services;
using NLog;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// The main infrastructure module.
    /// </summary>
    public class InfrastructureModule : IEngineModule
    {
        /// <inheritdoc />
        public void OnInitialized(IContainerProvider container)
        {
            using var dbContext = container.Resolve<IDbContextFactory>().Create();
            dbContext.Database.Migrate();
        }

        /// <inheritdoc />
        public void RegisterTypes(IContainerProvider container)
        {
            container.RegisterFactory<ILogger>(_ => LogManager.GetCurrentClassLogger());

            container.RegisterInstance<IDbContextFactory>(new DbContextFactory(() => new EngineDbContext(container.GetDbContextOptions<EngineDbContext>())));

            container.RegisterType<IModuleService, ModuleService>();
            container.RegisterType<IModuleConnectionService, ModuleConnectionService>();

            container.RegisterSingleton<ISettingsRegistry, SettingsRegistry>();
            container.RegisterSingleton<ISettingsService, SettingsService>();
            container.RegisterInstance<ISettingHandlerFactory>(new SettingHandlerFactory(t => (ISettingHandler)container.Resolve(t)));

            container.RegisterSingleton<IApiServer, ApiServer>();
            container.RegisterInstance<IApiServerConfiguration>(new ApiConfiguration { PipeName = Globals.EnginePipeName });
            container.RegisterSingleton<IApiClient, ApiClient>();
            container.RegisterInstance<IApiClientConfiguration>(new ApiConfiguration { PipeName = Globals.AppPipeName });
            container.RegisterSingleton<IRequestProcessorFactory, RequestProcessorFactory>();
            container.RegisterSingleton<IMessageSerializer, MessagePackMessageSerializer>();

            container.RegisterSingleton<IUpdateService, HttpUpdateService>();
        }
    }
}