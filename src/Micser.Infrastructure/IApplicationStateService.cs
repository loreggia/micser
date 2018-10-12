namespace Micser.Infrastructure
{
    public interface IApplicationStateService
    {
        bool ModulesLoaded { get; }

        void Initialize();
    }
}