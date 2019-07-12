using Prism.Interactivity.InteractionRequest;
using System;
using System.Drawing;
using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    public class MessageBoxViewModel : ViewModel
    {
        private MessageBoxButton _buttons;
        private string _cancelText;
        private INotification _confirmation;
        private string _confirmationText;
        private MessageBoxImage _image;
        private Icon _imageIcon;
        private string _message;
        private string _title;

        public MessageBoxButton Buttons
        {
            get => _buttons;
            set
            {
                if (SetProperty(ref _buttons, value))
                {
                    SetButtonTexts();
                }
            }
        }

        public string CancelText
        {
            get => _cancelText;
            set => SetProperty(ref _cancelText, value);
        }

        public string ConfirmationText
        {
            get => _confirmationText;
            set => SetProperty(ref _confirmationText, value);
        }

        public MessageBoxImage Image
        {
            get => _image;
            set
            {
                if (SetProperty(ref _image, value))
                {
                    SetImageIcon();
                }
            }
        }

        public Icon ImageIcon
        {
            get => _imageIcon;
            set => SetProperty(ref _imageIcon, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public INotification Notification
        {
            get => _confirmation;
            set => SetProperty(ref _confirmation, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private void SetButtonTexts()
        {
            switch (Buttons)
            {
                case MessageBoxButton.OK:
                    ConfirmationText = "OK";
                    CancelText = null;
                    break;

                case MessageBoxButton.OKCancel:
                    ConfirmationText = "OK";
                    CancelText = "Cancel";
                    break;

                case MessageBoxButton.YesNoCancel:
                    throw new NotSupportedException("MessageBoxButton mode YesNoCancel is not supported.");

                case MessageBoxButton.YesNo:
                    ConfirmationText = "Yes";
                    CancelText = "No";
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetImageIcon()
        {
            switch (Image)
            {
                case MessageBoxImage.None:
                    ImageIcon = null;
                    break;

                case MessageBoxImage.Error:
                    ImageIcon = SystemIcons.Error;
                    break;

                case MessageBoxImage.Question:
                    ImageIcon = SystemIcons.Question;
                    break;

                case MessageBoxImage.Warning:
                    ImageIcon = SystemIcons.Warning;
                    break;

                case MessageBoxImage.Information:
                    ImageIcon = SystemIcons.Information;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}