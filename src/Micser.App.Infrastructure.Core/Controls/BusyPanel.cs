using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// A simple control that shows a loading spinner.
    /// </summary>
    public class BusyPanel : Control
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="ShowText"/> property.
        /// </summary>
        public static readonly DependencyProperty ShowTextProperty =
            DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(BusyPanel), new PropertyMetadata(true));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="SpinnerSize"/> property.
        /// </summary>
        public static readonly DependencyProperty SpinnerSizeProperty =
            DependencyProperty.Register(nameof(SpinnerSize), typeof(double), typeof(BusyPanel), new PropertyMetadata(50d));

        static BusyPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyPanel), new FrameworkPropertyMetadata(typeof(BusyPanel)));
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the "Loading..." text is shown.
        /// </summary>
        public bool ShowText
        {
            get => (bool)GetValue(ShowTextProperty);
            set => SetValue(ShowTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the loading spinner.
        /// </summary>
        public double SpinnerSize
        {
            get => (double)GetValue(SpinnerSizeProperty);
            set => SetValue(SpinnerSizeProperty, value);
        }
    }
}