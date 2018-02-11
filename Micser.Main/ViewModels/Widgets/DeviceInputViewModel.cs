using System.Collections.Generic;
using System.Linq;
using Micser.Infrastructure;
using Micser.Main.Audio;
using Micser.Main.Services;

namespace Micser.Main.ViewModels.Widgets
{
    public class DeviceInputViewModel : AudioChainLinkViewModel
    {
        private IAudioChainLink _currentOutputLink;
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceInput _deviceInput;
        private ConnectorViewModel _outputViewModel;
        private DeviceDescription _selectedDeviceDescription;

        public IEnumerable<DeviceDescription> DeviceDescriptions
        {
            get => _deviceDescriptions;
            set => SetProperty(ref _deviceDescriptions, value);
        }

        public DeviceDescription SelectedDeviceDescription
        {
            get => _selectedDeviceDescription;
            set
            {
                if (SetProperty(ref _selectedDeviceDescription, value))
                {
                    _deviceInput.DeviceDescription = value;
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _outputViewModel = AddOutput(_deviceInput);

            _deviceInput = new DeviceInput();
            UpdateDeviceDescriptions();

            _deviceInput.DeviceDescription = SelectedDeviceDescription;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _deviceInput?.Dispose();
                _deviceInput = null;
            }
        }

        protected override void OnOutputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            base.OnOutputConnectionChanged(sender, e);

            if (_currentOutputLink != null)
            {
                _currentOutputLink.Input = null;
            }

            if (e.NewConnection?.Sink?.Data is IAudioChainLink audioChainLink)
            {
                audioChainLink.Input = _deviceInput;
                _currentOutputLink = audioChainLink;
            }
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Input).ToArray();
        }
    }
}