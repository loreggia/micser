using Micser.Common.Api;
using System;
using System.Collections.Generic;
using Unity;

namespace Micser.Common.Extensions
{
    public static class UnityContainerExtensions
    {
        public static void RegisterRequestProcessor<TProcessor>(this IUnityContainer container)
            where TProcessor : IRequestProcessor
        {
            var name = RequestProcessorNameAttribute.DefaultName;
            var attributes = typeof(TProcessor).GetCustomAttributes(typeof(RequestProcessorNameAttribute), true);
            if (attributes.Length == 1 && attributes[0] is RequestProcessorNameAttribute nameAttribute)
            {
                name = nameAttribute.Name;
            }

            container.RegisterType<IRequestProcessor, TProcessor>(name);
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