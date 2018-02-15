using System.Windows;

namespace CanvasTest
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            var widget = new DerivedWidget();
            widget.DataContext = new WidgetViewModel { Position = new Point(50, 50) };

            Canvas.Children.Add(widget);
        }
    }
}