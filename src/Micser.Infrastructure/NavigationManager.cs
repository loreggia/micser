using Prism.Regions;
using System;

namespace Micser.Infrastructure
{
    public class NavigationManager : INavigationManager
    {
        private readonly IRegionManager _regionManager;

        public NavigationManager(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void Navigate<TView>(string region = Globals.PrismRegions.Main)
        {
            _regionManager.RequestNavigate(region, new Uri(typeof(TView).Name, UriKind.Relative));
        }
    }
}