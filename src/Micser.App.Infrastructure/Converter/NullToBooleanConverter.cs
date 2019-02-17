using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    public class NullToBooleanConverter : ConverterExtension
    {
        public NullToBooleanConverter()
        {
            NullValue = false;
        }

        public bool NullValue { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? NullValue : !NullValue;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}