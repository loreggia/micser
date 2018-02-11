using System.Collections.Generic;
using System.Linq;
using Micser.Infrastructure;
using Micser.Main.Audio;
using Micser.Main.Services;

namespace Micser.Main.ViewModels.Widgets
{
    public class DeviceOutputViewModel : AudioChainLinkViewModel
    {
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceOutput _deviceOutput;
        private ConnectorViewModel _inputViewModel;
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
                if (SetProperty(ref _selectedDeviceDescription, value) && _deviceOutput != null)
                {
                    _deviceOutput.DeviceDescription = value;
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _inputViewModel = AddInput(_deviceOutput);

            _deviceOutput = new DeviceOutput();
            UpdateDeviceDescriptions();

            _deviceOutput.DeviceDescription = SelectedDeviceDescription;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _deviceOutput?.Dispose();
                _deviceOutput = null;
            }
        }

        protected override void OnInputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            base.OnInputConnectionChanged(sender, e);

            if (e.NewConnection?.Source?.Data is IAudioChainLink audioChainLink)
            {
                _deviceOutput.Input = audioChainLink;
            }
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Output).ToArray();
        }
    }
}