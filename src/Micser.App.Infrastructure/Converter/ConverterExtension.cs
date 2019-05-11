using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// Base class for converters. This is a markup extension that allows inline declaration of the converter.
    /// </summary>
    public abstract class ConverterExtension : MarkupExtension, IValueConverter
    {
        /// <inheritdoc />
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        /// <inheritdoc />
        public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Returns this instance.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}