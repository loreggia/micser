using System.Windows.Input;

namespace Micser.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainShell
    {
        public MainShell()
        {
            InitializeComponent();
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}