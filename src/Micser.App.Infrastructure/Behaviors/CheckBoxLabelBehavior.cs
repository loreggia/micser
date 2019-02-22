using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Behaviors
{
    public static class CheckBoxLabelBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(CheckBoxLabelBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Label label)
            {
                label.MouseLeftButtonUp += (s, me) =>
                {
                    if (label.Target is CheckBox cb)
                    {
                        cb.IsChecked = !cb.IsChecked;
                    }
                };
            }
        }
    }
}