using Micser.App.Infrastructure;
using Micser.App.Views;
using Micser.Infrastructure;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.ViewModels
{
    public class StartupViewModel : ViewModelNavigationAware
    {
        private readonly INavigationManager _navigationManager;
        private bool _isLoading;

        public StartupViewModel(IApplicationStateService applicationStateService, IEventAggregator eventAggregator, INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;

            IsLoading = true;

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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private async void OnModulesLoaded()
        {
            await Task.Delay(1000);
            _navigationManager.Navigate<MainStatusBarView>(Globals.PrismRegions.Status);
            _navigationManager.Navigate<MainMenuView>(Globals.PrismRegions.Menu);
            _navigationManager.Navigate<MainView>();
            IsLoading = false;
        }
    }
}