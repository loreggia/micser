using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Micser.Infrastructure;
using Micser.Main.Services;
using Prism.Commands;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class InputChannelViewModel : ViewModel
    {
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private string _name;
        private DeviceDescription _selectedDeviceDescription;

        public IEnumerable<DeviceDescription> DeviceDescriptions
        {
            get => _deviceDescriptions;
            set => SetProperty(ref _deviceDescriptions, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ICommand RemoveCommand => new DelegateCommand(Remove);

        public DeviceDescription SelectedDeviceDescription
        {
            get => _selectedDeviceDescription;
            set => SetProperty(ref _selectedDeviceDescription, value);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            UpdateDeviceDescriptions();
        }

        private void Remove()
        {
            // todo
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Input).ToArray();
        }
    }
}