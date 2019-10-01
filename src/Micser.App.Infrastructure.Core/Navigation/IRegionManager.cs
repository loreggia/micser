namespace Micser.App.Infrastructure.Navigation
{
    public interface IRegionManager
    {
        IRegionCollection Regions { get; }

        void RequestNavigate(string regionName, string uri);
    }
}