using System.Collections.Generic;
using System.Linq;
using Micser.Main.Audio;
using Micser.Main.Services;

namespace Micser.Main.ViewModels.Widgets
{
    public class DeviceInputViewModel : AudioChainLinkViewModel
    {
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceInput _deviceInput;
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

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Input).ToArray();
        }
    }
}