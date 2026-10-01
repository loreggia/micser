using Micser.Audio;
using Micser.Audio.Devices;
using Microsoft.Extensions.Logging;

namespace Micser.Plugins.Main.Modules;

public sealed record DeviceInputState(string? DeviceId, string? AdapterName) : DeviceModuleState(DeviceId, AdapterName);

/// <summary>
/// Captures an input device.
/// </summary>
public class DeviceInputModule : CaptureModule, IStatefulModule<DeviceInputState>
{
    public DeviceInputModule(AudioDeviceService devices, ILogger<DeviceInputModule> logger)
        : base(devices, logger)
    {
    }

    protected override DeviceDirection Direction => DeviceDirection.Input;

    public DeviceInputState GetState()
    {
        return new DeviceInputState(DeviceId, AdapterName);
    }

    public void SetState(DeviceInputState state)
    {
        SelectDevice(state.DeviceId, state.AdapterName);
    }

    protected override CaptureStream OpenStream(string deviceId)
    {
        return Devices.OpenCapture(deviceId, Format);
    }
}
