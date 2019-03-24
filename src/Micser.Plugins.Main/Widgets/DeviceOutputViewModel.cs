using Micser.App.Infrastructure.Widgets;
using Micser.Common.Devices;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class DeviceOutputViewModel : DeviceWidgetViewModel
    {
        public const string InputConnectorName = "Input1";

        public DeviceOutputViewModel()
        {
            AddInput(InputConnectorName);
        }

        public override Type ModuleType => typeof(DeviceOutputModule);

        protected override DeviceType DeviceType => DeviceType.Output;
    }
}