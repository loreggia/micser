using System;
using System.Globalization;
using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Value converter that converts a <see cref="double"/> value to a <see cref="Point"/> instance.
    /// </summary>
    public class DoubleToPointConverter : ConverterExtension
    {
        /// <summary>
        /// Gets or sets whether to set the conversion value in the resulting <see cref="Point.X"/> component.
        /// </summary>
        public bool SetX { get; set; }

        /// <summary>
        /// Gets or sets whether to set the conversion value in the resulting <see cref="Point.Y"/> component.
        /// </summary>
        public bool SetY { get; set; }

        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new Point(SetX ? d : 0, SetY ? d : 0);
            }

            return new Point();
        }

        /// <inheritdoc />
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