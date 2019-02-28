using System;
using System.Globalization;

namespace Micser.App.Infrastructure.Converter
{
    public class RelativeCenterPlacementConverter : ConverterExtension
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return -(d / 2d);
            }

            return value;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}