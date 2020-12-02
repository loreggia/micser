using System;
using System.Globalization;
using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    public enum MessageBoxButtonType
    {
        Ok,
        Yes,
        No,
        Cancel
    }

    public class MessageBoxButtonToVisibilityConverter : ConverterExtension
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageBoxButton buttonValue &&
                parameter is MessageBoxButtonType type)
            {
                var result = buttonValue switch
                {
                    MessageBoxButton.OK when type == MessageBoxButtonType.Ok => true,
                    MessageBoxButton.OKCancel when type == MessageBoxButtonType.Ok || type == MessageBoxButtonType.Cancel => true,
                    MessageBoxButton.YesNo when type == MessageBoxButtonType.Yes || type == MessageBoxButtonType.No => true,
                    MessageBoxButton.YesNoCancel when type == MessageBoxButtonType.Yes || type == MessageBoxButtonType.No || type == MessageBoxButtonType.Cancel => true,
                    _ => false
                };

                return result ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}