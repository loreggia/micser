using Micser.Infrastructure;
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

        public void Navigate<TView>(string regionName = Globals.PrismRegions.Main)
        {
            _regionManager.RequestNavigate(regionName, new Uri(typeof(TView).Name, UriKind.Relative));
        }
    }
}