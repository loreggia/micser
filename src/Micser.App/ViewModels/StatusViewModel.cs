using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Views;
using Micser.Common.Api;
using Prism.Commands;
using System;

namespace Micser.App.ViewModels
{
    public enum StatusType
    {
        Unknown,
        ConnectionFailed
    }

    public class StatusViewModel : ViewModelNavigationAware
    {
        private readonly IApiEndPoint _apiEndPoint;
        private readonly INavigationManager _navigationManager;
        private readonly StatusApiClient _statusApiClient;
        private string _actionText;
        private bool _canExecuteAction;
        private StatusType _currentStatus;
        private string _statusText;

        public StatusViewModel(INavigationManager navigationManager, IApiEndPoint apiEndPoint, StatusApiClient statusApiClient)
        {
            _navigationManager = navigationManager;
            _statusApiClient = statusApiClient;

            ActionCommand = new DelegateCommand(OnActionCommand, () => CanExecuteAction).ObservesProperty(() => CanExecuteAction);
            _apiEndPoint = apiEndPoint;
        }

        public DelegateCommand ActionCommand { get; set; }

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

        protected override void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);
            if (parameter is StatusType status)
            {
                _currentStatus = status;

                switch (status)
                {
                    case StatusType.Unknown:
                        StatusText = "Unknown error. You should exit and restart the application.";
                        ActionText = "Exit application";
                        CanExecuteAction = true;
                        break;

                    case StatusType.ConnectionFailed:
                        StatusText = "Connection to the service failed.";
                        ActionText = "Retry";
                        CanExecuteAction = true;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                StatusText = "Unknown error";
            }
        }

        private async void OnActionCommand()
        {
            try
            {
                IsBusy = true;
                CanExecuteAction = false;

                switch (_currentStatus)
                {
                    case StatusType.Unknown:
                        CustomApplicationCommands.Exit.Execute(null, null);
                        break;

                    case StatusType.ConnectionFailed:
                        var isConnected = _apiEndPoint.State == EndPointState.Connected;

                        if (!isConnected)
                        {
                            isConnected = await _apiEndPoint.ConnectAsync();
                        }

                        if (isConnected)
                        {
                            var statusResult = await _statusApiClient.GetStatus();

                            if (statusResult.IsSuccess)
                            {
                                _navigationManager.Navigate<MainStatusBarView>(AppGlobals.PrismRegions.Status);
                                _navigationManager.Navigate<MainMenuView>(AppGlobals.PrismRegions.Menu);
                                _navigationManager.Navigate<ToolBarView>(AppGlobals.PrismRegions.TopToolBar, AppGlobals.ToolBarIds.Main);
                                _navigationManager.Navigate<MainView>(AppGlobals.PrismRegions.Main);
                                return;
                            }
                        }

                        CanExecuteAction = true;

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}