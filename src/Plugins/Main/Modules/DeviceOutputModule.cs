using Micser.Audio;
using Micser.Audio.Devices;
using Microsoft.Extensions.Logging;

namespace Micser.Plugins.Main.Modules;

/// <summary>
/// Plays its input on an output device. The input is mixed into the device's channel layout.
/// </summary>
public class DeviceOutputModule : DeviceModule<RenderStream>
{
    public DeviceOutputModule(AudioDeviceService devices, ILogger<DeviceOutputModule> logger)
        : base(devices, logger)
    {
        Input = AddInput("Input", ChannelLayout.None);
    }

    public InputPort Input { get; }

    protected override DeviceDirection Direction => DeviceDirection.Output;

    protected override void OnStreamChanged(RenderStream? stream)
    {
        Input.Layout = stream?.Layout ?? ChannelLayout.None;
    }

    protected override RenderStream OpenStream(string deviceId)
    {
        return Devices.OpenRender(deviceId, Format);
    }

    protected override void Process()
    {
        UseStream(static (stream, module) =>
        {
            var buffer = module.Input.Buffer;
            if (stream != null && buffer.Layout == stream.Layout)
            {
                module.ApplyVolume(buffer);
                stream.Write(buffer);
            }
        }, this);
    }
}
