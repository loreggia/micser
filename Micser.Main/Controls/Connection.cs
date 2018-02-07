using System.Windows;
using System.Windows.Controls;

namespace Micser.Main.Controls
{
    public class Connection : Control, ISelectable
    {
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Connection), new PropertyMetadata(false));

        public static readonly DependencyProperty SinkProperty = DependencyProperty.Register(
            nameof(Sink), typeof(Connector), typeof(Connection), new PropertyMetadata(null));

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Connector), typeof(Connection), new PropertyMetadata(null));

        static Connection()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Connection), new FrameworkPropertyMetadata(typeof(Connection)));
        }

        public Connection(Connector source, Connector sink)
        {
            Source = source;
            Sink = sink;
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public Connector Sink
        {
            get => (Connector)GetValue(SinkProperty);
            set => SetValue(SinkProperty, value);
        }

        public Connector Source
        {
            get => (Connector)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
    }
}