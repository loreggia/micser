using Micser.App.Infrastructure;
using Prism.Commands;
using System.Threading.Tasks;

namespace Micser.App.ViewModels
{
    public class StatusViewModel : ViewModel
    {
        private readonly INavigationManager _navigationManager;
        private string _actionText;
        private bool _canExecuteAction;
        private string _statusText;

        public StatusViewModel(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            ActionCommand = new DelegateCommand(OnActionCommand);
            ActionCommand.ObservesProperty(() => CanExecuteAction);
        }

        public DelegateCommand ActionCommand { get; }

        public string ActionText
        {
            get => _actionText;
            set => SetProperty(ref _actionText, value);
        }

        public bool CanExecuteAction
        {
            get => _canExecuteAction;
            set => SetProperty(ref _canExecuteAction, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private async void OnActionCommand()
        {
            try
            {
                CanExecuteAction = false;

                await Task.Delay(2000);
            }
            finally
            {
                CanExecuteAction = true;
            }
        }
    }
}