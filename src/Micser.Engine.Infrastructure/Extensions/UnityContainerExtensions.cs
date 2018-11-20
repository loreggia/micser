using Micser.Infrastructure.Extensions;
using System;
using Unity;

namespace Micser.Engine.Infrastructure.Extensions
{
    public static class UnityContainerExtensions
    {
        public static void RegisterAudioModules(this IUnityContainer container, params Type[] moduleTypes)
        {
            foreach (var moduleType in moduleTypes)
            {
                container.RegisterType(moduleType);
            }

            container.RegisterTypes<IAudioModule>(moduleTypes);
        }
    }
}