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
            _regionManager.Regions[regionName].NavigationService.Journal.Clear();
        }

        public void GoBack(string regionName)
        {
            _regionManager.Regions[regionName].NavigationService.Journal.GoBack();
        }

        public void Navigate<TView>(string regionName, object parameter = null)
        {
            _regionManager.RequestNavigate(regionName, new Uri(typeof(TView).Name, UriKind.Relative), new NavigationParameters { { AppGlobals.NavigationParameterKey, parameter } });
        }
    }
}