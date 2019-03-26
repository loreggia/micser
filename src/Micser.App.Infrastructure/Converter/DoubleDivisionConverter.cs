using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    public class DoubleDivisionConverter : ConverterExtension
    {
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