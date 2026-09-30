using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class PitchModuleTests
{
    [Test]
    [Arguments(1, 256, 4)]
    [Arguments(10, 4096, 8)]
    [Arguments(0, 256, 4)]
    public async Task GetFftSettings_MapsQuality(int quality, int fftSize, int oversampling)
    {
        await Assert.That(PitchModule.GetFftSettings(quality)).IsEqualTo((fftSize, oversampling));
    }

    [Test]
    [Arguments(-1f, 0.5f)]
    [Arguments(0f, 1f)]
    [Arguments(0.5f, 1.4142135f)]
    [Arguments(1f, 2f)]
    [Arguments(3f, 2f)]
    public async Task GetPitchFactor_IsOneOctavePerUnit(float pitch, float factor)
    {
        await Assert.That(PitchModule.GetPitchFactor(pitch)).IsEqualTo(factor).Within(1e-5f);
    }

    [Test]
    public async Task Pitch_OneOctaveUp_DoublesFrequency()
    {
        var spectrum = new SpectrumModule();
        var bench = new SignalTestBench(440, 0.5f, null, new PitchModule { Pitch = 1f, Quality = 8 }, spectrum);
        bench.Run(TimeSpan.FromSeconds(1));

        var result = spectrum.GetSpectrum()!;
        var peakFrequency = Array.IndexOf(result.Magnitudes, result.Magnitudes.Max()) * result.FrequencyResolution;

        await Assert.That(peakFrequency).IsEqualTo(880f).Within(2 * result.FrequencyResolution);
    }

    [Test]
    public async Task Pitch_Zero_PassesSignalThrough()
    {
        var bench = new SignalTestBench(440, 0.5f, null, new PitchModule());

        var output = bench.Run(TimeSpan.FromMilliseconds(100));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0.5f).Within(1e-4f);
    }
}
