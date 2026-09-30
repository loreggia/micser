using Micser.Audio;
using Micser.Audio.Devices;
using Microsoft.Extensions.Logging;

namespace Micser.Plugins.Main.Modules;

/// <summary>
/// Captures an input device. The output has the device's channel layout.
/// </summary>
public class DeviceInputModule : DeviceModule<CaptureStream>
{
    public DeviceInputModule(AudioDeviceService devices, ILogger<DeviceInputModule> logger)
        : this(devices, (ILogger)logger)
    {
    }

    protected DeviceInputModule(AudioDeviceService devices, ILogger logger)
        : base(devices, logger)
    {
        Output = AddOutput("Output");
    }

    public OutputPort Output { get; }

    protected override DeviceDirection Direction => DeviceDirection.Input;

    protected override CaptureStream OpenStream(string deviceId)
    {
        return Devices.OpenCapture(deviceId, Format);
    }

    protected override void Process()
    {
        UseStream(static (stream, output) =>
        {
            if (stream == null)
            {
                output.Buffer.SetLayout(ChannelLayout.None);
            }
            else
            {
                stream.Read(output.Buffer);
            }
        }, Output);
    }
}
