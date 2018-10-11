namespace Micser.Infrastructure
{
    public interface INavigationManager
    {
        void Navigate<TView>(string region = Globals.PrismRegions.Main);
    }
}