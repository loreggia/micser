using Micser.Audio.Devices;
using Microsoft.Extensions.Logging;

namespace Micser.Plugins.Main.Modules;

/// <summary>
/// Captures what is played on an output device.
/// </summary>
public class LoopbackInputModule : DeviceInputModule
{
    public LoopbackInputModule(AudioDeviceService devices, ILogger<LoopbackInputModule> logger)
        : base(devices, logger)
    {
    }

    protected override DeviceDirection Direction => DeviceDirection.Output;

    protected override CaptureStream OpenStream(string deviceId)
    {
        return Devices.OpenLoopback(deviceId, Format);
    }
}
