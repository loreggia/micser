using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Views;
using Prism.Events;
using System.Threading.Tasks;

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
                await Task.Delay(1000);
                _navigationManager.Navigate<MainStatusBarView>(null, AppGlobals.PrismRegions.Status);
                _navigationManager.Navigate<MainMenuView>(null, AppGlobals.PrismRegions.Menu);
                _navigationManager.Navigate<MainView>();
            }
            else
            {
                _navigationManager.Navigate<StatusView>(StatusType.ConnectionFailed);
            }

            IsBusy = false;
        }
    }
}