using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Micser.Core.Common;
using Micser.Core.Services;

namespace Micser.Core.ViewModels
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

        public override Task InitializeAsync()
        {
            UpdateDeviceDescriptions();
            return base.InitializeAsync();
        }

        private void Remove(object param)
        {
            // todo
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetInputDevices().ToArray();
        }
    }
}