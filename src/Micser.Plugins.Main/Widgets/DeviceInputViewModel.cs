using Micser.App.Infrastructure.Widgets;
using Micser.Common.Devices;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class DeviceInputViewModel : DeviceWidgetViewModel
    {
        public const string OutputConnectorName = "Output1";

        public DeviceInputViewModel()
        {
            AddOutput(OutputConnectorName);
        }

        public override Type ModuleType => typeof(DeviceInputModule);
        protected override DeviceType DeviceType => DeviceType.Input;
    }
}