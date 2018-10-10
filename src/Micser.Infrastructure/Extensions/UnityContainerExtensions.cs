using System;
using System.Collections.Generic;
using Unity;
using Unity.Lifetime;

namespace Micser.Infrastructure.Extensions
{
    public static class UnityContainerExtensions
    {
        public static void RegisterTypes(this IUnityContainer container, Type from, IEnumerable<Type> types, LifetimeManager lifetimeManager)
        {
            foreach (var type in types)
            {
                container.RegisterType(from, type, type.AssemblyQualifiedName, lifetimeManager);
            }
        }

        public static void RegisterTypes<TFrom>(this IUnityContainer container, IEnumerable<Type> types, LifetimeManager lifetimeManager)
        {
            container.RegisterTypes(typeof(TFrom), types, lifetimeManager);
        }
    }
}