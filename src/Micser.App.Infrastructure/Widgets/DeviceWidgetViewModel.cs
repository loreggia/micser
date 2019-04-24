using CSCore.CoreAudioAPI;
using Micser.Common.Devices;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure.Widgets
{
    public abstract class DeviceWidgetViewModel : AudioWidgetViewModel
    {
        public const string StateKeyDeviceId = "DeviceId";
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceDescription _selectedDeviceDescription;

        protected DeviceWidgetViewModel()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.DeviceAdded += OnDeviceAdded;
            _deviceEnumerator.DeviceRemoved += OnDeviceRemoved;
            _deviceEnumerator.DeviceStateChanged += OnDeviceStateChanged;
        }

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
            UpdateDeviceDescriptions();
            base.Initialize();
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(StateKeyDeviceId);
            SelectedDeviceDescription = deviceId != null ? DeviceDescriptions?.FirstOrDefault(d => d.Id == deviceId) : null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _deviceEnumerator.DeviceAdded -= OnDeviceAdded;
                _deviceEnumerator.DeviceRemoved -= OnDeviceRemoved;
                _deviceEnumerator.DeviceStateChanged -= OnDeviceStateChanged;
                _deviceEnumerator.Dispose();
            }
        }

        protected virtual void OnDeviceAdded(object sender, DeviceNotificationEventArgs e)
        {
            UpdateDeviceDescriptions();
        }

        protected virtual void OnDeviceRemoved(object sender, DeviceNotificationEventArgs e)
        {
            UpdateDeviceDescriptions();
        }

        protected virtual void OnDeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            UpdateDeviceDescriptions();
        }

        protected virtual void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType).ToArray();
        }
    }
}