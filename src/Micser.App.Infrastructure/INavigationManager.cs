namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        void ClearJournal(string regionName);

        void GoBack(string regionName);

        void Navigate<TView>(string regionName, object parameter = null);
    }
}