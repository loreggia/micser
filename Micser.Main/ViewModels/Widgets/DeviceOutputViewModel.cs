using System.Collections.Generic;
using System.Linq;
using Micser.Main.Audio;
using Micser.Main.Services;
using Prism.Regions;

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

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _deviceOutput.Dispose();
            _deviceOutput = null;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _deviceOutput = new DeviceOutput();
            UpdateDeviceDescriptions();

            _deviceOutput.DeviceDescription = SelectedDeviceDescription;
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Output).ToArray();
        }
    }
}