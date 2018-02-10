using System.Collections.Generic;
using System.Linq;
using Micser.Main.Audio;
using Micser.Main.Services;

namespace Micser.Main.ViewModels.Widgets
{
    public class DeviceOutputViewModel : AudioChainLinkViewModel
    {
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceOutput _deviceOutput;
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

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Output).ToArray();
        }
    }
}