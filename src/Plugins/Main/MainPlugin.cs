using Microsoft.Extensions.DependencyInjection;
using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main;

/// <summary>
/// The built-in modules. They need an <see cref="Audio.Devices.AudioDeviceService"/> and logging from the host.
/// </summary>
public sealed class MainPlugin : IAudioPlugin
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddAudioModule<DeviceInputModule, DeviceInputState>("DeviceInput")
            .AddAudioModule<LoopbackInputModule, LoopbackInputState>("LoopbackInput")
            .AddAudioModule<DeviceOutputModule, DeviceOutputState>("DeviceOutput")
            .AddAudioModule<GainModule, GainState>("Gain")
            .AddAudioModule<CompressorModule, CompressorState>("Compressor")
            .AddAudioModule<EqualizerModule, EqualizerState>("Equalizer")
            .AddAudioModule<FilterModule, FilterState>("Filter")
            .AddAudioModule<PitchModule, PitchState>("Pitch")
            .AddAudioModule<SpectrumModule, SpectrumState>("Spectrum");
    }
}
