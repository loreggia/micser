using System.Windows;

namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// A value converter that converts a <see cref="bool"/> value to a <see cref="Visibility"/> value.
    /// </summary>
    public class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        /// <summary>
        /// Creates an instance of the <see cref="BooleanToVisibilityConverter"/> class.
        /// The default values are: true -> <see cref="Visibility.Visible"/>, false -> <see cref="Visibility.Collapsed"/>.
        /// </summary>
        public BooleanToVisibilityConverter()
            : base(Visibility.Visible, Visibility.Collapsed)
        {
        }
    }
}