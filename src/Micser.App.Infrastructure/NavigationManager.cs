using Prism.Regions;
using System;
using System.Collections.Generic;

namespace Micser.App.Infrastructure
{
    public class NavigationManager : INavigationManager
    {
        private readonly Dictionary<string, bool> _allowGoBack;
        private readonly IRegionManager _regionManager;

        private string _lastRegionName;

        public NavigationManager(IRegionManager regionManager)
        {
            _allowGoBack = new Dictionary<string, bool>();
            _regionManager = regionManager;
        }

        public void GoBack(string regionName)
        {
            if (regionName == null)
            {
                regionName = _lastRegionName;
            }

            if (_allowGoBack.TryGetValue(regionName, out var allowGoBack) && !allowGoBack)
            {
                return;
            }

            _regionManager.Regions[regionName].NavigationService.Journal.GoBack();
        }

        public void Navigate<TView>(string regionName, object parameter = null, bool allowGoBack = true)
        {
            _allowGoBack[regionName] = allowGoBack;
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