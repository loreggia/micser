using Prism.Events;
using Prism.Regions;
using System;

namespace Micser.App.Infrastructure
{
    public class NavigationManager : INavigationManager, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        public NavigationManager(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        public bool CanGoBack(string regionName)
        {
            var journal = _regionManager.Regions[regionName].NavigationService.Journal;
            return journal.CanGoBack;
        }

        public bool CanGoForward(string regionName)
        {
            var journal = _regionManager.Regions[regionName].NavigationService.Journal;
            return journal.CanGoForward;
        }

        public void ClearJournal(string regionName)
        {
            var journal = GetJournal(regionName);
            var currentEntry = journal.CurrentEntry;
            journal.Clear();
            journal.RecordNavigation(currentEntry, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void GoBack(string regionName)
        {
            var journal = GetJournal(regionName);
            journal.GoBack();
        }

        public void GoForward(string regionName)
        {
            var journal = GetJournal(regionName);
            journal.GoForward();
        }

        public void Navigate<TView>(string regionName, object parameter = null)
        {
            var viewName = typeof(TView).Name;
            var region = _regionManager.Regions[regionName];
            var currentEntry = region.NavigationService.Journal.CurrentEntry;
            if (currentEntry != null &&
                currentEntry.Uri.OriginalString == viewName &&
                Equals(currentEntry.Parameters[AppGlobals.NavigationParameterKey], parameter))
            {
                return;
            }

            region.NavigationService.Navigated -= OnNavigated;
            region.NavigationService.Navigated += OnNavigated;
            _regionManager.RequestNavigate(regionName, new Uri(viewName, UriKind.Relative), new NavigationParameters { { AppGlobals.NavigationParameterKey, parameter } });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var region in _regionManager.Regions)
                {
                    region.NavigationService.Navigated -= OnNavigated;
                }
            }
        }

        private IRegionNavigationJournal GetJournal(string regionName)
        {
            return _regionManager.Regions[regionName].NavigationService.Journal;
        }

        private void OnNavigated(object sender, RegionNavigationEventArgs e)
        {
            _eventAggregator.GetEvent<ApplicationEvents.Navigated>().Publish(new ApplicationEvents.Navigated.NavigationInfo
            {
                RegionName = e.NavigationContext.NavigationService.Region.Name,
                Parameter = e.NavigationContext.Parameters[AppGlobals.NavigationParameterKey],
                ViewName = e.NavigationContext.Uri.OriginalString
            });
        }
    }
}