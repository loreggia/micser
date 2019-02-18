using Prism.Regions;
using System;

namespace Micser.App.Infrastructure
{
    public class NavigationManager : INavigationManager
    {
        private readonly IRegionManager _regionManager;

        public NavigationManager(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void ClearJournal(string regionName)
        {
            var journal = _regionManager.Regions[regionName].NavigationService.Journal;
            var currentEntry = journal.CurrentEntry;
            journal.Clear();
            journal.RecordNavigation(currentEntry, true);
        }

        public void GoBack(string regionName)
        {
            var navService = _regionManager.Regions[regionName].NavigationService;
            navService.NavigationFailed += OnNavigationFailed;
            navService.Journal.GoBack();
            navService.NavigationFailed -= OnNavigationFailed;
        }

        public void Navigate<TView>(string regionName, object parameter = null)
        {
            _regionManager.RequestNavigate(regionName, new Uri(typeof(TView).Name, UriKind.Relative), new NavigationParameters { { AppGlobals.NavigationParameterKey, parameter } });
        }

        private void OnNavigationFailed(object sender, RegionNavigationFailedEventArgs e)
        {
        }
    }
}