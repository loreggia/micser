using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    public class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter()
            : base(Visibility.Visible, Visibility.Collapsed)
        {
        }
    }
}