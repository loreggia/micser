using System.Collections.Generic;
using Micser.Infrastructure;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private IEnumerable<InputChannelViewModel> _inputChannels;

        public MainViewModel()
        {
        }

        public IEnumerable<InputChannelViewModel> InputChannels
        {
            get => _inputChannels;
            set => SetProperty(ref _inputChannels, value);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            InputChannels = new[]
            {
                new InputChannelViewModel { Name = "Test 1" },
                new InputChannelViewModel { Name = "Test 2" },
            };

            foreach (var inputChannelViewModel in InputChannels)
            {
                inputChannelViewModel.OnNavigatedTo(navigationContext);
            }
        }
    }
}