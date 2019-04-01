using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    public class BooleanConverter<T> : ConverterExtension
    {
        public BooleanConverter()
        {
        }

        public BooleanConverter(T trueValue, T falseValue)
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        public T FalseValue { get; set; }
        public T TrueValue { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? TrueValue : FalseValue;
            }

            return FalseValue;
        }

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