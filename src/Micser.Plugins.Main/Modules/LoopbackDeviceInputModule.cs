using CSCore.SoundIn;

namespace Micser.Plugins.Main.Modules
{
    public class LoopbackDeviceInputModule : DeviceInputModule
    {
        private int _latency;

        public LoopbackDeviceInputModule()
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

        protected override WasapiCapture CreateCapture()
        {
            return new WasapiLoopbackCapture(Latency) { Device = Device };
        }
    }
}