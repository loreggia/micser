using Micser.Infrastructure;

namespace Micser.App.Infrastructure
{
    public interface INavigationManager
    {
        void Navigate<TView>(string regionName = Globals.PrismRegions.Main);
    }
}