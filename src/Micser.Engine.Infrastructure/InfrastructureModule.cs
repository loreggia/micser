using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.Services;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// The main infrastructure module.
    /// </summary>
    public class InfrastructureModule : IModule
    {
        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IModuleService, ModuleService>();
            services.AddTransient<IModuleConnectionService, ModuleConnectionService>();

            services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
            services.AddSingleton<ISettingsService, SettingsService<EngineDbContext>>();
            services.AddSingleton<ISettingHandlerFactory>(sp => new SettingHandlerFactory(t => (ISettingHandler)sp.GetService(t)));

            services.AddSingleton<IUpdateService, HttpUpdateService>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}