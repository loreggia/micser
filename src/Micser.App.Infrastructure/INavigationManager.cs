namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        void Navigate<TView>(object parameter = null, string regionName = AppGlobals.PrismRegions.Main);
    }
}