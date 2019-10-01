using Micser.Common;
using Micser.Common.Extensions;
using Micser.Engine.Infrastructure.Audio;
using System;

namespace Micser.Engine.Infrastructure.Extensions
{
    /// <summary>
    /// Contains helper extension methods for the <see cref="IContainerProvider"/> class.
    /// </summary>
    public static class ContainerProviderExtensions
    {
        /// <summary>
        /// Registers all specified types as available audio modules.
        /// </summary>
        public static void RegisterAudioModules(this IContainerProvider container, params Type[] moduleTypes)
        {
            foreach (var moduleType in moduleTypes)
            {
                container.RegisterType(moduleType);
            }

            container.RegisterTypes<IAudioModule>(moduleTypes);
        }
    }
}