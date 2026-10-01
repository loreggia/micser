using Micser.Audio;
using Micser.Plugins.Main.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.Plugins.Main;

public static class MainPlugin
{
    /// <summary>
    /// Registers the built-in modules. They need an <see cref="Audio.Devices.AudioDeviceService"/> and logging from the host.
    /// </summary>
    public static IServiceCollection AddMainPlugin(this IServiceCollection services)
    {
        return services
            .AddAudioModule<DeviceInputModule, DeviceInputState>("DeviceInput")
            .AddAudioModule<LoopbackInputModule, LoopbackInputState>("LoopbackInput")
            .AddAudioModule<DeviceOutputModule, DeviceOutputState>("DeviceOutput")
            .AddAudioModule<GainModule, GainState>("Gain")
            .AddAudioModule<CompressorModule, CompressorState>("Compressor")
            .AddAudioModule<EqualizerModule, EqualizerState>("Equalizer")
            .AddAudioModule<PitchModule, PitchState>("Pitch")
            .AddAudioModule<SpectrumModule, SpectrumState>("Spectrum");
    }
}
