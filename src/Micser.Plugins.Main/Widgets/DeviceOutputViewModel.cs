using Micser.App.Infrastructure.Widgets;
using Micser.Common.Devices;
using Micser.Common.Widgets;
using Micser.Plugins.Main.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Plugins.Main.Widgets
{
    public class DeviceOutputViewModel : WidgetViewModel
    {
        public const string InputConnectorName = "Input1";
        public const string SettingKeyDeviceId = "DeviceId";

        private readonly ConnectorViewModel _inputViewModel;
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceDescription _selectedDeviceDescription;

        public DeviceOutputViewModel()
        {
            Header = "Device Output";
            _inputViewModel = AddInput(InputConnectorName);
        }

        public IEnumerable<DeviceDescription> DeviceDescriptions
        {
            get => _deviceDescriptions;
            set => SetProperty(ref _deviceDescriptions, value);
        }

        public override Type ModuleType => typeof(DeviceOutputModule);

        public DeviceDescription SelectedDeviceDescription
        {
            get => _selectedDeviceDescription;
            set => SetProperty(ref _selectedDeviceDescription, value);
        }

        public override WidgetState GetState()
        {
            var state = base.GetState();
            state.Data[SettingKeyDeviceId] = SelectedDeviceDescription?.Id;
            return state;
        }

        public override void Initialize()
        {
            UpdateDeviceDescriptions();
            base.Initialize();
        }

        public override void LoadState(WidgetState state)
        {
            base.LoadState(state);

            var deviceId = state?.Data.GetObject<string>(SettingKeyDeviceId);
            if (deviceId != null)
            {
                SelectedDeviceDescription = DeviceDescriptions?.FirstOrDefault(d => d.Id == deviceId);
            }
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Output).ToArray();
        }
    }
}