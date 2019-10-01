using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Events;
using Micser.App.Infrastructure.Interaction;
using System;
using System.Windows;

namespace Micser.App
{
    public class MainShellViewModel : ViewModel
    {
        public MainShellViewModel(IEventAggregator eventAggregator)
        {
            MessageBoxInteractionRequest = new InteractionRequest<IConfirmation>();

            eventAggregator.GetEvent<MessageBoxEvent>().Subscribe(ShowMessageBox, ThreadOption.UIThread, false);
        }

        public InteractionRequest<IConfirmation> MessageBoxInteractionRequest { get; }

        private void ShowMessageBox(MessageBoxEventArgs args)
        {
            MessageBoxButton buttons;
            MessageBoxImage image;

            switch (args.Type)
            {
                case MessageBoxType.Information:
                    buttons = MessageBoxButton.OK;
                    image = MessageBoxImage.Information;
                    break;

                case MessageBoxType.Warning:
                    buttons = MessageBoxButton.OK;
                    image = MessageBoxImage.Warning;
                    break;

                case MessageBoxType.Error:
                    buttons = MessageBoxButton.OK;
                    image = MessageBoxImage.Error;
                    break;

                case MessageBoxType.Question:
                    buttons = MessageBoxButton.YesNo;
                    image = MessageBoxImage.Question;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            MessageBoxInteractionRequest.Raise(new Confirmation
            {
                Title = args.Title,
                Content = new MessageBoxContent
                {
                    Buttons = buttons,
                    Image = image,
                    Message = args.Message,
                    IsModal = args.IsModal,
                    ParentWindow = Application.Current.MainWindow
                }
            }, c => args.Callback?.Invoke(c.Confirmed));
        }
    }
}