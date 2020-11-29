using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Engine.Infrastructure.Audio;

namespace Micser.Engine.Infrastructure.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddAudioModules(this IServiceCollection services, params Type[] types)
        {
            foreach (var type in types)
            {
                services.AddTransient(typeof(IAudioModule), type);
            }

            return services;
        }

        public static IApplicationBuilder UseModules(this IApplicationBuilder app)
        {
            var modules = app.ApplicationServices.GetRequiredService<IEnumerable<IModule>>();

            foreach (var module in modules)
            {
                if (module is IEngineModule engineModule)
                {
                    engineModule.Configure(app);
                }

                module.Initialize(app.ApplicationServices);
            }

            return app;
        }
    }
}