using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Settings;
using Micser.App.Settings;
using Micser.App.Views;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.ViewModels
{
    public class StartupViewModel : ViewModelNavigationAware
    {
        private readonly INavigationManager _navigationManager;
        private readonly ISettingsService _settingsService;
        private readonly StatusApiClient _statusApiClient;

        public StartupViewModel(
            IApplicationStateService applicationStateService,
            IEventAggregator eventAggregator,
            INavigationManager navigationManager,
            ISettingsService settingsService,
            StatusApiClient statusApiClient)
        {
            _navigationManager = navigationManager;
            _settingsService = settingsService;
            _statusApiClient = statusApiClient;

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
            await _settingsService.LoadAsync();

            await Task.Delay(2000);

            var shellState = _settingsService.GetSetting<ShellState>(AppGlobals.SettingKeys.ShellState);
            var shell = System.Windows.Application.Current.MainWindow;

            if (shellState != null && shell != null)
            {
                shell.Width = shellState.Width;
                shell.Height = shellState.Height;
                shell.Top = shellState.Top;
                shell.Left = shellState.Left;
            }

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