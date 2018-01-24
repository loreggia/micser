using System.Collections.Generic;
using Micser.Core.Common;

namespace Micser.Core.ViewModels
{
    public class MainViewModel : Bindable
    {
        private IEnumerable<InputChannelViewModel> _inputChannels;

        public MainViewModel()
        {
            InputChannels = new[] { new InputChannelViewModel { Name = "Test" } };
        }

        public IEnumerable<InputChannelViewModel> InputChannels
        {
            get => _inputChannels;
            set => SetProperty(ref _inputChannels, value);
        }
    }
}