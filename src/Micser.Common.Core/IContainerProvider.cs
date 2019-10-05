using System;
using System.Collections.Generic;

namespace Micser.Common
{
    public interface IContainerProvider : IDisposable
    {
        void RegisterFactory(Type from, Func<IContainerProvider, object> factory, string name = null);

        void RegisterInstance(Type from, object instance, string name = null);

        void RegisterInstance<T>(T instance, string name = null);

        void RegisterSingleton(Type from, Type to, string name = null);

        void RegisterType(Type from, Type to, string name = null);

        void RegisterType(Type type, string name = null);

        object Resolve(Type type, string name = null);

        IEnumerable<object> ResolveAll(Type type);
    }
}