namespace Micser.App.Infrastructure.Navigation
{
    /// <summary>
    /// Handles UI navigation.
    /// </summary>
    public interface INavigationManager
    {
        /// <summary>
        /// Gets a value that indicates whether back navigation is allowed in the specified region.
        /// </summary>
        bool CanGoBack(string regionName);

        /// <summary>
        /// Gets a value that indicates whether forward navigation is allowed in the specified region.
        /// </summary>
        bool CanGoForward(string regionName);

        /// <summary>
        /// Clears the navigation journal/stack in the specified region.
        /// </summary>
        void ClearJournal(string regionName);

        /// <summary>
        /// Requests a back navigation in the specified region.
        /// </summary>
        void GoBack(string regionName);

        /// <summary>
        /// Requests a forward navigation in the specified region.
        /// </summary>
        void GoForward(string regionName);

        /// <summary>
        /// Navigates to view specified by <typeparamref name="TView"/> in the specified region.
        /// </summary>
        /// <param name="regionName">The name of the region to perform the navigation in.</param>
        /// <param name="parameter">An optional parameter that is sent to the <see cref="ViewModelNavigationAware.OnNavigatedTo(object)"/> method
        /// if the navigation target's view model derives from <see cref="ViewModelNavigationAware"/>.</param>
        void Navigate<TView>(string regionName, object parameter = null);
    }
}