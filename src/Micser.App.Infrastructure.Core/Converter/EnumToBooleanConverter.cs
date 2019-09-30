using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Converter that simplifies binding an enum value to radio buttons.
    /// Returns true when the conversion value equals to the specified <see cref="Value"/>, otherwise false.
    /// </summary>
    public class EnumToBooleanConverter : ConverterExtension
    {
        /// <summary>
        /// Gets or sets the target enum value.
        /// </summary>
        public object Value { get; set; }

        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            var checkValue = value.ToString();
            var targetValue = parameter.ToString();

            return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || Value == null && parameter == null)
            {
                return null;
            }

            var useValue = (bool)value;
            var targetValue = (Value ?? parameter).ToString();

            if (useValue)
            {
                return Enum.Parse(targetType, targetValue);
            }

            return null;
        }
    }
}