namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        void GoBack(string regionName);

        void Navigate<TView>(object parameter = null, string regionName = AppGlobals.PrismRegions.Main);
    }
}