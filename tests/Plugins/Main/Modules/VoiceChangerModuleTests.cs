using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class VoiceChangerModuleTests
{
    [Test]
    [Arguments(1, 48000, 512, 4)]
    [Arguments(4, 48000, 1024, 4)]
    [Arguments(8, 48000, 1024, 8)]
    [Arguments(10, 48000, 2048, 8)]
    [Arguments(10, 96000, 4096, 8)]
    [Arguments(0, 44100, 512, 4)]
    public async Task GetFftSettings_MapsQualityAndSampleRate(
        int quality,
        int sampleRate,
        int fftSize,
        int oversampling
    )
    {
        await Assert.That(VoiceChangerModule.GetFftSettings(quality, sampleRate)).IsEqualTo((fftSize, oversampling));
    }

    [Test]
    [Arguments(-12f, 0.5f)]
    [Arguments(0f, 1f)]
    [Arguments(12f, 2f)]
    public async Task GetFactor_IsOneOctavePerTwelveSemitones(float semitones, float factor)
    {
        await Assert.That(VoiceChangerModule.GetFactor(semitones)).IsEqualTo(factor).Within(1e-5f);
    }

    [Test]
    public async Task Unshifted_PassesSignalThrough()
    {
        var bench = new SignalTestBench(440, 0.5f, null, new VoiceChangerModule());

        var output = bench.Run(TimeSpan.FromMilliseconds(100));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0.5f).Within(1e-4f);
    }

    [Test]
    public async Task Radio_WithoutShift_StillFiltersTheOutput()
    {
        var module = new VoiceChangerModule();
        module.SetState(new VoiceChangerState(Effect: VoiceEffect.Radio));
        var bench = new SignalTestBench(100, 0.05f, null, module);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(SignalTestBench.Peak(output.AsSpan(output.Length / 2))).IsLessThan(0.05f / 4f);
    }

    [Test]
    public async Task TargetMode_MovesThePitchToTheTarget()
    {
        var module = new VoiceChangerModule();
        // the formants move with the pitch (twice the frequency), so the lone sine stays under its envelope
        module.SetState(new VoiceChangerState(Formant: 12f, Mode: VoicePitchMode.Target, TargetPitch: 300f));
        var spectrum = new SpectrumModule();
        var bench = new SignalTestBench(150, 0.5f, null, module, spectrum);
        bench.Run(TimeSpan.FromSeconds(1));

        var result = spectrum.GetSpectrum()!;
        var peakFrequency = Array.IndexOf(result.Magnitudes, result.Magnitudes.Max()) * result.FrequencyResolution;

        await Assert.That(peakFrequency).IsEqualTo(300f).Within(2 * result.FrequencyResolution);
    }

    [Test]
    public async Task Chipmunk_DoublesFrequency()
    {
        var module = new VoiceChangerModule();
        module.SetState(new VoiceChangerState(VoiceChangerPreset.Chipmunk, 12f, 12f));
        var spectrum = new SpectrumModule();
        var bench = new SignalTestBench(440, 0.5f, null, module, spectrum);
        bench.Run(TimeSpan.FromSeconds(1));

        var result = spectrum.GetSpectrum()!;
        var peakFrequency = Array.IndexOf(result.Magnitudes, result.Magnitudes.Max()) * result.FrequencyResolution;

        await Assert.That(peakFrequency).IsEqualTo(880f).Within(2 * result.FrequencyResolution);
    }
}
