using Micser.Common.Devices;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class LoopbackDeviceInputViewModel : DeviceInputViewModel
    {
        public override Type ModuleType => typeof(LoopbackDeviceInputModule);

        protected override DeviceType DeviceType => DeviceType.Output;
    }
}