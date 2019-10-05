namespace Micser.App.Views
{
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();

            Loaded += MainView_Loaded;
        }

        private void MainView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetPanel.Focus();
        }
    }
}