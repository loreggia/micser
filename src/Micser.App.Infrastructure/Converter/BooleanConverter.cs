using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Converts from the type <typeparamref name="T"/> to <see cref="bool"/>.
    /// </summary>
    public class BooleanConverter<T> : ConverterExtension
    {
        /// <summary>
        /// Creates an instance of the <see cref="BooleanConverter{T}"/> class.
        /// </summary>
        public BooleanConverter()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="BooleanConverter{T}"/> class.
        /// </summary>
        /// <param name="trueValue">The value to return when the source value is true.</param>
        /// <param name="falseValue">The value to return when the source value is false.</param>
        public BooleanConverter(T trueValue, T falseValue)
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        /// <summary>
        /// Gets or sets the value that corresponds to false.
        /// </summary>
        public T FalseValue { get; set; }

        /// <summary>
        /// Gets or sets the value that corresponds to true.
        /// </summary>
        public T TrueValue { get; set; }

        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? TrueValue : FalseValue;
            }

            return FalseValue;
        }

        /// <inheritdoc />
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is T t)
            {
                if (Equals(t, TrueValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}