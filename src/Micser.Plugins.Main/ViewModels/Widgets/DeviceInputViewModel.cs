using System.Collections.Generic;
using System.Linq;
using Micser.Infrastructure.Widgets;
using Micser.Shared;
using Micser.Shared.Models;

namespace Micser.Plugins.Main.ViewModels.Widgets
{
    public class DeviceInputViewModel : AudioChainLinkViewModel
    {
        public const string OutputConnectorName = "Output1";
        public const string SettingKeyDeviceId = "DeviceId";

        private readonly ConnectorViewModel _outputViewModel;
        private IEnumerable<DeviceDescription> _deviceDescriptions;
        private DeviceDescription _selectedDeviceDescription;

        public DeviceInputViewModel()
        {
            Header = "Device Input";
            _outputViewModel = AddOutput(OutputConnectorName);
        }

        public IEnumerable<DeviceDescription> DeviceDescriptions
        {
            get => _deviceDescriptions;
            set => SetProperty(ref _deviceDescriptions, value);
        }

        public DeviceDescription SelectedDeviceDescription
        {
            get => _selectedDeviceDescription;
            set => SetProperty(ref _selectedDeviceDescription, value);
        }

        public override void Initialize()
        {
            UpdateDeviceDescriptions();
            base.Initialize();
        }

        public override void LoadState(WidgetState state)
        {
            base.LoadState(state);

            if (state.Settings.TryGetValue(SettingKeyDeviceId, out var deviceId) && deviceId is string idString)
            {
                SelectedDeviceDescription = DeviceDescriptions?.FirstOrDefault(d => d.Id == idString);
            }
        }

        public override void SaveState(WidgetState state)
        {
            base.SaveState(state);

            state.Settings[SettingKeyDeviceId] = SelectedDeviceDescription?.Id;
        }

        private void UpdateDeviceDescriptions()
        {
            var deviceService = new DeviceService();
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Input).ToArray();
        }
    }
}