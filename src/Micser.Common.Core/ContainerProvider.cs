using System;
using System.Collections.Generic;
using Unity;
using Unity.Lifetime;

namespace Micser.Common
{
    public class UnityContainerProvider : IContainerProvider
    {
        private readonly IUnityContainer _container;

        public UnityContainerProvider(IUnityContainer container = null)
        {
            _container = container ?? new UnityContainer();
            _container.RegisterInstance<IContainerProvider>(this);
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public void RegisterFactory<TFrom>(Func<IContainerProvider, TFrom> factory)
        {
            _container.RegisterFactory<TFrom>(c => factory(this));
        }

        public void RegisterSingleton(Type from, Type to, string name = null)
        {
            _container.RegisterType(from, to, name, new ContainerControlledLifetimeManager());
        }

        public void RegisterType(Type from, Type to, string name = null)
        {
            _container.RegisterType(from, to, name);
        }

        public void RegisterType(Type type, string name = null)
        {
            _container.RegisterType(type, name);
        }

        public object Resolve(Type type, string name)
        {
            return _container.Resolve(type, name);
        }

        public IEnumerable<object> ResolveAll(Type type)
        {
            return _container.ResolveAll(type);
        }
    }
}