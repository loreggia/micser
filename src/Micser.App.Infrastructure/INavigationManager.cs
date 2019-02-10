namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        void GoBack(string regionName);

        void Navigate<TView>(string regionName, object parameter = null, bool allowGoBack = true);
    }
}