using System;
using System.Globalization;
using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    public class DoubleToPointConverter : ConverterExtension
    {
        public bool SetX { get; set; }
        public bool SetY { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new Point(SetX ? d : 0, SetY ? d : 0);
            }

            return new Point();
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Point p)
            {
                return SetX ? p.X : SetY ? p.Y : 0d;
            }

            return 0d;
        }
    }
}