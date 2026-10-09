using Microsoft.Extensions.Logging;
using Micser.Audio;
using Micser.Audio.Devices;

namespace Micser.Plugins.Main.Modules;

/// <param name="DeviceId">The selected device, or null for none.</param>
/// <param name="AdapterName">The adapter of the selected device, used to find it again when its ID changes.</param>
/// <param name="BufferMilliseconds">The stream buffer the engine learned for this device (it grows on dropouts), so a reopened stream starts
/// there. Null to start at the minimum, e.g. for a newly selected device.</param>
public abstract record DeviceModuleState(string? DeviceId, string? AdapterName, double? BufferMilliseconds);

/// <summary>
/// Captures a device. The output has the device's channel layout.
/// </summary>
public abstract class CaptureModule : DeviceModule<CaptureStream>
{
    protected CaptureModule(AudioDeviceService devices, ILogger logger)
        : base(devices, logger)
    {
        Output = AddOutput("Output");
    }

    public OutputPort Output { get; }

    protected override void Process()
    {
        UseStream(
            static (stream, output) =>
            {
                if (stream == null)
                {
                    output.Buffer.SetLayout(ChannelLayout.None);
                }
                else
                {
                    stream.Read(output.Buffer);
                }
            },
            Output
        );
    }
}
