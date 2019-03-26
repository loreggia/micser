using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    public class EnumToBooleanConverter : ConverterExtension
    {
        public object Value { get; set; }

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