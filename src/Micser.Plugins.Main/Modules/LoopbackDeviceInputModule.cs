using CSCore.SoundIn;
using Micser.Common.Api;
using Micser.Common.Devices;
using Micser.Engine.Infrastructure.Services;

namespace Micser.Plugins.Main.Modules
{
    public class LoopbackDeviceInputModule : DeviceInputModule
    {
        private int _latency;

        public LoopbackDeviceInputModule(IApiClient apiClient, IModuleService moduleService)
            : base(apiClient, moduleService)
        {
            Latency = 1;
        }

        public int Latency
        {
            get => _latency;
            set
            {
                if (_latency != value)
                {
                    _latency = value;
                    InitializeDevice();
                }
            }
        }

        protected override DeviceType DeviceType => DeviceType.Output;

        protected override WasapiCapture CreateCapture()
        {
            return new WasapiLoopbackCapture(Latency) { Device = Device };
        }
    }
}