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
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}