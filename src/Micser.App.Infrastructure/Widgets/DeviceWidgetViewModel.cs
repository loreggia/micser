using CSCore.CoreAudioAPI;
using Micser.Common;
using Micser.Common.Devices;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// A widget view model base class for widgets that provide hardware device selection.
    /// </summary>
    public abstract class DeviceWidgetViewModel : AudioWidgetViewModel
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private DeviceDescription _selectedDeviceDescription;

        /// <inheritdoc />
        protected DeviceWidgetViewModel()
        {
            DeviceDescriptions = new ObservableCollection<DeviceDescription>();
            BindingOperations.EnableCollectionSynchronization(DeviceDescriptions, new object());

            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.DeviceAdded += OnDeviceAdded;
            _deviceEnumerator.DeviceRemoved += OnDeviceRemoved;
            _deviceEnumerator.DeviceStateChanged += OnDeviceStateChanged;
        }

        /// <summary>
        /// Gets or sets the available devices. This is set automatically in <see cref="UpdateDeviceDescriptions"/>.
        /// </summary>
        public ObservableCollection<DeviceDescription> DeviceDescriptions { get; }

        /// <summary>
        /// Gets or sets the currently selected device.
        /// </summary>
        public DeviceDescription SelectedDeviceDescription
        {
            get => _selectedDeviceDescription;
            set => SetProperty(ref _selectedDeviceDescription, value);
        }

        /// <summary>
        /// Gets the type of devices that are handled by the widget. This controls the devices that are available in <see cref="DeviceDescriptions"/>.
        /// </summary>
        protected abstract DeviceType DeviceType { get; }

        /// <inheritdoc />
        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[Globals.StateKeys.DeviceId] = SelectedDeviceDescription?.Id;
            return state;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            UpdateDeviceDescriptions();
            base.Initialize();
        }

        /// <inheritdoc />
        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(Globals.StateKeys.DeviceId);
            SelectedDeviceDescription = deviceId != null ? DeviceDescriptions?.FirstOrDefault(d => d.Id == deviceId) : null;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && _deviceEnumerator != null)
            {
                _deviceEnumerator.DeviceAdded -= OnDeviceAdded;
                _deviceEnumerator.DeviceRemoved -= OnDeviceRemoved;
                _deviceEnumerator.DeviceStateChanged -= OnDeviceStateChanged;
                _deviceEnumerator.Dispose();
            }
        }

        /// <summary>
        /// Event handler that is called when a new device is added in the system.
        /// </summary>
        protected virtual void OnDeviceAdded(object sender, DeviceNotificationEventArgs e)
        {
            UpdateDeviceDescriptions();
        }

        /// <summary>
        /// Event handler that is called when a device is no longer available in the system.
        /// </summary>
        protected virtual void OnDeviceRemoved(object sender, DeviceNotificationEventArgs e)
        {
            UpdateDeviceDescriptions();
        }

        /// <summary>
        /// Event handler that is called when the state of an available device changes.
        /// </summary>
        protected virtual void OnDeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            UpdateDeviceDescriptions();
        }

        /// <summary>
        /// Updates the available devices in <see cref="DeviceDescriptions"/> using the current <see cref="DeviceType"/>.
        /// </summary>
        protected virtual void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            var deviceDescriptions = deviceService.GetDevices(DeviceType).ToArray();
            DeviceDescriptions.Update(deviceDescriptions, (a, b) => a.Id == b.Id);
        }
    }
}