using Micser.Common.Devices;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class LoopbackDeviceInputViewModel : DeviceInputViewModel
    {
        public LoopbackDeviceInputViewModel()
        {
            Header = "Loopback Device Input";

            Volume = 1f;
        }

        public override Type ModuleType => typeof(LoopbackDeviceInputModule);

        protected override void UpdateDeviceDescriptions(DeviceService deviceService)
        {
            DeviceDescriptions = deviceService.GetDevices(DeviceType.Output);
        }
    }
}