using Prism.Events;

namespace Micser.App.Infrastructure
{
    public class ApplicationStateService : IApplicationStateService
    {
        private readonly IEventAggregator _eventAggregator;

        private bool _isInitialized;

        public ApplicationStateService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public bool ModulesLoaded { get; private set; }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>().Subscribe(() => ModulesLoaded = true);

            _isInitialized = true;
        }
    }
}