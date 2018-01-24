using System.Windows;
using System.Windows.Controls;

namespace Micser.Core.Controls
{
    public class InputChannelControl : Control
    {
        public static readonly DependencyProperty ChannelNameProperty = DependencyProperty.Register(
            nameof(ChannelName), typeof(string), typeof(InputChannelControl), new PropertyMetadata(null));

        static InputChannelControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InputChannelControl), new FrameworkPropertyMetadata(typeof(InputChannelControl)));
        }

        public string ChannelName
        {
            get => (string)GetValue(ChannelNameProperty);
            set => SetValue(ChannelNameProperty, value);
        }
    }
}