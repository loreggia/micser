using System;
using System.Collections.Generic;

namespace Micser.Common
{
    public interface IContainerProvider : IDisposable
    {
        void RegisterFactory<T>(Func<IContainerProvider, T> factory);

        void RegisterInstance<T>(T instance);

        void RegisterSingleton(Type from, Type to, string name = null);

        void RegisterType(Type from, Type to, string name = null);

        void RegisterType(Type type, string name = null);

        object Resolve(Type type, string name = null);

        IEnumerable<object> ResolveAll(Type type);
    }
}