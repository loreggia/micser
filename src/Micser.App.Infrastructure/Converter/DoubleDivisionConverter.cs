using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    public class DoubleDivisionConverter : ConverterExtension
    {
        /// <summary>
        /// Gets or sets the divisor that the conversion value is divided by.
        /// </summary>
        public double Divisor { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                var divisor = Divisor != 0d ? Divisor : (parameter is double dParam) ? dParam : 1d;

                return d / divisor;
            }

            return value;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                var divisor = Divisor != 0d ? Divisor : (parameter is double dParam) ? dParam : 1d;

                return d * divisor;
            }

            return value;
        }
    }
}