using Prism.Regions;
using System;

namespace Micser.App.Infrastructure
{
    public class NavigationManager : INavigationManager
    {
        private readonly IRegionManager _regionManager;

        private string _lastRegionName;

        public NavigationManager(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void GoBack(string regionName)
        {
            if (regionName == null)
            {
                regionName = _lastRegionName;
            }

            _regionManager.Regions[regionName].NavigationService.Journal.GoBack();
        }

        public void Navigate<TView>(object parameter = null, string regionName = AppGlobals.PrismRegions.Main)
        {
            _regionManager.RequestNavigate(regionName, new Uri(typeof(TView).Name, UriKind.Relative), result =>
            {
                if (result.Result == true)
                {
                    _lastRegionName = regionName;
                }
            }, new NavigationParameters { { AppGlobals.NavigationParameterKey, parameter } });
        }
    }
}