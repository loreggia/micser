namespace Micser.Infrastructure
{
    public interface INavigationManager
    {
        void Navigate<TView>(string regionName = Globals.PrismRegions.Main);
    }
}