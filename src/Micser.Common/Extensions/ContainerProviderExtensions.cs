using Micser.Common.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity;

namespace Micser.Common.Extensions
{
    public static class ContainerProviderExtensions
    {
        public static void RegisterFactory<T>(this IContainerProvider container, Func<IContainerProvider, object> factory, string name = null)
        {
            container.RegisterFactory(typeof(T), factory, name);
        }

        public static void RegisterInstance<T>(this IContainerProvider container, T instance, string name = null)
        {
            container.RegisterInstance(typeof(T), instance, name);
        }

        /// <summary>
        /// Registers a request processor. If the <paramref name="name"/> parameter is not specified the processor has to be annotated with a <see cref="RequestProcessorNameAttribute"/> attribute.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="name">The name of the resource to register this processor for.
        /// If this is omitted and no <see cref="RequestProcessorNameAttribute"/> is present
        /// the processor will be used as a default processor for requests without a specific resource.</param>
        /// <typeparam name="TProcessor"></typeparam>
        public static void RegisterRequestProcessor<TProcessor>(this IContainerProvider container, string name = null)
            where TProcessor : IRequestProcessor
        {
            if (name == null)
            {
                name = RequestProcessorNameAttribute.DefaultName;
                var attributes = typeof(TProcessor).GetCustomAttributes(typeof(RequestProcessorNameAttribute), true);
                if (attributes.Length == 1 && attributes[0] is RequestProcessorNameAttribute nameAttribute)
                {
                    name = nameAttribute.Name;
                }
            }

            container.RegisterType<IRequestProcessor, TProcessor>(name);
        }

        public static void RegisterSingleton<TFrom, TTo>(this IContainerProvider container, string name = null)
        {
            container.RegisterSingleton(typeof(TFrom), typeof(TTo), name);
        }

        public static void RegisterSingleton<T>(this IContainerProvider container, string name = null)
        {
            container.RegisterSingleton(typeof(T), typeof(T), name);
        }

        /// <summary>
        /// Registers multiple types for a specific interface as singletons. The types are registered using their <see cref="Type.AssemblyQualifiedName"/> as registration name.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="from">The interface type to register the types for.</param>
        /// <param name="types">The types to register.</param>
        public static void RegisterSingletons(this IContainerProvider container, Type from, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                container.RegisterSingleton(from, type, type.AssemblyQualifiedName);
            }
        }

        /// <summary>
        /// Registers multiple types for a specific interface as singletons. The types are registered using their <see cref="Type.AssemblyQualifiedName"/> as registration name.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="types">The types to register.</param>
        /// <typeparam name="TFrom">The interface type to register the types for.</typeparam>
        public static void RegisterSingletons<TFrom>(this IContainerProvider container, IEnumerable<Type> types)
        {
            container.RegisterSingletons(typeof(TFrom), types);
        }

        public static void RegisterType<TFrom, TTo>(this IContainerProvider container, string name = null)
            where TTo : TFrom
        {
            container.RegisterType(typeof(TFrom), typeof(TTo), name);
        }

        public static void RegisterType<T>(this IContainerProvider container, string name = null)
        {
            container.RegisterType(typeof(T), name);
        }

        /// <summary>
        /// Registers multiple types for a specific interface. The types are registered using their <see cref="Type.AssemblyQualifiedName"/> as registration name.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="from">The interface type to register the types for.</param>
        /// <param name="types">The types to register.</param>
        public static void RegisterTypes(this IContainerProvider container, Type from, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                container.RegisterType(from, type, type.AssemblyQualifiedName);
            }
        }

        /// <summary>
        /// Registers multiple types for a specific interface. The types are registered using their <see cref="Type.AssemblyQualifiedName"/> as registration name.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="types">The types to register.</param>
        /// <typeparam name="TFrom">The interface type to register the types for.</typeparam>
        public static void RegisterTypes<TFrom>(this IContainerProvider container, IEnumerable<Type> types)
        {
            container.RegisterTypes(typeof(TFrom), types);
        }

        public static T Resolve<T>(this IContainerProvider containerProvider, string name = null)
        {
            return (T)containerProvider.Resolve(typeof(T), name);
        }

        public static T Resolve<T>(this IContainerProvider containerProvider, string name, params DependencyOverride[] dependencyOverrides)
        {
            return (T)containerProvider.Resolve(typeof(T), name, dependencyOverrides);
        }

        public static IEnumerable<T> ResolveAll<T>(this IContainerProvider container)
        {
            return container.ResolveAll(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Tries to resolve the type <typeparamref name="T"/>. Returns null if the type is not registered.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="name">The optional name to resolve the type with.</param>
        /// <typeparam name="T">The registration type.</typeparam>
        /// <returns>An instance of type <typeparamref name="T"/> or null if no registration was found.</returns>
        public static T TryResolve<T>(this IContainerProvider container, string name = null)
        {
            try
            {
                return container.Resolve<T>(name);
            }
            catch (ResolutionFailedException)
            {
                return default;
            }
        }
    }
}