using System;
using System.Collections.Generic;
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

        public static void UseModules(this IServiceProvider services)
        {
            var modules = services.GetRequiredService<IEnumerable<IModule>>();

            foreach (var module in modules)
            {
                module.Initialize(services);
            }
        }
    }
}