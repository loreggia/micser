namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        void Navigate<TView>(string regionName = AppGlobals.PrismRegions.Main);
    }
}