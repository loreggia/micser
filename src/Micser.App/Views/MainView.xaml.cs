using Micser.App.ViewModels;
using System.Windows.Input;

namespace Micser.App.Views
{
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
        }

        private MainViewModel MainViewModel => (MainViewModel)DataContext;

        private void RefreshCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void RefreshCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
        }
    }
}