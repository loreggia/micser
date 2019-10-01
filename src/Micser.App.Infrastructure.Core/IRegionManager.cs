namespace Micser.App.Infrastructure
{
    public interface IRegionManager
    {
        void RequestNavigate(string regionName, string uri);
    }
}