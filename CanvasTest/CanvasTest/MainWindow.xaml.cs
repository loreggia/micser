using System.Windows;

namespace CanvasTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            var widget = new Widget();
            widget.DataContext = new WidgetViewModel { Position = new Point(50, 50) };

            Canvas.Children.Add(widget);
        }
    }
}