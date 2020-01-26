using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Interaction;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Windows;

namespace Micser.App
{
    public class MainShellViewModel : ViewModel
    {
        private readonly IDialogService _dialogService;

        public MainShellViewModel(IEventAggregator eventAggregator, IDialogService dialogService)
        {
            _dialogService = dialogService;
            eventAggregator.GetEvent<MessageBoxEvent>().Subscribe(ShowMessageBox, ThreadOption.UIThread, false);
        }

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

            // TODO return ButtonResult?
            _dialogService.ShowMessageBox(args.Message, buttons, image, result => args.Callback?.Invoke(result == ButtonResult.OK || result == ButtonResult.Yes));
        }
    }
}