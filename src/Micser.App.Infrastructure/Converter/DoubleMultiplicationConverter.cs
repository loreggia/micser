using Micser.Common.Extensions;
using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Value converter that divides a <see cref="double"/> value by a divisor.
    /// </summary>
    public class DoubleMultiplicationConverter : ConverterExtension
    {
        /// <summary>
        /// Gets or sets the factor the value is multiplied by.
        /// </summary>
        public double Factor { get; set; }

        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var d = value.ToType<double>();
            var factor = parameter is double dParam ? dParam : Factor;
            return d * factor;
        }

        /// <inheritdoc />
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var d = value.ToType<double>();

            var divisor = parameter is double dParam ? dParam : Factor;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (divisor == 0)
            {
                divisor = 1d;
            }

            return d / divisor;
        }
    }
}