using Micser.Common.Extensions;
using Micser.Engine.Infrastructure.Audio;
using System;
using Unity;

namespace Micser.Engine.Infrastructure.Extensions
{
    public static class UnityContainerExtensions
    {
        /// <summary>
        /// Registers all specified types as available audio modules.
        /// </summary>
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