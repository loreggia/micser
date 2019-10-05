using System;

namespace Micser.Common
{
    public class DependencyOverride
    {
        public DependencyOverride(Type dependencyType, object instance)
        {
            DependencyType = dependencyType;
            Instance = instance;
        }

        public Type DependencyType { get; }
        public object Instance { get; }
    }

    public class DependencyOverride<T> : DependencyOverride
    {
        public DependencyOverride(T instance)
            : base(typeof(T), instance)
        {
        }
    }
}