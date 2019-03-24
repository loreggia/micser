using Micser.Common.Devices;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure.Widgets
{
    public abstract class DeviceWidgetViewModel : AudioWidgetViewModel
    {
        public const string StateKeyDeviceId = "DeviceId";
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceDescription _selectedDeviceDescription;

        public IEnumerable<DeviceDescription> DeviceDescriptions
        {
            get => _deviceDescriptions;
            set
            {
                var selectedId = SelectedDeviceDescription?.Id;
                if (SetProperty(ref _deviceDescriptions, value) && !string.IsNullOrEmpty(selectedId))
                {
                    SelectedDeviceDescription = value.FirstOrDefault(d => d.Id == selectedId);
                }
            }
        }

        public DeviceDescription SelectedDeviceDescription
        {
            get => _selectedDeviceDescription;
            set => SetProperty(ref _selectedDeviceDescription, value);
        }

        protected abstract DeviceType DeviceType { get; }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[StateKeyDeviceId] = SelectedDeviceDescription?.Id;
            return state;
        }

        public override void Initialize()
        {
            var deviceService = new DeviceService();
            UpdateDeviceDescriptions(deviceService);
            base.Initialize();
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(StateKeyDeviceId);
            SelectedDeviceDescription = deviceId != null ? DeviceDescriptions?.FirstOrDefault(d => d.Id == deviceId) : null;
        }

        protected virtual void UpdateDeviceDescriptions(DeviceService deviceService)
        {
            DeviceDescriptions = deviceService.GetDevices(DeviceType).ToArray();
        }
    }
}