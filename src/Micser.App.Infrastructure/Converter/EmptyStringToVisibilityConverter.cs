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
        /// <inheritdoc />
        public EmptyStringToVisibilityConverter()
        {
            EmptyValue = Visibility.Collapsed;
            NonEmptyValue = Visibility.Visible;
        }

        /// <summary>
        /// Gets or sets the visibility that is returned when the value is null or empty.
        /// </summary>
        public Visibility EmptyValue { get; set; }

        /// <summary>
        /// Gets or sets the visibility that is returned when the value is not null or empty.
        /// </summary>
        public Visibility NonEmptyValue { get; set; }

        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value as string))
            {
                return EmptyValue;
            }

            return NonEmptyValue;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}