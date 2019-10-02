using Micser.App.Infrastructure;
using Prism.Events;

namespace Micser.App.ViewModels
{
    public class MainStatusBarViewModel : ViewModelNavigationAware
    {
        private string _statusText;

        public MainStatusBarViewModel(IEventAggregator eventAggregator)
        {
            var statusEvent = eventAggregator.GetEvent<ApplicationEvents.StatusChange>();
            statusEvent.Subscribe(OnStatusChange);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private void OnStatusChange(string text)
        {
            StatusText = text;
        }
    }
}