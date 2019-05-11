using Micser.Common.Api;
using System;
using System.Collections.Generic;
using Unity;
using Unity.Exceptions;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Contains helper extension methods for <see cref="IUnityContainer"/> objects.
    /// </summary>
    public static class UnityContainerExtensions
    {
        /// <summary>
        /// Registers a request processor. If the <paramref name="name"/> parameter is not specified the processor has to be annotated with a <see cref="RequestProcessorNameAttribute"/> attribute.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="name">The name of the resource to register this processor for.
        /// If this is omitted and no <see cref="RequestProcessorNameAttribute"/> is present
        /// the processor will be used as a default processor for requests without a specific resource.</param>
        /// <typeparam name="TProcessor"></typeparam>
        public static void RegisterRequestProcessor<TProcessor>(this IUnityContainer container, string name = null)
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

        /// <summary>
        /// Registers multiple types for a specific interface as singletons. The types are registered using their <see cref="Type.AssemblyQualifiedName"/> as registration name.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="from">The interface type to register the types for.</param>
        /// <param name="types">The types to register.</param>
        public static void RegisterSingletons(this IUnityContainer container, Type from, IEnumerable<Type> types)
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
        public static void RegisterSingletons<TFrom>(this IUnityContainer container, IEnumerable<Type> types)
        {
            container.RegisterSingletons(typeof(TFrom), types);
        }

        /// <summary>
        /// Registers multiple types for a specific interface. The types are registered using their <see cref="Type.AssemblyQualifiedName"/> as registration name.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="from">The interface type to register the types for.</param>
        /// <param name="types">The types to register.</param>
        public static void RegisterTypes(this IUnityContainer container, Type from, IEnumerable<Type> types)
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
        public static void RegisterTypes<TFrom>(this IUnityContainer container, IEnumerable<Type> types)
        {
            container.RegisterTypes(typeof(TFrom), types);
        }

        /// <summary>
        /// Tries to resolve the type <typeparamref name="T"/>. Returns null if the type is not registered.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="name">The optional name to resolve the type with.</param>
        /// <typeparam name="T">The registration type.</typeparam>
        /// <returns>An instance of type <typeparamref name="T"/> or null if no registration was found.</returns>
        public static T TryResolve<T>(this IUnityContainer container, string name = null)
        {
            try
            {
                return container.Resolve<T>(name);
            }
            catch (ResolutionFailedException)
            {
                return default(T);
            }
        }
    }
}