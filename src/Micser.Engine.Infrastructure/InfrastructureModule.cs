using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.Services;
using NLog.Extensions.Logging;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// The main infrastructure module.
    /// </summary>
    public class InfrastructureModule : IEngineModule
    {
        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(options => options.AddNLog());

            services.AddDbContext<EngineDbContext>(configuration.GetConnectionString("DefaultConnection"));

            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IModuleConnectionService, ModuleConnectionService>();

            services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ISettingHandlerFactory>(sp => new SettingHandlerFactory(t => (ISettingHandler)sp.GetService(t)));

            //services.AddSingleton<IApiServer, ApiServer>();
            //services.AddSingleton<IApiServerConfiguration>(new ApiConfiguration { PipeName = Globals.EnginePipeName });
            //services.AddSingleton<IApiClient, ApiClient>();
            //services.AddSingleton<IApiClientConfiguration>(new ApiConfiguration { PipeName = Globals.AppPipeName });
            //services.AddSingleton<IRequestProcessorFactory, RequestProcessorFactory>();
            //services.AddSingleton<IMessageSerializer, MessagePackMessageSerializer>();

            services.AddSingleton<IUpdateService, HttpUpdateService>();
        }

        /// <inheritdoc />
        public void OnInitialized(IServiceProvider serviceProvider)
        {
            using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<EngineDbContext>>().CreateDbContext();
            dbContext.Database.Migrate();
        }
    }
}