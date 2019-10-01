using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    public partial class MessageBoxWindow
    {
        /// <inheritdoc />
        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        private MessageBoxViewModel ViewModel => (MessageBoxViewModel)DataContext;

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.Notification is IConfirmation confirmation)
            {
                confirmation.Confirmed = false;
            }

            Close();
        }

        private void OnConfirmationButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.Notification is IConfirmation confirmation)
            {
                confirmation.Confirmed = true;
            }

            Close();
        }
    }
}