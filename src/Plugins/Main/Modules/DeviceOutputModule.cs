using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Micser.Audio;
using Micser.Audio.Devices;

namespace Micser.Plugins.Main.Modules;

public sealed record DeviceOutputState(
    string? DeviceId,
    string? AdapterName,
    [Range(0, 1000)] double? BufferMilliseconds = null
) : DeviceModuleState(DeviceId, AdapterName, BufferMilliseconds);

/// <summary>
/// Plays its input on an output device. The input is mixed into the device's channel layout.
/// </summary>
public class DeviceOutputModule : DeviceModule<RenderStream>, IStatefulModule<DeviceOutputState>
{
    public DeviceOutputModule(AudioDeviceService devices, ILogger<DeviceOutputModule> logger)
        : base(devices, logger)
    {
        Input = AddInput("Input", ChannelLayout.None);
    }

    public InputPort Input { get; }

    protected override DeviceDirection Direction => DeviceDirection.Output;

    public DeviceOutputState GetState()
    {
        return new DeviceOutputState(DeviceId, AdapterName, BufferMilliseconds);
    }

    public void SetState(DeviceOutputState state)
    {
        SelectDevice(state.DeviceId, state.AdapterName, state.BufferMilliseconds);
    }

    protected override void OnStreamChanged(RenderStream? stream)
    {
        Input.Layout = stream?.Layout ?? ChannelLayout.None;
    }

    protected override RenderStream OpenStream(string deviceId, double? bufferMilliseconds)
    {
        return Devices.OpenRender(deviceId, Format, bufferMilliseconds);
    }

    protected override void Process()
    {
        UseStream(
            static (stream, module) =>
            {
                var buffer = module.Input.Buffer;
                if (stream != null && buffer.Layout == stream.Layout)
                {
                    module.ApplyVolume(buffer);
                    stream.Write(buffer);
                }
            },
            this
        );
    }
}
