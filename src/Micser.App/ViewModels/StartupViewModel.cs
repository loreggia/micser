using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Views;
using Prism.Events;

namespace Micser.App.ViewModels
{
    public class StartupViewModel : ViewModelNavigationAware
    {
        private readonly INavigationManager _navigationManager;
        private readonly StatusApiClient _statusApiClient;

        public StartupViewModel(
            IApplicationStateService applicationStateService,
            IEventAggregator eventAggregator,
            INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;

            _statusApiClient = new StatusApiClient();

            IsBusy = true;

            if (applicationStateService.ModulesLoaded)
            {
                OnModulesLoaded();
            }
            else
            {
                var modulesLoadedEvent = eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>();
                modulesLoadedEvent.Subscribe(OnModulesLoaded);
            }
        }

        private async void OnModulesLoaded()
        {
            var statusResult = await _statusApiClient.GetStatus();

            if (statusResult.IsSuccess)
            {
                _navigationManager.Navigate<MainStatusBarView>(AppGlobals.PrismRegions.Status);
                _navigationManager.Navigate<MainMenuView>(AppGlobals.PrismRegions.Menu);
                _navigationManager.Navigate<ToolBarView>(AppGlobals.PrismRegions.TopToolBar, AppGlobals.ToolBarIds.Main);
                _navigationManager.Navigate<MainView>(AppGlobals.PrismRegions.Main);
            }
            else
            {
                _navigationManager.Navigate<StatusView>(AppGlobals.PrismRegions.Main, StatusType.ConnectionFailed);
            }

            IsBusy = false;
        }
    }
}