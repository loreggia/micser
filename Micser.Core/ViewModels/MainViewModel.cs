using System.Collections.Generic;
using Micser.Core.Common;

namespace Micser.Core.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private IEnumerable<InputChannelViewModel> _inputChannels;

        public MainViewModel()
        {
            InputChannels = new[]
            {
                new InputChannelViewModel { Name = "Test 1" },
                new InputChannelViewModel { Name = "Test 2" },
            };
        }

        public IEnumerable<InputChannelViewModel> InputChannels
        {
            get => _inputChannels;
            set => SetProperty(ref _inputChannels, value);
        }
    }
}