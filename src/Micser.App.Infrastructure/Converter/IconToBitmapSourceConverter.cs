using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Micser.App.Infrastructure.Converter
{
    [ValueConversion(typeof(Icon), typeof(BitmapSource))]
    public class IconToBitmapSourceConverter : ConverterExtension
    {
        /// <inheritdoc />
        public override object Convert(object value, Type type, object parameter, CultureInfo culture)
        {
            if (value is Icon icon)
            {
                var bs = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                return bs;
            }

            return null;
        }

        /// <inheritdoc />
        public override object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}