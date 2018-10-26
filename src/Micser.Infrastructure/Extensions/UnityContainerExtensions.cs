using Micser.Infrastructure.Modules;
using System;
using System.Collections.Generic;
using Unity;

namespace Micser.Infrastructure.Extensions
{
    public static class UnityContainerExtensions
    {
        public static void RegisterAudioModules(this IUnityContainer container, params Type[] moduleTypes)
        {
            foreach (var moduleType in moduleTypes)
            {
                container.RegisterType(moduleType);
            }

            container.RegisterTypes<IModule>(moduleTypes);
        }

        public static void RegisterSingletons(this IUnityContainer container, Type from, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                container.RegisterSingleton(from, type, type.AssemblyQualifiedName);
            }
        }

        public static void RegisterSingletons<TFrom>(this IUnityContainer container, IEnumerable<Type> types)
        {
            container.RegisterSingletons(typeof(TFrom), types);
        }

        public static void RegisterTypes(this IUnityContainer container, Type from, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                container.RegisterType(from, type, type.AssemblyQualifiedName);
            }
        }

        public static void RegisterTypes<TFrom>(this IUnityContainer container, IEnumerable<Type> types)
        {
            container.RegisterTypes(typeof(TFrom), types);
        }
    }
}