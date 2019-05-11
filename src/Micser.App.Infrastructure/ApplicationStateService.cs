using Prism.Events;

namespace Micser.App.Infrastructure
{
    /// <inheritdoc cref="IApplicationStateService" />
    public class ApplicationStateService : IApplicationStateService
    {
        private readonly IEventAggregator _eventAggregator;

        private bool _isInitialized;

        /// <summary>
        /// Creates an instance of the <see cref="ApplicationStateService"/> class.
        /// </summary>
        public ApplicationStateService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc />
        public bool ModulesLoaded { get; private set; }

        /// <inheritdoc />
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