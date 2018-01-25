using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Micser.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainShell
    {
        private bool _isExiting;

        public MainShell()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !_isExiting;
            base.OnClosing(e);
            Hide();
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _isExiting = true;
            Close();
        }

        private void OnRestoreWindow(object sender, RoutedEventArgs e)
        {
            Show();
        }
    }
}