using System;
using System.Globalization;
using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Value converter that converts a <see cref="string"/> instance to a <see cref="Visibility"/> value.
    /// If the string is empty, <see cref="Visibility.Collapsed"/> is returned, otherwise <see cref="Visibility.Visible"/>.
    /// </summary>
    public class EmptyStringToVisibilityConverter : ConverterExtension
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value as string))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}