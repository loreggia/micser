using Micser.App.ViewModels;
using System.Windows.Input;

namespace Micser.App.Views
{
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();

            Loaded += MainView_Loaded;
        }

        private MainViewModel MainViewModel => (MainViewModel)DataContext;

        private void MainView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetPanel.Focus();
        }

        private void RefreshCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void RefreshCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
        }
    }
}