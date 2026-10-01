using Micser.Audio;
using Micser.Audio.Devices;
using Microsoft.Extensions.Logging;

namespace Micser.Plugins.Main.Modules;

public sealed record LoopbackInputState(string? DeviceId, string? AdapterName) : DeviceModuleState(DeviceId, AdapterName);

/// <summary>
/// Captures what is played on an output device.
/// </summary>
public class LoopbackInputModule : CaptureModule, IStatefulModule<LoopbackInputState>
{
    public LoopbackInputModule(AudioDeviceService devices, ILogger<LoopbackInputModule> logger)
        : base(devices, logger)
    {
    }

    protected override DeviceDirection Direction => DeviceDirection.Output;

    public LoopbackInputState GetState()
    {
        return new LoopbackInputState(DeviceId, AdapterName);
    }

    public void SetState(LoopbackInputState state)
    {
        SelectDevice(state.DeviceId, state.AdapterName);
    }

    protected override CaptureStream OpenStream(string deviceId)
    {
        return Devices.OpenLoopback(deviceId, Format);
    }
}
