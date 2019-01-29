using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Micser.Common.DataAccess
{
    public enum EntryState
    {
        Unchanged,
        Added,
        Changed,
        Deleted
    }

    public class DbEntry<T> : DbEntry
    {
        private readonly PropertyInfo _idProperty;
        private object _id;

        public DbEntry(T entity)
        {
            Entity = entity;

            _idProperty = GetIdProperty<T>();

            _id = _idProperty?.GetValue(entity) ?? Guid.NewGuid();
        }

        public T Entity { get; }

        public object Id
        {
            get => _id;
            set
            {
                _id = value;
                _idProperty?.SetValue(Entity, value);
            }
        }

        public override object GetEntity()
        {
            return Entity;
        }
    }

    public abstract class DbEntry
    {
        private static readonly Dictionary<Type, PropertyInfo> PropertyCache;

        static DbEntry()
        {
            PropertyCache = new Dictionary<Type, PropertyInfo>();
        }

        public EntryState State { get; set; }

        public static PropertyInfo GetIdProperty<T>()
        {
            var type = typeof(T);

            if (PropertyCache.ContainsKey(type))
            {
                return PropertyCache[type];
            }

            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty;

            var allProperties = type.GetProperties(bindingFlags);

            var idProperty = allProperties.FirstOrDefault(p => p.GetCustomAttribute<IdAttribute>() != null) ??
                             allProperties.FirstOrDefault(p => p.Name == "Id");

            if (idProperty != null)
            {
                PropertyCache.Add(type, idProperty);
            }

            return idProperty;
        }

        public abstract object GetEntity();
    }
}