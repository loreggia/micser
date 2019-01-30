using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Micser.Common.Extensions
{
    public static class SerializationExtensions
    {
        public static void AddProperty<T>(this SerializationInfo info, Expression<Func<T>> property)
        {
            var propertyName = PropertyExtensions.GetName(property);
            info.AddValue(propertyName, property.Compile()(), typeof(T));
        }

        public static T GetProperty<T>(this SerializationInfo info, Expression<Func<T>> property, bool throwIfNotFound = false)
        {
            var propertyName = PropertyExtensions.GetName(property);

            if (info.GetValue(propertyName, typeof(T)) is T value)
            {
                return value;
            }

            if (throwIfNotFound)
            {
                throw new SerializationException($"No value with name '{propertyName}' was found.");
            }

            return default(T);
        }
    }
}