using System.ComponentModel;

namespace Micser.Common.Extensions
{
    public static class ObjectExtensions
    {
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