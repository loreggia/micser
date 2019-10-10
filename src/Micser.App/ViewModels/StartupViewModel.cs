using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Navigation;
using Micser.App.Settings;
using Micser.App.Views;
using Micser.Common.Api;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Prism.Events;
using System.Threading.Tasks;
using System.Windows;

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
            bool isConnected = false;

            for (int i = 0; i < 5; i++)
            {
                var result = await _apiClient.ConnectAsync();
                isConnected = result || _apiClient.State == ConnectionState.Connected;

                if (isConnected)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            await _settingsService.LoadAsync(true);

            var shellState = _settingsService.GetSetting<ShellState>(AppGlobals.SettingKeys.ShellState);
            var shell = Application.Current.MainWindow;

            if (shellState != null && shell != null)
            {
                shell.Width = shellState.Width;
                shell.Height = shellState.Height;
                shell.Top = shellState.Top;
                shell.Left = shellState.Left;

                if (shellState.State != WindowState.Minimized)
                {
                    shell.WindowState = shellState.State;
                }
            }

            if (isConnected)
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