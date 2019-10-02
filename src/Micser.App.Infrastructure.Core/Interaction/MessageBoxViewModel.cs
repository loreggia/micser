using Micser.App.Infrastructure.Resources;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Drawing;
using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// View model for generic message box interaction requests.
    /// </summary>
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

        /// <inheritdoc />
        public MessageBoxViewModel()
        {
            SetButtonTexts();
            SetImageIcon();
        }

        /// <summary>
        /// Gets or sets the confirmation/cancel button layout and labeling.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the text shown on the cancel button.
        /// </summary>
        public string CancelText
        {
            get => _cancelText;
            set => SetProperty(ref _cancelText, value);
        }

        /// <summary>
        /// Gets or sets the text shown on the confirmation button.
        /// </summary>
        public string ConfirmationText
        {
            get => _confirmationText;
            set => SetProperty(ref _confirmationText, value);
        }

        /// <summary>
        /// Gets or sets the image type of the icon displayed in the message box.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the icon corresponding to the <see cref="Image"/>.
        /// </summary>
        public Icon ImageIcon
        {
            get => _imageIcon;
            set => SetProperty(ref _imageIcon, value);
        }

        /// <summary>
        /// Gets or sets the main message text.
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// Gets or sets the notification/confirmation interaction request.
        /// </summary>
        public INotification Notification
        {
            get => _confirmation;
            set => SetProperty(ref _confirmation, value);
        }

        /// <summary>
        /// Gets or sets the message box title text.
        /// </summary>
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
                    ConfirmationText = Strings.MessageBoxButtonOk;
                    CancelText = null;
                    break;

                case MessageBoxButton.OKCancel:
                    ConfirmationText = Strings.MessageBoxButtonOk;
                    CancelText = Strings.MessageBoxButtonCancel;
                    break;

                case MessageBoxButton.YesNoCancel:
                    throw new NotSupportedException("MessageBoxButton mode YesNoCancel is not supported.");

                case MessageBoxButton.YesNo:
                    ConfirmationText = Strings.MessageBoxButtonYes;
                    CancelText = Strings.MessageBoxButtonNo;
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