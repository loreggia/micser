using System;
using System.Globalization;
using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Value converter that converts a <see cref="double"/> value to a rectangle.
    /// The result is a square with its width and height from the conversion value and a <see cref="Rect.Location"/> of (0,0).
    /// </summary>
    public class DoubleToRectConverter : ConverterExtension
    {
        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new Rect(0, 0, d, d);
            }

            return null;
        }

        /// <inheritdoc />
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Rect r)
            {
                return r.Width;
            }

            return null;
        }
    }
}