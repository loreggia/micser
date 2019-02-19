namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        bool CanGoBack(string regionName);

        bool CanGoForward(string regionName);

        void ClearJournal(string regionName);

        void GoBack(string regionName);

        void GoForward(string regionName);

        void Navigate<TView>(string regionName, object parameter = null);
    }
}