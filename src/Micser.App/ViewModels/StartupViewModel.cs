using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Settings;
using Micser.App.Views;
using Micser.Common.Api;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.ViewModels
{
    public class StartupViewModel : ViewModelNavigationAware
    {
        private readonly IApiClient _apiClient;
        private readonly INavigationManager _navigationManager;
        private readonly ISettingsService _settingsService;

        public StartupViewModel(
            IApplicationStateService applicationStateService,
            IEventAggregator eventAggregator,
            INavigationManager navigationManager,
            ISettingsService settingsService,
            IApiClient apiClient)
        {
            _navigationManager = navigationManager;
            _settingsService = settingsService;
            _apiClient = apiClient;

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
            await Task.Run(() => { _settingsService.Load(); });

            var statusResult = await _apiClient.SendMessageAsync(new JsonRequest("status"));

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