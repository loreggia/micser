using System.Collections.Generic;
using System.Linq;
using Micser.Main.Audio;
using Micser.Main.Services;
using Prism.Regions;

namespace Micser.Main.ViewModels.Widgets
{
    public class DeviceOutputViewModel : AudioChainLinkViewModel
    {
        private readonly DeviceOutput _deviceOutput;
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceDescription _selectedDeviceDescription;

        public DeviceOutputViewModel()
        {
            _deviceOutput = new DeviceOutput();
        }

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
                    _deviceOutput.DeviceDescription = value;
                }
            }
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            UpdateDeviceDescriptions();
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Output).ToArray();
        }
    }
}