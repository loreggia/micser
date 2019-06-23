using System.ComponentModel;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Extension methods usable on all objects.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Tries to convert the object to an instance of type <typeparamref name="T"/> using <see cref="TypeDescriptor"/>/<see cref="TypeConverter"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An instance of type <typeparamref name="T"/> if the conversion was successful or <c>default(T)</c>.</returns>
        public static T ToType<T>(this object value)
        {
            var valueType = value.GetType();
            var resultType = typeof(T);

            if (valueType == resultType || resultType.IsAssignableFrom(valueType))
            {
                return (T)value;
            }

            var valueConverter = TypeDescriptor.GetConverter(valueType);
            if (valueConverter.CanConvertTo(resultType))
            {
                return (T)valueConverter.ConvertTo(value, resultType);
            }

            var resultConverter = TypeDescriptor.GetConverter(resultType);
            if (resultConverter.CanConvertFrom(valueType))
            {
                return (T)resultConverter.ConvertFrom(value);
            }

            return default;
        }
    }
}