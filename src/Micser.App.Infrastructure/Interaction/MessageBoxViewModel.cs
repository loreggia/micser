using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Drawing;
using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// View model for generic message box interaction requests.
    /// </summary>
    public class MessageBoxViewModel : ViewModel, IDialogAware
    {
        private MessageBoxButton _buttons;
        private MessageBoxImage _image;
        private Icon _imageIcon;
        private string _message;
        private IDialogParameters _parameters;
        private string _title;

        /// <inheritdoc />
        public MessageBoxViewModel()
        {
            CloseCommand = new DelegateCommand<ButtonResult?>(CloseDialog);
        }

        public event Action<IDialogResult> RequestClose;

        /// <summary>
        /// Gets or sets the confirmation/cancel button layout and labeling.
        /// </summary>
        public MessageBoxButton Buttons
        {
            get => _buttons;
            set => SetProperty(ref _buttons, value);
        }

        /// <summary>
        /// Gets a command that closes the dialog. Requires a parameter of type <see cref="ButtonResult"/>.
        /// </summary>
        public DelegateCommand<ButtonResult?> CloseCommand { get; }

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
        /// Gets or sets the message box title text.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public virtual bool CanCloseDialog()
        {
            return true;
        }

        public virtual void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            _parameters = parameters;

            Message = parameters.GetValue<string>("message");
            Image = parameters.GetValue<MessageBoxImage>("image");
            Buttons = parameters.GetValue<MessageBoxButton>("buttons");

            SetImageIcon();
        }

        protected virtual void CloseDialog(ButtonResult? result)
        {
            OnRequestClose(new DialogResult(result ?? ButtonResult.None, _parameters));
        }

        protected virtual void OnRequestClose(IDialogResult result)
        {
            RequestClose?.Invoke(result);
        }

        protected virtual void SetImageIcon()
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