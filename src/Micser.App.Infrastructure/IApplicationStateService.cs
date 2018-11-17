namespace Micser.App.Infrastructure
{
    public interface IApplicationStateService
    {
        bool ModulesLoaded { get; }

        void Initialize();
    }
}