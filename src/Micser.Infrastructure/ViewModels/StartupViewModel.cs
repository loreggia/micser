using Micser.Infrastructure.Views;
using Prism.Events;
using Prism.Regions;
using System.Threading.Tasks;

namespace Micser.Infrastructure.ViewModels
{
    public class StartupViewModel : ViewModelNavigationAware
    {
        private readonly INavigationManager _navigationManager;
        private bool _isLoading;

        public StartupViewModel(IEventAggregator eventAggregator, INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            var modulesLoadedEvent = eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>();
            modulesLoadedEvent.Subscribe(OnModulesLoaded);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            IsLoading = true;
            base.OnNavigatedTo(navigationContext);
        }

        private async void OnModulesLoaded()
        {
            await Task.Delay(1000);
            _navigationManager.Navigate<MainMenuView>(Globals.PrismRegions.Menu);
            _navigationManager.Navigate<MainView>();
            IsLoading = false;
        }
    }
}