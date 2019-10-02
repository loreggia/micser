using Microsoft.EntityFrameworkCore;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.DataAccess.Repositories;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
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
            using (var dbContext = container.Resolve<DbContext>())
            {
                dbContext.Database.Migrate();
            }
        }

        /// <inheritdoc />
        public void RegisterTypes(IContainerProvider container)
        {
            container.RegisterFactory<ILogger>(c => LogManager.GetCurrentClassLogger());

            container.RegisterType<DbContext, EngineDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory(t => (IRepository)container.Resolve(t)));
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

            container.RegisterSingleton<IUpdateService, HttpUpdateService>();

            var server = container.Resolve<IApiServer>();
            container.RegisterInstance<IApiEndPoint>(server);
        }
    }
}