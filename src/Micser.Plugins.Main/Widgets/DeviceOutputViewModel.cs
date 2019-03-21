using Micser.App.Infrastructure.Widgets;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Plugins.Main.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Plugins.Main.Widgets
{
    public class DeviceOutputViewModel : AudioWidgetViewModel
    {
        public const string InputConnectorName = "Input1";
        public const string StateKeyDeviceId = "DeviceId";

        private readonly ConnectorViewModel _inputViewModel;
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceDescription _selectedDeviceDescription;

        public DeviceOutputViewModel()
        {
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

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[StateKeyDeviceId] = SelectedDeviceDescription?.Id;
            return state;
        }

        public override void Initialize()
        {
            UpdateDeviceDescriptions();
            base.Initialize();
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(StateKeyDeviceId);
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