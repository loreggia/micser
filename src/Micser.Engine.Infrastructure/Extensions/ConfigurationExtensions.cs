using System;
using Microsoft.Extensions.DependencyInjection;
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
    }
}