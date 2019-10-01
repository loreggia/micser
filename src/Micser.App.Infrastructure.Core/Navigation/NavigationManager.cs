using Micser.App.Infrastructure.Events;
using System;

namespace Micser.App.Infrastructure.Navigation
{
    /// <inheritdoc cref="INavigationManager" />
    public class NavigationManager : INavigationManager, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        /// <inheritdoc />
        public NavigationManager(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc />
        public bool CanGoBack(string regionName)
        {
            var journal = _regionManager.Regions[regionName].NavigationService.Journal;
            return journal.CanGoBack;
        }

        /// <inheritdoc />
        public bool CanGoForward(string regionName)
        {
            var journal = _regionManager.Regions[regionName].NavigationService.Journal;
            return journal.CanGoForward;
        }

        /// <inheritdoc />
        public void ClearJournal(string regionName)
        {
            var journal = GetJournal(regionName);
            var currentEntry = journal.CurrentEntry;
            journal.Clear();
            journal.RecordNavigation(currentEntry, true);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void GoBack(string regionName)
        {
            var journal = GetJournal(regionName);
            journal.GoBack();
        }

        /// <inheritdoc />
        public void GoForward(string regionName)
        {
            var journal = GetJournal(regionName);
            journal.GoForward();
        }

        /// <inheritdoc />
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

        /// <summary>
        /// Releases resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_regionManager != null)
                {
                    foreach (var region in _regionManager.Regions)
                    {
                        region.NavigationService.Navigated -= OnNavigated;
                    }
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