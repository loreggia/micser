using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Settings;
using Micser.App.Settings;
using Micser.App.Views;
using Micser.Common.Api;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.ViewModels
{
    public class StartupViewModel : ViewModelNavigationAware
    {
        private readonly IApiEndPoint _apiEndPoint;
        private readonly INavigationManager _navigationManager;
        private readonly ISettingsService _settingsService;

        public StartupViewModel(
            IApplicationStateService applicationStateService,
            IEventAggregator eventAggregator,
            INavigationManager navigationManager,
            ISettingsService settingsService,
            IApiEndPoint apiEndPoint)
        {
            _navigationManager = navigationManager;
            _settingsService = settingsService;
            _apiEndPoint = apiEndPoint;

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

            var isConnected = await _apiEndPoint.ConnectAsync();

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